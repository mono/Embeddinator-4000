using System.Collections.Generic;
using CppSharp.AST;
using CppSharp.Generators;

namespace MonoEmbeddinator4000.Generators
{
    public class ObjCGenerator : CGenerator
    {
        public ObjCGenerator(BindingContext context, Options options)
            : base(context, options)
        {
        }

        public override List<Template> Generate(TranslationUnit unit)
        {
            var headers = new ObjCHeaders(Context, Options, unit);
            var sources = new ObjCSources(Context, Options, unit);

            return new List<Template> { headers, sources };
        }
    }

    public static class ObjCExtensions
    {
        public static void GenerateObjCMethodSignature(this CTemplate template,
            Method method)
        {
            var @class = method.Namespace as Class;
            var retType = method.ReturnType.Visit(template.CTypePrinter);

            template.Write("{0}", method.IsStatic ? "+" : "-");

            template.Write(" ({0}){1}", retType, method.Name);

            template.Write(template.CTypePrinter.VisitParameters(method.Parameters));
        }
    }
}
