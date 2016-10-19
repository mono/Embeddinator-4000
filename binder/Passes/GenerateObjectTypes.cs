using CppSharp.AST;
using CppSharp.Passes;
using System.Collections.Generic;

namespace MonoManagedToNative.Passes
{
    public class GenerateObjectTypesPass : TranslationUnitPass
    {
        TranslationUnit TranslationUnit;

        List<TypedefDecl> Declarations;
        HashSet<Declaration> Tags;

        public GenerateObjectTypesPass()
        {
            Declarations = new List<TypedefDecl>();
            Tags = new HashSet<Declaration>();
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            TranslationUnit = unit;

            var ret = base.VisitTranslationUnit(unit);

            unit.Declarations.InsertRange(0, Declarations);

            Declarations.Clear();
            Tags.Clear();
            TranslationUnit = null;

            return ret;
        }

        public override bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            var @class = tag.Declaration as Class;

            if (@class == null)
                return false;

            if (Tags.Contains(@class))
                return false;

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
    }
}
