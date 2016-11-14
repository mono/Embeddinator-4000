using CppSharp.AST;
using CppSharp.Passes;

namespace MonoEmbeddinator4000.Passes
{
    public class FixMethodParametersPass : TranslationUnitPass
    {
        void AddObjectParameterToMethod(Method method, Class @class)
        {
            var ptrType = new QualifiedType(
                new PointerType(new QualifiedType(new TagType(@class))));

            var param = new Parameter
            {
                Name = "object",
                Namespace = @class,
                QualifiedType = ptrType,
                IsImplicit = true
            };

            method.Parameters.Insert(0, param);
        }

        bool ShouldReplaceType(QualifiedType type, out QualifiedType replacementType)
        {
            replacementType = new QualifiedType();

            var tag = type.Type as TagType;
            if (tag == null)
                return false;

            var @class = tag.Declaration as Class;
            if (@class == null)
                return false;

            replacementType = new QualifiedType(
                new PointerType(new QualifiedType(new TagType(@class))));

            return !@class.IsValueType;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!VisitDeclaration(method))
                return false;

            var @class = method.Namespace as Class;

            QualifiedType replacementType;

            if (ShouldReplaceType(method.ReturnType, out replacementType))
                method.ReturnType = replacementType;

            foreach (var param in method.Parameters)
            {
                if (ShouldReplaceType(param.QualifiedType, out replacementType))
                    param.QualifiedType = replacementType;
            }

            if (@class.IsStatic || method.IsStatic)
                return false;

            if (method.IsConstructor)
                return false;

            AddObjectParameterToMethod(method, @class);

            return true;
        }
    }
}
