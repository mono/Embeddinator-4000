using MonoEmbeddinator4000.Generators;
using CppSharp.Generators;

namespace MonoEmbeddinator4000
{
    public class Options
    {
        public Options()
        {
            Project = new Project();
            GenerateSupportFiles = true;
        }

        public Project Project;

        // General options
        public bool ShowHelpText;
        public bool OutputDebug;

        // Parser options
        public bool IgnoreParseErrors;

        // Generator options
        public string LibraryName;
        public GeneratorKind Language;

        public string OutputNamespace;
        public string OutputDir;

        // If true, will use unmanaged->managed thunks to call managed methods.
        // In this mode the JIT will generate specialized wrappers for marshaling
        // which will be faster but also lead to higher memory consumption.
        public bool UseUnmanagedThunks;

        // If true, will generate support files alongside generated binding code.
        public bool GenerateSupportFiles;

        // If true, will try to compile the generated managed-to-native binding code.
        public bool CompileCode;

        // If true, will compile the generated as a shared library / DLL.
        public bool CompileSharedLibrary;
    }
}
