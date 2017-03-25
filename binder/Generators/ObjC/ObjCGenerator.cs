using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;
using CppSharp.Generators;

namespace MonoEmbeddinator4000.Generators
{
    public class ObjCGenerator : CGenerator
    {
        public override CManagedToNativeTypePrinter TypePrinter =>
            GetCTypePrinter(GeneratorKind.ObjectiveC);

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
        public static bool IsProperty (this Method method)
        {
            if (method.Name.StartsWith ("get_", StringComparison.Ordinal))
                return method.Parameters.Count == 0;
            if (method.Name.StartsWith ("set_", StringComparison.Ordinal))
                return method.Parameters.Count == 1;
            return false;
        }

        public static bool IsProperty (this Method method, out bool readOnly)
        {
            readOnly = false;
            if (!method.IsProperty ())
                return false;

            readOnly = true;
            var name = method.Name;
            var setter = "set_" + name.Substring (4, name.Length - 4);
            foreach (var candidate in method.Namespace.Declarations) {
                if (candidate.Name == setter) {
                    readOnly = false;
                    break;
                }
            }
            return true;
        }

        public static void GenerateObjCMethodSignature(this CCodeGenerator gen, Method method, bool headers)
        {
            var name = method.Name;
            var retType = method.ReturnType.Visit (gen.CTypePrinter);

            bool is_readonly;
            bool is_property = method.IsProperty (out is_readonly);
            if (is_property) {
                // skip setters for headers, it's already dealt with the getters
                if (headers && method.Name.StartsWith ("set_", StringComparison.Ordinal))
                    return;
                // by convention ObjC is camel cased
                name = Char.ToLowerInvariant (name [4]) + name.Substring (5, name.Length - 5);
            }

            if (headers) {
               if (is_property) {
                    gen.Write ("@property (nonatomic");
                    if (method.IsStatic)
                        gen.Write (", class");
                    if (is_readonly)
                        gen.Write (", readonly");
                    gen.Write (") {0} {1}", retType, name);
                } else {
                    gen.Write (method.IsStatic ? "+" : "-");
                    gen.Write (" ({0}){1}", retType, name);
                }
                gen.Write (gen.CTypePrinter.VisitParameters (method.Parameters));
            } else {
                gen.Write (method.IsStatic ? "+" : "-");
                gen.Write (" ({0}){1}", retType, name);
                gen.Write (gen.CTypePrinter.VisitParameters (method.Parameters));
            }
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
