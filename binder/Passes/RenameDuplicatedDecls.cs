using CppSharp;
using CppSharp.AST;
using CppSharp.Passes;
using System.Linq;

namespace Embeddinator.Passes
{
    public class RenameDuplicatedDeclsPass : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            var members = @class.Declarations.Where(d => d is Field || d is Function).ToList();

            foreach (var member in members)
            {
                var duplicates = members.FindAll(f => member.Name == f.Name || member.Name == "get_" + f.Name || member.Name == "set_" + f.Name);
                duplicates.Remove(member);

                if (duplicates.Count == 0)
                    continue;
                
                for (int i = 0; i < duplicates.Count; ++i)
                {
                    var duplicate = duplicates[i];
                    duplicate.Name = string.Format("{0}_{1}", duplicate.Name, i + 1);
                    Diagnostics.Debug("Renamed {0} {1}", duplicate.GetType().Name.ToLowerInvariant(), duplicate.QualifiedName);
                }
            }

            return true;
        }

        public override bool VisitParameterDecl(Parameter param)
        {
            return true;
        }
    }
}
