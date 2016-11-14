using CppSharp.AST;
using CppSharp.Passes;
using System.Collections.Generic;

namespace MonoEmbeddinator4000.Passes
{
    public class GenerateObjectTypesPass : TranslationUnitPass
    {
        TranslationUnit TranslationUnit;

        public List<TypedefDecl> Declarations;
        public List<Class> Classes;

        HashSet<Declaration> Tags;

        public GenerateObjectTypesPass()
        {
            Declarations = new List<TypedefDecl>();
            Classes = new List<Class>();
            Tags = new HashSet<Declaration>();
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            TranslationUnit = unit;

            var ret = base.VisitTranslationUnit(unit);

            unit.Declarations.InsertRange(0, Declarations);

            Declarations.Clear();
            Classes.Clear();
            Tags.Clear();
            TranslationUnit = null;

            return ret;
        }

        bool HandleClass(Class @class)
        {
            if (Tags.Contains(@class))
                return false;

            Classes.Add(@class);
            Tags.Add(@class);

            var monoObjectType = new Class { Name = "MonoEmbedObject" };

            var typedef = new TypedefDecl
            {
                Name = @class.QualifiedName,
                Namespace = TranslationUnit,
                QualifiedType = new QualifiedType(new TagType(monoObjectType))
            };

            Declarations.Add(typedef);

            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            HandleClass(@class);
            return base.VisitClassDecl(@class);
        }

        public override bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            var @class = tag.Declaration as Class;

            if (@class == null)
                return false;

            return HandleClass(@class);
        }
    }
}
