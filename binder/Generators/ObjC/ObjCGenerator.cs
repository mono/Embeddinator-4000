using System;
using System.Collections.Generic;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;

namespace MonoManagedToNative.Generators
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
}
