using CppSharp.AST;
using CppSharp.Generators;
using System.Collections.Generic;

namespace MonoManagedToNative.Generators
{
    /// <summary>
    /// Generators are the base class for each language backend.
    /// </summary>
    public abstract class Generator
    {
        public BindingContext Context { get; private set; }

        protected Generator(BindingContext context)
        {
            Context = context;
        }

        /// <summary>
        /// Generates the outputs for a given translation unit.
        /// </summary>
        public abstract List<Template> Generate(TranslationUnit unit);
    }
}
