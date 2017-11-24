using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CppSharp;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;

namespace Embeddinator.Generators
{
    [DebuggerDisplay("Decl = {Declaration}")]
    public class JavaSources : CodeGenerator
    {
        public JavaTypePrinter TypePrinter;

        public JavaSources(BindingContext context, Declaration decl)
            : this(context, decl.TranslationUnit)
        {
            Declaration = decl;
        }

        public JavaSources(BindingContext context, TranslationUnit unit)
            : base(context, new List<TranslationUnit> { unit })
        {
            TypePrinter = new JavaTypePrinter(context);
            VisitOptions.VisitPropertyAccessors = true;
        }

        public Declaration Declaration;

        public override string FileExtension => "java";

        public static IEnumerable<string> GetPackageNames(Declaration decl)
        {
            var namespaces = Declaration.GatherNamespaces(decl.Namespace)
                .Where(ns => !(ns is TranslationUnit));

            var names = namespaces.Select(n => n.Name.ToLowerInvariant()).ToList();
            names.Insert(0, JavaGenerator.GetNativeLibPackageName(decl.TranslationUnit));

            return names;
        }

        public override string FilePath
        {
            get
            {
                var names = GetPackageNames(Declaration).ToList();
                names.Add(Declaration.Name);

                var filePath = string.Join(Path.DirectorySeparatorChar.ToString(), names);
                return $"{filePath}.{FileExtension}";
            }
        }

        public string AssemblyId => CGenerator.AssemblyId(TranslationUnit);

        public override string AccessIdentifier(AccessSpecifier accessSpecifier) =>
            GetAccess(accessSpecifier);

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.JavaDoc, "Embeddinator-4000");

            GenerateJavaPackage(Declaration);
            GenerateJavaImports();

