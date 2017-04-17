using CppSharp;
using CppSharp.AST;
using CppSharp.Passes;

namespace MonoEmbeddinator4000.Passes
{
    public class RenameEnumItemsPass : TranslationUnitPass
    {
        public override bool VisitEnumDecl(Enumeration @enum)
        {
            if (!VisitDeclaration(@enum))
                return false;

            foreach (var item in @enum.Items)
                item.Name = string.Format("{0}_{1}", @enum.QualifiedName, item.Name);

            @enum.Name = @enum.QualifiedName;

            return true;
        }

        public override bool VisitParameterDecl(Parameter param)
        {
            return true;
        }
    }
}
