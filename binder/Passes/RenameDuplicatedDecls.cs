using CppSharp;
using CppSharp.AST;
using CppSharp.Passes;
using System.Linq;

namespace MonoManagedToNative.Passes
{
    public class RenameDuplicatedDeclsPass : TranslationUnitPass
    {
        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            var functions = @class.Functions.ToList();

            foreach (var func in functions)
            {
                var duplicates = functions.FindAll (f => func.Name == f.Name);
                duplicates.Remove (func);

                if (duplicates.Count == 0)
                    continue;
                
                for (int i = 0; i < duplicates.Count; ++i)
                {
                    var duplicate = duplicates[i];
                    duplicate.Name = string.Format("{0}_{1}", duplicate.Name, i+1);
                    Diagnostics.Debug("Renamed {0}", duplicate.QualifiedName);
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
