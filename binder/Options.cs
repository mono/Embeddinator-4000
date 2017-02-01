using CppSharp;
using CppSharp.Generators;

namespace MonoEmbeddinator4000
{
    public enum CompilationTarget
    {
        SharedLibrary,
        StaticLibrary,
        Application
    }

    public class Options : DriverOptions
    {
        public Options()
        {
            Project = new Project();
            GenerateSupportFiles = true;
        }

        public Project Project;

        // Generator options
        public GeneratorKind Language;

        public TargetPlatform Platform;

        /// <summary>
        /// Specifies the VS version.
        /// </summary>
        /// <remarks>When null, latest is used.</remarks>
        public VisualStudioVersion VsVersion;

        // If code compilation is enabled, then sets the compilation target.
        public CompilationTarget Target;

        // If true, will force the generation of debug metadata for the native
        // and managed code.
        public bool DebugMode;

        // If true, will use unmanaged->managed thunks to call managed methods.
        // In this mode the JIT will generate specialized wrappers for marshaling
        // which will be faster but also lead to higher memory consumption.
        public bool UseUnmanagedThunks;

        // If true, will generate support files alongside generated binding code.
        public bool GenerateSupportFiles;

        // If true, will compile the generated as a shared library / DLL.
        public bool CompileSharedLibrary => Target == CompilationTarget.SharedLibrary;
    }
}
