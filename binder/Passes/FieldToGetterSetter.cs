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

            var property = new Property
            {
                Name = field.Name,
                Namespace = field.Namespace,
                Field = field,
                QualifiedType = field.QualifiedType,
                AssociatedDeclaration = field
            };

            var getter = new Method
            {
                Name = $"get_{field.Name}",
                Namespace = @class,
                ReturnType = field.QualifiedType,
                Access = field.Access,
                AssociatedDeclaration = property,
                IsStatic = field.IsStatic,
                SynthKind = FunctionSynthKind.FieldAcessor
            };
            property.GetMethod = getter;

            var setter = new Method
            {
                Name = $"set_{field.Name}",
                Namespace = @class,
                ReturnType = new QualifiedType(new BuiltinType(PrimitiveType.Void)),
                Access = field.Access,
                AssociatedDeclaration = property,
                IsStatic = field.IsStatic,
                SynthKind = FunctionSynthKind.FieldAcessor
            };
            property.SetMethod = setter;

            var param = new Parameter
            {
                Name = "value",
                QualifiedType = field.QualifiedType,
            };
            setter.Parameters.Add(param);

            @class.Declarations.Add(property);

            Diagnostics.Debug($"Property created from field: '{field.QualifiedName}'");

            return true;
        }
    }
}
