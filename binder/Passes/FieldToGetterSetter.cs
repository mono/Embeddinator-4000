using CppSharp;
using CppSharp.AST;
using CppSharp.Passes;

namespace Embeddinator.Passes
{
    public class FieldToGetterSetterPropertyPass : TranslationUnitPass
    {
        public override bool VisitFieldDecl(Field field)
        {
            if (!VisitDeclaration(field))
                return false;

            if (field.Access == AccessSpecifier.Private)
                return false;

            if (field.IsImplicit)
                return false;

            if (!field.IsGenerated)
                return false;

            field.GenerationKind = GenerationKind.None;

            var @class = field.Namespace as Class;

            var getter = new Method
            {
                Name = $"get_{field.Name}",
                Namespace = @class,
                ReturnType = field.QualifiedType,
                Access = field.Access,
                Field = field,
                IsStatic = field.IsStatic,
            };

            var setter = new Method
            {
                Name = $"set_{field.Name}",
                Namespace = @class,
                ReturnType = new QualifiedType(new BuiltinType(PrimitiveType.Void)),
                Access = field.Access,
                Field = field,
                IsStatic = field.IsStatic,
            };

            var param = new Parameter
            {
                Name = "value",
                QualifiedType = field.QualifiedType,
            };
            setter.Parameters.Add(param);

            var property = new Property
            {
                Name = field.Name,
                Namespace = field.Namespace,
                GetMethod = getter,
                SetMethod = setter,
                Field = field,
                QualifiedType = field.QualifiedType
            };

            @class.Declarations.Add(property);

            Diagnostics.Debug($"Getter/setter property created from field {field.QualifiedName}");

            return true;
        }
    }
}