            PushBlock();
            Declaration.Visit(this);
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateJavaPackage(Declaration decl)
        {
            PushBlock();
            var package = string.Join(".", GetPackageNames(decl));
            if (!string.IsNullOrWhiteSpace(package))
                WriteLine($"package {package};");
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateJavaImports()
        {
            PushBlock();
            WriteLine("import mono.embeddinator.*;");
            WriteLine("import com.sun.jna.*;");
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            return decl.IsGenerated && !AlreadyVisited(decl);
        }

        public override bool VisitDeclContext(DeclarationContext context)
        {
            foreach (var decl in context.Declarations)
                if (decl.IsGenerated)
                    decl.Visit(this);

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!VisitDeclaration(@enum))
                return false;

            if (@enum.IsIncomplete)
                return true;

            PushBlock(BlockKind.Enum);
            GenerateDeclarationCommon(@enum);

            Write("{0} final class {1} ", AccessIdentifier(@enum.Access), @enum.Name);

            WriteStartBraceIndent();
            GenerateEnumItems(@enum);

            NewLine();

            var typeName = @enum.BuiltinType.Visit(TypePrinter);
            WriteLine($"private final {typeName} id;");
            WriteLine($"{@enum.Name}({typeName} id) {{ this.id = id; }}");
            WriteLine($"public {typeName} getValue() {{ return id; }}");

            NewLine();
            var value = @enum.BuiltinType.IsUnsigned ? "n.intValue()" : "n";
            WriteLine($"public static {@enum.Name} fromOrdinal({typeName} n) {{");
            WriteLineIndent($"return valuesMap.containsKey({value}) ? valuesMap.get({value}) : new {@enum.Name}(n);");
            WriteLine("}");

            TypePrinter.PushContext(TypePrinterContextKind.Template);
            var refTypeName = @enum.BuiltinType.Visit(TypePrinter);
            TypePrinter.PopContext();

            NewLine();
            WriteLine($"private static final java.util.Map<{refTypeName}, {@enum.Name}> valuesMap = ");
            WriteLineIndent($"new java.util.HashMap<{refTypeName}, {@enum.Name}>();");

            NewLine();
            WriteLine("static {");
            PushIndent();

            WriteLine("try {");
            PushIndent();

            WriteLine($"java.lang.reflect.Field[] constants = {@enum.Name}.class.getFields();");
            WriteLine($"for (final java.lang.reflect.Field field : constants) {{");
            WriteLineIndent($"{@enum.Name} item = ({@enum.Name}) field.get(null);");
            WriteLineIndent($"valuesMap.put(item.getValue(), item);");
            WriteLine("}");

            PopIndent();
            WriteLine("} catch(java.lang.IllegalAccessException ex) {");
            WriteLine("}");

            PopIndent();
            WriteLine("}");

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override void GenerateEnumItems(Enumeration @enum)
        {
            for (int i = 0; i < @enum.Items.Count; i++)
            {
                if (@enum.Items[i].Visit(this))
                    NewLine();
            }
        }

        public override bool VisitEnumItemDecl(Enumeration.Item item)
        {
            if (!VisitDeclaration(item))
                return false;

            if (item.Comment != null)
                GenerateInlineSummary(item.Comment);

            var @enum = item.Namespace as Enumeration;
            Write($"public static final {@enum.Name} {item.Name} = new {@enum.Name}");

            var typeName = @enum.BuiltinType.Visit(TypePrinter);
            if (item.ExplicitValue)
            {
                var value = @enum.GetItemValueAsString(item);

                if (@enum.BuiltinType.IsUnsigned)
                    Write($"(new {typeName}({value}));");
                else
                    Write($"(({typeName}){value});");
            }

            return true;
        }

        public override void GenerateClassSpecifier(Class @class)
        {
            var keywords = new List<string>();
            
            keywords.Add(AccessIdentifier(@class.Access));

            if (@class.IsAbstract)
                keywords.Add("abstract");

            if (@class.IsFinal || @class.IsStatic)
                keywords.Add("final");

            keywords.Add(@class.IsInterface ? "interface" : "class");
            keywords.Add(@class.Name);

            keywords = keywords.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (keywords.Count != 0)
                Write("{0}", string.Join(" ", keywords));

            var bases = @class.Bases.Where(@base => @base.IsClass && @base.Class.IsGenerated).ToList();

            if (bases.Count > 0 && !@class.IsStatic)
            {
                var classes = bases.Where(@base => !@base.Class.IsInterface)
                                   .Select(@base => @base.Class.Visit(TypePrinter).Type);
                if (classes.Count() > 0)
                    Write(" extends {0}", string.Join(", ", classes));

                var implements = @class.IsInterface ? "extends" : "implements";
                var interfaces = bases.Where(@base => @base.Class.IsInterface && @base.Class.IsGenerated)
                                      .Select(@base => @base.Class.Visit(TypePrinter).Type);
                if (interfaces.Count() > 0)
                    Write($" {implements} {string.Join(", ", interfaces)}");
            }
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            GenerateClassSpecifier(@class);

            Write(" ");
            WriteStartBraceIndent();

            var hasNonInterfaceBase = @class.HasBaseClass && @class.BaseClass.IsGenerated
                && !@class.BaseClass.IsInterface;

            var objectIdent = JavaGenerator.GeneratedIdentifier("object");

            if (!@class.IsStatic && !@class.IsInterface)
            {
                if (!hasNonInterfaceBase)
                {
                    WriteLine($"public {JavaGenerator.IntPtrType} {objectIdent};");
                    NewLine();
                }
                
                Write($"public {@class.Name}({JavaGenerator.IntPtrType} object) {{ ");
                WriteLine(hasNonInterfaceBase ? "super(object); }" : $"this.{objectIdent} = object; }}");
                NewLine();

                var implementsInterfaces = @class.Bases.Any(b => b.Class.IsGenerated && b.Class.IsInterface);
                if (implementsInterfaces)
                {
                    WriteLine("@Override");
                    WriteLine($"public com.sun.jna.Pointer __getObject() {{ return this.{objectIdent}; }}");
                    NewLine();
                }
            }

            VisitDeclContext(@class);
            WriteCloseBraceIndent();

            return true;
        }

        public static string GetMethodIdentifier(Method method)
        {
            var name = method.Name;

            if (method.AssociatedDeclaration is Property)
            {
                // Property names shoud follow get/set Java convention.
                if (name.StartsWith("get_", StringComparison.Ordinal))
                    name = $"get{name.TrimStart("get_")}";
                else if (name.StartsWith("set_", StringComparison.Ordinal))
                    name = $"set{name.TrimStart("set_")}";
            }

            var associated = method.GetRootAssociatedDecl();
            if (associated.DefinitionOrder != 0)
                name += $"_{associated.DefinitionOrder}";

            return name;
        }

        public override void GenerateMethodSpecifier(Method method, Class @class)
        {
            var keywords = new List<string>();

            if (method.IsGeneratedOverride() || method.IsOverride)
            {
                Write("@Override");
                NewLine();
            }

            keywords.Add(AccessIdentifier(method.Access));

            if (@method.IsFinal)
                keywords.Add("final");

            if (method.IsStatic)
                keywords.Add("static");

            if (method.IsPure && !@class.IsInterface)
                keywords.Add("abstract");

            keywords = keywords.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (keywords.Count != 0)
                Write("{0} ", string.Join(" ", keywords));

            if (method.IsConstructor || method.IsDestructor)
                Write("{0}(", @class.Name);
            else
                Write("{0} {1}(", method.ReturnType, GetMethodIdentifier(method));

            var @params = method.Parameters.Where(m => !m.IsImplicit);
            Write("{0}", TypePrinter.VisitParameters(@params, hasNames: true));

            Write(")");
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!VisitDeclaration(method))
                return false;

            PushBlock(BlockKind.Method, method);

            var @class = method.Namespace as Class;
            GenerateMethodSpecifier(method, @class);

            if (method.IsPure)
            {
                Write(";");
            }
            else
            {
                Write(" ");
                WriteStartBraceIndent();

                var hasNonInterfaceBase = @class.HasBaseClass && @class.BaseClass.IsGenerated
                    && !@class.BaseClass.IsInterface;

                if (method.IsConstructor && hasNonInterfaceBase)
                    WriteLine("super((com.sun.jna.Pointer)null);");

                GenerateMethodInvocation(method);

                WriteCloseBraceIndent();
            }

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public void GenerateMethodInvocation(Method method)
        {
            var marshalers = new List<Marshaler>();
            var @params = new List<string>();

            if (!method.IsStatic && !(method.IsConstructor || method.IsDestructor))
                @params.Add("__object");

            int paramIndex = 0;
            foreach (var param in method.Parameters.Where(m => !m.IsImplicit))
            {
                var marshal = new JavaMarshalManagedToNative(Context)
                {
                    ArgName = param.Name,
                    Parameter = param,
                    ParameterIndex = paramIndex++
                };
                marshalers.Add(marshal);

                param.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Before))
                        Write(marshal.Before);

                @params.Add(marshal.Return);
            }

