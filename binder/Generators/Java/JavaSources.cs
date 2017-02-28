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

            PushBlock(BlockKind.Enum);
            GenerateDeclarationCommon(@enum);

            Write(AccessIdentifier(@enum.Access));
            Write("enum {0} ", SafeIdentifier(@enum.Name));

            WriteStartBraceIndent();
            GenerateEnumItems(@enum);

            NewLine();

            var typeName = TypePrinter.VisitPrimitiveType(@enum.BuiltinType.Type,
                                                          new TypeQualifiers());
            WriteLine($"private final {typeName} id;");
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

        public override void GenerateClassSpecifier(Class @class)
        {
            var keywords = new List<string>();
            
            keywords.Add(@class.Access == AccessSpecifier.Protected ? "protected internal" : "public");

            if (@class.IsAbstract)
                keywords.Add("abstract");

            if (@class.IsStatic)
                keywords.Add("static");

            keywords.Add(@class.IsInterface ? "interface" : "class");
            keywords.Add(SafeIdentifier(@class.Name));

            Write(string.Join(" ", keywords));

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
            VisitDeclContext(@class);
            WriteCloseBraceIndent();

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
