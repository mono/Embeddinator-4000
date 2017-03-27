using CppSharp;
using CppSharp.AST;
using CppSharp.Passes;

namespace MonoEmbeddinator4000.Passes
{
    public class FieldToGetterSetterPass : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (@class.CompleteDeclaration != null)
                return VisitClassDecl(@class.CompleteDeclaration as Class);

            return base.VisitClassDecl(@class);
        }

        public override bool VisitFieldDecl(Field field)
        {
            if (!VisitDeclaration(field))
                return false;

            var @class = field.Namespace as Class;
            if (@class == null)
                return false;

            if (field.Access == AccessSpecifier.Private)
                return true;

            // Check if we already have a synthetized getter/setter.
            //var existing = @class.Methods.FirstOrDefault(
            //    method => method.Parameters.SingleOrDefault(
            //        (Parameter p) =>
            //    {
            //        Class paramClass;
            //        if(!p.Type.TryGetClass(out paramClass))
            //            return false;

            //        return paramClass == @class;
            //    }));

            //if (existing != null)
            //    return false;

            field.GenerationKind = GenerationKind.None;

            var getter = new Method
            {
                Name = string.Format("get{0}", field.Name),
                Namespace = @class,
                ReturnType = field.QualifiedType,
                Access = field.Access
            };

            @class.Methods.Add(getter);

            var setter = new Method
            {
                Name = string.Format("set{0}", field.Name),
                Namespace = @class,
                Access = field.Access,
                
            };

            var param = new Parameter
            {
                Name = "value",
                QualifiedType = field.QualifiedType
            };
            setter.Parameters.Add(param);

            @class.Methods.Add(setter);

            Diagnostics.Debug("Getter/setter created from field: {0}::{1}",
                @class.QualifiedOriginalName, field.Name);

            return false;
        }
    }
}
