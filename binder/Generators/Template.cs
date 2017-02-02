using CppSharp.Generators;

namespace MonoEmbeddinator4000.Generators
{
    public abstract class Template : CppSharp.Generators.Template
    {
        protected Template(BindingContext context)
            : base(context, null)
        {
        }

        public abstract string Name { get; }

        public abstract void WriteHeaders();
    }
}
