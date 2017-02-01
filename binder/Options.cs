using CppSharp;

namespace MonoEmbeddinator4000
{
    public class Options : DriverOptions
    {
        public Options()
        {
            Project = new Project();
        }

        public Project Project;
    }
}
