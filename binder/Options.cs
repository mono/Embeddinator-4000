using System.Collections.Generic;
using CppSharp;
using CppSharp.Generators;

namespace Embeddinator
{
    public class Options : DriverOptions
    {
        /// <summary>
        /// The list of generators that will be targeted.
        /// </summary>
        public IEnumerable<GeneratorKind> GeneratorKinds = new List<GeneratorKind>();

        /// <summary>
        /// The name of the library to be bound.
        /// </summary>
        public string LibraryName;

        // If true, will use unmanaged->managed thunks to call managed methods.
        // In this mode the JIT will generate specialized wrappers for marshaling
        // which will be faster but also lead to higher memory consumption.
        public bool UseUnmanagedThunks;

        // If true, will generate support files alongside generated binding code.
        public bool GenerateSupportFiles = true;
    }
}
