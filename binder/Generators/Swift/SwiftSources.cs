using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CppSharp;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;

namespace Embeddinator.Generators
{
    [DebuggerDisplay("Decl = {Declaration}")]
    public class SwiftSources : CodeGenerator
    {
        public SwiftTypePrinter TypePrinter;

        public SwiftSources(BindingContext context, Declaration decl)
            : this(context, decl.TranslationUnit)
        {
            Declaration = decl;
            TypePrinter = new SwiftTypePrinter(context);
        }

        public SwiftSources(BindingContext context, TranslationUnit unit)
            : base(context, new List<TranslationUnit> { unit })
        {
            Declaration = unit;
            TypePrinter = new SwiftTypePrinter(context);
        }

        public Declaration Declaration;

        public override string FileExtension => "swift";

        public string AssemblyId => CGenerator.AssemblyId(TranslationUnit);

        public static string GetAccess(AccessSpecifier accessSpecifier)
        {
            switch (accessSpecifier)
            {
                case AccessSpecifier.Private:
                    return "private";
                case AccessSpecifier.Internal:
                    return "internal";
                case AccessSpecifier.Protected:
                    return "protected";
                default:
                    return "public";
            }
        }

        public override string AccessIdentifier(AccessSpecifier accessSpecifier) =>
            GetAccess(accessSpecifier);

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.JavaDoc, "Embeddinator-4000");
            NewLine();

            GenerateImports();

            PushBlock();
            Declaration.Visit(this);
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateImports()
        {
            PushBlock();
            WriteLine("import Foundation");
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

            var typeName = @enum.BuiltinType.Visit(TypePrinter);
            Write($"{AccessIdentifier(@enum.Access)} enum {@enum.QualifiedName}: {typeName} ");

            WriteStartBraceIndent();

            GenerateEnumItems(@enum);

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

            Write($"case {item.Name}");

            if (item.ExplicitValue)
                Write($" = {@enum.GetItemValueAsString(item)}");

            return true;
        }

        public override void GenerateClassSpecifier(Class @class)
        {
            var keywords = new List<string>();

            keywords.Add(AccessIdentifier(@class.Access));

            if (@class.IsFinal || @class.IsStatic)
                keywords.Add("final");

            keywords.Add(@class.IsInterface ? "protocol" : "class");
            keywords.Add(@class.QualifiedName);

            keywords = keywords.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (keywords.Count != 0)
                Write($"{string.Join(" ", keywords)}");

            var bases = @class.Bases.Where(@base => @base.IsClass && @base.Class.IsGenerated).ToList();

            if (bases.Count > 0 && !@class.IsStatic)
            {
                var classes = bases.Select(@base => @base.Class.Visit(TypePrinter).Type);
                //if (classes.Count() > 0)
                //  Write($": {string.Join(", ", classes)}");
            }
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            PushBlock(BlockKind.Class);
            GenerateClassSpecifier(@class);

            Write(" ");
            WriteStartBraceIndent();

            VisitDeclContext(@class);
            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override void GenerateMethodSpecifier(Method method, Class @class)
        {
            var keywords = new List<string>();

            if (method.IsGeneratedOverride() || method.IsOverride)
            {
                Write("override");
                NewLine();
            }

            if (!@class.IsInterface)
                keywords.Add(AccessIdentifier(method.Access));

            if (method.IsStatic)
                keywords.Add("static");

            if (@method.IsFinal)
                keywords.Add("final");

            if (!method.IsConstructor && !method.IsDestructor)
                keywords.Add("func");

            keywords = keywords.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (keywords.Count != 0)
                Write($"{string.Join(" ", keywords)} ");

            if (method.IsConstructor)
                Write("init(");
            else if (method.IsDestructor)
                Write("deinit");
            else
                Write($"{method.Name}(");

            var @params = method.Parameters.Where(m => !m.IsImplicit);
            Write($"{TypePrinter.VisitParameters(@params, hasNames: true)}");

            Write(")");

            if (!method.ReturnType.Type.IsPrimitiveType(PrimitiveType.Void) &&
                !(method.IsConstructor || method.IsDestructor))
                Write($" -> {method.ReturnType}");
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!VisitDeclaration(method))
                return false;

            PushBlock(BlockKind.Method, method);

            var @class = method.Namespace as Class;
            GenerateMethodSpecifier(method, @class);

            if (!@class.IsInterface)
            {
                Write(" ");
                WriteStartBraceIndent();

                if (!method.IsPure)
                {
                    GenerateMethodInvocation(method);
                }

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

                var marshal = new SwiftMarshalManagedToNative(ctx);
                param.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                @params.Add(marshal.Context.Return);
            }

            var hasReturn = !method.ReturnType.Type.IsPrimitiveType(PrimitiveType.Void) &&
                            !(method.IsConstructor || method.IsDestructor);

            if (hasReturn)
            {
                TypePrinter.PushContext(TypePrinterContextKind.Native);
                var typeName = method.ReturnType.Visit(TypePrinter);
                TypePrinter.PopContext();
                Write($"let __ret : {typeName.Type} = ");
            }

            var effectiveMethod = method.CompleteDeclaration as Method ?? method;
            var nativeMethodId = JavaNative.GetCMethodIdentifier(effectiveMethod);
            WriteLine($"{nativeMethodId}({string.Join(", ", @params)})");

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

                var marshal = new SwiftMarshalNativeToManaged(ctx);
                method.ReturnType.Visit(marshal);

                if (marshal.Context.Return.ToString().Length == 0)
                    throw new NotSupportedException($"Cannot marshal return type {method.ReturnType}");

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                        Write(marshal.Context.SupportBefore);

                WriteLine($"return {marshal.Context.Return}");
            }
        }

        public override bool VisitTypedefDecl(TypedefDecl typedef)
        {
            return true;
        }

        public override bool VisitProperty(Property property)
        {
            if (!VisitDeclaration(property))
                return false;

            if (property.Field == null)
                return false;

            var getter = property.GetMethod;
            if (getter != null)
                VisitMethodDecl(getter);

            var setter = property.SetMethod;
            if (setter != null)
                VisitMethodDecl(setter);

            return true;
        }

        public override bool VisitFieldDecl(Field field)
        {
            // Ignore fields since they're converted to properties (getter/setter pairs).
            return true;
        }
    }
}
