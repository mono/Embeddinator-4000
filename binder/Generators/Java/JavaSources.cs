using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;

namespace MonoEmbeddinator4000.Generators
{
    public class JavaSources : CSharpSources
    {
        public JavaSources(BindingContext context, Declaration decl)
            : this(context, decl.TranslationUnit)
        {
            Declaration = decl;
        }

        public JavaSources(BindingContext context, TranslationUnit unit)
            : base(context, new List<TranslationUnit> { unit })
        {
            TypePrinter = new JavaTypePrinter(context);
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
            GenerateFilePreamble(CommentKind.JavaDoc);

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

        public override bool VisitDeclContext(DeclarationContext context)
        {
            foreach (var decl in context.Declarations)
                if (decl.IsGenerated)
                    decl.Visit(this);

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (@enum.IsIncomplete)
                return true;

            PushBlock(BlockKind.Enum);
            GenerateDeclarationCommon(@enum);

            Write("{0} enum {1} ", AccessIdentifier(@enum.Access),
                SafeIdentifier(@enum.Name));

            WriteStartBraceIndent();
            GenerateEnumItems(@enum);

            NewLine();

            var typeName = TypePrinter.VisitPrimitiveType(@enum.BuiltinType.Type,
                                                          new TypeQualifiers());
            WriteLine($"private final {typeName} id;");
            WriteLine($"{@enum.Name}({typeName} id) {{ this.id = id; }}");
            WriteLine($"public {typeName} getValue() {{ return id; }}");

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override void GenerateEnumItems(Enumeration @enum)
        {
            for (int i = 0; i < @enum.Items.Count; i++)
            {
                @enum.Items[i].Visit(this);
                WriteLine(i == @enum.Items.Count - 1 ? ";" : ",");
            }
        }

        public override bool VisitEnumItemDecl(Enumeration.Item item)
        {
            if (item.Comment != null)
                GenerateInlineSummary(item.Comment);

            Write(item.Name);

            var @enum = item.Namespace as Enumeration;
            var typeName = TypePrinter.VisitPrimitiveType(@enum.BuiltinType.Type,
                                                          new TypeQualifiers());
            if (item.ExplicitValue)
            {
                var value = @enum.GetItemValueAsString(item);

                if (@enum.BuiltinType.IsUnsigned)
                    Write($"(new {typeName}({value}))");
                else
                    Write($"(({typeName}){value})");
            }

            return true;
        }

        public override void GenerateClassSpecifier(Class @class)
        {
            var keywords = new List<string>();
            
            keywords.Add(AccessIdentifier(@class.Access));

            if (@class.IsAbstract)
                keywords.Add("abstract");

            if (@class.IsFinal)
                keywords.Add("final");

            if (@class.IsStatic)
                keywords.Add("static");

            keywords.Add(@class.IsInterface ? "interface" : "class");
            keywords.Add(SafeIdentifier(@class.Name));

            keywords = keywords.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (keywords.Count != 0)
                Write("{0}", string.Join(" ", keywords));

            var bases = new List<BaseClassSpecifier>();

            if (@class.NeedsBase)
                bases.AddRange(@class.Bases.Where(@base => @base.IsClass));

            if (bases.Count > 0 && !@class.IsStatic)
            {
                var classes = bases.Where(@base => !@base.Class.IsInterface)
                                   .Select(@base => @base.Class.Visit(TypePrinter).Type);
                if (classes.Count() > 0)
                    Write(" extends {0}", string.Join(", ", classes));

                var interfaces = bases.Where(@base => @base.Class.IsInterface)
                                      .Select(@base => @base.Class.Visit(TypePrinter).Type);
                if (interfaces.Count() > 0)
                    Write(" implements {0}", string.Join(", ", interfaces));
            }
        }

        public override bool VisitClassDecl(Class @class)
        {
            GenerateClassSpecifier(@class);

            Write(" ");
            WriteStartBraceIndent();

            if (!@class.IsStatic && !@class.HasBase)
            {
                WriteLine("public {0} __object;", JavaGenerator.IntPtrType);
                NewLine();
                
                WriteLine($"{@class.Name}(com.sun.jna.Pointer object) {{ this.__object = object; }}");
                NewLine();
            }

            VisitDeclContext(@class);
            WriteCloseBraceIndent();

            return true;
        }

        public override void GenerateMethodSpecifier(Method method, Class @class)
        {
            var keywords = new List<string>();

            if (method.IsGeneratedOverride())
            {
                Write("@Override");
                NewLine();
            }

            keywords.Add(AccessIdentifier(method.Access));

            if (@method.IsFinal)
                keywords.Add("final");

            if (method.IsStatic)
                keywords.Add("static");

            if (method.IsPure)
                keywords.Add("abstract");

            keywords = keywords.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (keywords.Count != 0)
                Write("{0} ", string.Join(" ", keywords));

            var functionName = GetMethodIdentifier(method);

            if (method.IsConstructor || method.IsDestructor)
                Write("{0}(", functionName);
            else
                Write("{0} {1}(", method.ReturnType, functionName);

            var @params = method.Parameters.Where(m => !m.IsImplicit);
            Write("{0}", TypePrinter.VisitParameters(@params, hasNames: true));

            Write(")");
        }

        public override bool VisitMethodDecl(Method method)
        {
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

                if (method.IsConstructor && @class.HasBase)
                    WriteLine("super((com.sun.jna.Pointer)null);");

                GenerateMethodInvocation(method);

                WriteCloseBraceIndent();
            }

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public void GenerateMethodInvocation(Method method)
        {
            var contexts = new List<MarshalContext>();
            var @params = new List<string>();

            if (!method.IsStatic && !(method.IsConstructor || method.IsDestructor))
                @params.Add("__object");

            int paramIndex = 0;
            foreach (var param in method.Parameters.Where(m => !m.IsImplicit))
            {
                var ctx = new MarshalContext(Context)
                {
                    ArgName = param.Name,
                    Parameter = param,
                    ParameterIndex = paramIndex++
                };
                contexts.Add(ctx);

                var marshal = new JavaMarshalManagedToNative(ctx);
                param.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                        Write(marshal.Context.SupportBefore);

                @params.Add(marshal.Context.Return);
            }

            PrimitiveType primitive;
            method.ReturnType.Type.IsPrimitiveType(out primitive);

            var hasReturn = primitive != PrimitiveType.Void && !(method.IsConstructor || method.IsDestructor);
            if (hasReturn)
            {
                TypePrinter.PushContext(TypePrinterContextKind.Native);
                var typeName = method.ReturnType.Visit(TypePrinter);
                TypePrinter.PopContext();

                Write("{0} __ret = ", typeName.Type);
            }

            if (method.IsConstructor)
                Write("__object = ");

            var unit = method.TranslationUnit;
            var package = string.Join(".", GetPackageNames(unit));
            Write($"{package}.{JavaNative.GetNativeLibClassName(unit)}.INSTANCE.{JavaNative.GetCMethodIdentifier(method)}(");

            Write(string.Join(", ", @params));

            WriteLine(");");

            WriteLine("mono.embeddinator.Runtime.checkExceptions();");

            foreach (var marshal in contexts)
            {
                if (!string.IsNullOrWhiteSpace(marshal.SupportAfter))
                    Write(marshal.SupportAfter);
            }

            if (hasReturn)
            {
                var ctx = new MarshalContext(Context)
                {
                    ReturnType = method.ReturnType,
                    ReturnVarName = "__ret"
                };

                var marshal = new JavaMarshalNativeToManaged(ctx);
                method.ReturnType.Visit(marshal);

                if (marshal.Context.Return.ToString().Length == 0)
                    throw new System.Exception();

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                        Write(marshal.Context.SupportBefore);

                WriteLine($"return {marshal.Context.Return};");
            }
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            return true;
        }

        public override bool VisitProperty(Property property)
        {
            // Ignore properties since they're converted to getter/setter pais.
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
