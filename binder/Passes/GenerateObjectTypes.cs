using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.Utils;
using MonoEmbeddinator4000.Generators;

namespace MonoEmbeddinator4000.Passes
{
    public class GetReferencedDecls : TranslationUnitPass
    {
        public OrderedSet<Declaration> Declarations = new OrderedSet<Declaration>();
        public IEnumerable<Class> Classes => Declarations.OfType<Class>();
        public IEnumerable<Enumeration> Enums => Declarations.OfType<Enumeration>();

        protected TranslationUnit TranslationUnit;

        public GetReferencedDecls()
        {
            ClearVisitedDeclarations = true;
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            TranslationUnit = unit;
            var ret = base.VisitTranslationUnit(unit);

            return ret;
        }

        public override bool VisitDeclaration(Declaration decl)
        {
            if (AlreadyVisited(@decl))
                return false;

            if (Declarations.Contains(@decl))
                return false;

            if (decl == TranslationUnit)
                return true;

            if (decl is TranslationUnit)
                return false;

            if (decl is Class || decl is Enumeration || decl is TypedefDecl)
                Declarations.Add(decl);

            // No need to continue visiting after a declaration of another
            // translation unit is encountered.
            return decl.Namespace != null && decl.Namespace.TranslationUnit == TranslationUnit;
        }
    }

    public class GenerateObjectTypesPass : GetReferencedDecls
    {
        public List<TypedefDecl> Typedefs;

        public GenerateObjectTypesPass()
        {
            Typedefs = new List<TypedefDecl>();
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            var ret = base.VisitTranslationUnit(unit);

            foreach (var @class in Classes.Where(c => c.TranslationUnit == TranslationUnit))
                HandleClass(@class);

            unit.Declarations.InsertRange(0, Typedefs);

            Typedefs.Clear();
            Declarations.Clear();
            TranslationUnit = null;

            return ret;
        }

        void HandleClass(Class @class)
        {
            // If we are generating C, there are no classes, so for each C# class create a
            // struct representing the object with a typedef for the MonoEmbedObject type.

            // For other languages we generate a class in the target language, so generate a 
            // MonoEmbedObject field directly in the object representation.

            if (Options.GeneratorKind == GeneratorKind.C)
                CreateTypedefObjectForClass(@class);
            else
                AddObjectFieldsToClass(@class);
        }
        
        public static Class MonoEmbedObject = new Class { Name = "MonoEmbedObject" };

        void CreateTypedefObjectForClass(Class @class)
        {
            var typedef = new TypedefDecl
            {
                Name = @class.QualifiedName,
                Namespace = TranslationUnit,
                QualifiedType = new QualifiedType(new TagType(MonoEmbedObject))
            };

            Typedefs.Add(typedef);
        }

        void AddObjectFieldsToClass(Class @class)
        {
            var ptrType = new PointerType(new QualifiedType(new TagType(MonoEmbedObject)));

            var field = new Field
            {
                Name = CGenerator.ObjectInstanceId,
                QualifiedType = new QualifiedType(ptrType),
                Access = AccessSpecifier.Public,
                Namespace = @class,
                IsImplicit = true
            };

            @class.Fields.Add(field);
        }
    }
}
