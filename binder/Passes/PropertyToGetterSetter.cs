using CppSharp;
using CppSharp.AST;
using CppSharp.Passes;

namespace MonoEmbeddinator4000.Passes
{
    /// <summary>
    /// This pass converts properties to getter/setter pairs.
    /// </summary>
    public class PropertyToGetterSetterPass : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

            return base.VisitClassDecl(@class);
        }

        public override bool VisitProperty(Property property)
        {
            if (!VisitDeclaration(property))
                return false;

            var @class = property.Namespace as Class;
            if (@class == null)
                return false;

            property.GenerationKind = GenerationKind.None;

            var getter = new Method
            {
                Name = $"get{property.Name}",
                Namespace = property.Namespace,
                ReturnType = property.QualifiedType,
                Access = property.Access,
                IsStatic = property.IsStatic
            };

            @class.Methods.Add(getter);

            var param = new Parameter
            {
                Name = "value",
                QualifiedType = property.QualifiedType,
            };

            var setter = new Method
            {
                Name = $"set{property.Name}",
                Namespace = property.Namespace,
                ReturnType = new QualifiedType(new BuiltinType(PrimitiveType.Void)),
                Access = property.Access,
                IsStatic = property.IsStatic
            };

            setter.Parameters.Add(param);

            @class.Methods.Add(setter);

            Diagnostics.Debug($"Getter/setter pair created from property: {property.QualifiedName}");

            return true;
        }
    }
}
