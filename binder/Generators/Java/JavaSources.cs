using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;

namespace MonoEmbeddinator4000.Generators
{
    public class JavaSources : CSharpSources
    {
        public JavaSources(BindingContext context, Declaration decl)
            : base(context, new List<TranslationUnit> { decl.TranslationUnit })
        {
            Declaration = decl;
            TypePrinter = new JavaTypePrinter(context);
        }

        public Declaration Declaration;

        public override string FileExtension => "java";

        static IEnumerable<string> GetPackageNames(Declaration decl)
        {
            var namespaces = Declaration.GatherNamespaces(decl.Namespace)
                .ToList();
            namespaces.Remove(namespaces.First());

            return namespaces.Select(n => n.Name.ToLowerInvariant());
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

        public override void Process()
        {
            GenerateFilePreamble(CommentKind.JavaDoc);

            GenerateJavaPackage(Declaration);

            PushBlock();
            Declaration.Visit(this);
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateJavaPackage(Declaration decl)
        {
            var package = string.Join(".", GetPackageNames(decl));
            if (!string.IsNullOrWhiteSpace(package))
                WriteLine($"package {package};");
        }

        public override bool VisitDeclContext(DeclarationContext context)
        {
            foreach (var decl in context.Declarations)
                decl.Visit(this);

            return true;
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (@enum.IsIncomplete)
                return true;

            PushBlock(CSharpBlockKind.Enum);
            GenerateDeclarationCommon(@enum);

            //Write(Helpers.GetAccess(@enum.Access));
            Write("enum {0}", SafeIdentifier(@enum.Name));

            //var typeName = TypePrinter.VisitPrimitiveType(@enum.BuiltinType.Type,
            //                                              new TypeQualifiers());

            //if (@enum.BuiltinType.Type != PrimitiveType.Int)
            //    Write(" : {0}", typeName);

            NewLine();

            WriteStartBraceIndent();
            GenerateEnumItems(@enum);

            NewLine();
            WriteLine("private final int id;");
            WriteLine($"{@enum.Name}(int id) {{ this.id = id; }}");
            WriteLine("public int getValue() { return id; }");

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public override void GenerateEnumItems(Enumeration @enum)
        {
            base.GenerateEnumItems(@enum);
            WriteLine(";");
        }

        public override bool VisitEnumItemDecl(Enumeration.Item item)
        {
            if (item.Comment != null)
                GenerateInlineSummary(item.Comment);

            Write(item.Name);

            var @enum = item.Namespace as Enumeration;
            if (item.ExplicitValue)
                Write("({0})", @enum.GetItemValueAsString(item));

            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            VisitDeclContext(@class);

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            return true;
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
    }
}
