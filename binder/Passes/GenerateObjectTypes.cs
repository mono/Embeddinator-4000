using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.Utils;

namespace MonoEmbeddinator4000.Passes
{
    public class GetReferencedClasses : TranslationUnitPass
    {
        public OrderedSet<Class> Classes = new OrderedSet<Class>();

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            return base.VisitTranslationUnit(unit);
        }

        public override bool VisitClassDecl(Class @class)
        {
            // Check if we already handled this class.
            if (Classes.Contains(@class))
                return false;

            Classes.Add(@class);

            return base.VisitClassDecl(@class);
        }

        public override bool VisitTagType(TagType tag, TypeQualifiers quals)
        {
            var @class = tag.Declaration as Class;

            if (@class == null)
                return false;

            return VisitClassDecl(@class);
        }
    }

    public class GenerateObjectTypesPass : GetReferencedClasses
    {
        TranslationUnit TranslationUnit;

        public List<TypedefDecl> Declarations;
        
        public GenerateObjectTypesPass()
        {
            Declarations = new List<TypedefDecl>();
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            TranslationUnit = unit;
            var ret = base.VisitTranslationUnit(unit);

            foreach (var @class in Classes)
                HandleClass(@class);

            unit.Declarations.InsertRange(0, Declarations);
            Declarations.Clear();
            
            TranslationUnit = null;
            Classes.Clear();

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

            Declarations.Add(typedef);
        }

        void AddObjectFieldsToClass(Class @class)
        {
            var ptrType = new PointerType(new QualifiedType(new TagType(MonoEmbedObject)));

            var field = new Field
            {
                Name = "_object",
                QualifiedType = new QualifiedType(ptrType),
                Access = AccessSpecifier.Internal,
                Namespace = @class
            };

            @class.Fields.Add(field);
        }
    }
}
