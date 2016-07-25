using MonoManagedToNative.Generators;

namespace MonoManagedToNative
{
    public class Options
    {
        public Options()
        {
            Project = new Project();
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
    }
}
