using CppSharp;

namespace MonoEmbeddinator4000
{
    public class Options : DriverOptions
    {
        // If true, will use unmanaged->managed thunks to call managed methods.
        // In this mode the JIT will generate specialized wrappers for marshaling
        // which will be faster but also lead to higher memory consumption.
        public bool UseUnmanagedThunks;

        // If true, will generate support files alongside generated binding code.
        public bool GenerateSupportFiles = true;
    }
}
