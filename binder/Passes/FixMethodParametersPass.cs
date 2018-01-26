using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using System.Linq;

namespace Embeddinator.Passes
{
    public class FixMethodParametersPass : TranslationUnitPass
    {
        public static string ObjectParameterId => "object";

        public FixMethodParametersPass()
        {
            VisitOptions.VisitPropertyAccessors = true;
        }

        void AddObjectParameterToMethod(Method method, Class @class)
        {
            var ptrType = new QualifiedType(
                new PointerType(new QualifiedType(new TagType(@class))));

            var objectId = ObjectParameterId;

            // Check if the method already provides a parameter named "object"
            if (method.Parameters.Any(p => p.Name == ObjectParameterId))
                objectId = $"__{objectId}";

            var param = new Parameter
            {
                Name = objectId,
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

            replacementType = new QualifiedType(new PointerType(type));

            return true;
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!VisitDeclaration(method))
                return false;

            QualifiedType replacementType;
            if (ShouldReplaceType(method.ReturnType, out replacementType))
                method.ReturnType = replacementType;

            foreach (var param in method.Parameters)
            {
                if (ShouldReplaceType(param.QualifiedType, out replacementType))
                    param.QualifiedType = replacementType;
            }

            var @class = method.Namespace as Class;
            if (@class.IsStatic || method.IsStatic)
                return false;

            if (method.IsConstructor)
                return false;

            var field = method.AssociatedDeclaration as Field;
            var isStaticField = field != null && field.IsStatic;
            if (Options.GeneratorKind == GeneratorKind.C && !isStaticField)
                AddObjectParameterToMethod(method, @class);

            return true;
        }
    }
}
