using IKVM.Reflection;
using CppSharp;
using CppSharp.Generators;

namespace MonoEmbeddinator4000.Generators
{
    public abstract class Template : BlockGenerator
    {
        public BindingContext Context { get; private set; }
        public Options Options { get; private set; }

        protected Template(BindingContext context, Options options)
        {
            Context = context;
            Options = options;
        }

        public abstract string Name { get; }

        public abstract string FileExtension { get; }

        public abstract void Process();

        public abstract void WriteHeaders();
    }
}