            PrimitiveType primitive;
            method.ReturnType.Type.IsPrimitiveType(out primitive);

            var hasReturn = primitive != PrimitiveType.Void && !(method.IsConstructor || method.IsDestructor);
            if (hasReturn)
            {
                TypePrinter.PushContext(TypePrinterContextKind.Native);
                var typeName = method.ReturnType.Visit(TypePrinter);
                TypePrinter.PopContext();
                Write($"{typeName.Type} __ret = ");
            }

            if (method.IsConstructor)
                Write("__object = ");

            // Get the effective method for synthetized interface method implementations.
            var effectiveMethod = method.CompleteDeclaration as Method ?? method;
            var unit = effectiveMethod.TranslationUnit;
            var package = string.Join(".", GetPackageNames(unit));
            var nativeMethodId = JavaNative.GetCMethodIdentifier(effectiveMethod);
            Write($"{package}.{JavaNative.GetNativeLibClassName(unit)}.INSTANCE.{nativeMethodId}(");

            Write(string.Join(", ", @params));
            WriteLine(");");

            WriteLine("mono.embeddinator.Runtime.checkExceptions();");

            foreach (var marshal in marshalers)
            {
                if (!string.IsNullOrWhiteSpace(marshal.After))
                    Write(marshal.After);
            }

            if (hasReturn)
            {
                var marshal = new JavaMarshalNativeToManaged(Context)
                {
                    ReturnType = method.ReturnType,
                    ReturnVarName = "__ret"
                };

                method.ReturnType.Visit(marshal);

                if (marshal.Return.ToString().Length == 0)
                    throw new System.Exception();

                if (!string.IsNullOrWhiteSpace(marshal.Before))
                        Write(marshal.Before);

                WriteLine($"return {marshal.Return};");
            }
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            // Ignore fields since they're converted to properties (getter/setter pairs).
            return true;
        }

        public static string GetAccess(AccessSpecifier accessSpecifier)
        {
            switch (accessSpecifier)
            {
                case AccessSpecifier.Private:
                    return "private";
                case AccessSpecifier.Internal:
                    return string.Empty;
                case AccessSpecifier.Protected:
                    return "protected";
                default:
                    return "public";
            }
        }
    }
}
