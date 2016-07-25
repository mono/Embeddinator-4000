using IKVM.Reflection;
using System.Collections.Generic;

namespace MonoManagedToNative.Generators
{
    public enum GeneratorKind
    {
        C
    }

    /// <summary>
    /// Generators are the base class for each language backend.
    /// </summary>
    public abstract class Generator
    {
        public Driver Driver { get; private set; }

        protected Generator(Driver driver)
        {
            Driver = driver;
        }

        /// <summary>
        /// Generates the outputs for a given translation unit.
        /// </summary>
        public abstract List<Template> Generate(Assembly assembly);
    }
}
