using CppSharp.AST;
using CppSharp.Generators;
using System.Linq;

namespace MonoManagedToNative.Generators
{
    public class ObjCHeaders : CHeaders
    {
        public ObjCHeaders(BindingContext context, Options options,
            TranslationUnit unit)
            : base(context, options, unit)
        {
        }

        public override void Process()
        {
            base.Process();
        }
    }

}
