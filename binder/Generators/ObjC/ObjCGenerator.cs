using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;

namespace MonoEmbeddinator4000.Generators
{
    public class ObjCGenerator : CGenerator
    {
        public ObjCGenerator(BindingContext context)
            : base(context)
        {
        }

        public override List<CodeGenerator> Generate(IEnumerable<TranslationUnit> units)
        {
            var unit = units.First();
            var headers = new ObjCHeaders(Context, unit);
            var sources = new ObjCSources(Context, unit);

            return new List<CodeGenerator> { headers, sources };
        }
    }

    public static class ObjCExtensions
    {
        public static void GenerateObjCMethodSignature(this CCodeGenerator gen,
            Method method)
        {
            gen.Write("{0}", method.IsStatic ? "+" : "-");

            var retType = method.ReturnType.Visit(gen.CTypePrinter);
            gen.Write(" ({0}){1}", retType, method.Name);

            gen.Write(gen.CTypePrinter.VisitParameters(method.Parameters));
        }

        public static string GetObjCAccessKeyword(AccessSpecifier access)
        {
            switch (access)
            {
            case AccessSpecifier.Private:
                return "@private";
            case AccessSpecifier.Protected:
                return "@protected";
            case AccessSpecifier.Public:
                return "@public";
            case AccessSpecifier.Internal:
                throw new Exception($"Unmappable Objective-C access specifier: {access}");
            }

            throw new NotSupportedException();
        }

        public static bool GenerateObjCField(this CCodeGenerator gen, Field field)
        {
            gen.WriteLine($"{GetObjCAccessKeyword(field.Access)} {field.Type} {field.Name};");
            return true;
        }
    }
}
