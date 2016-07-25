using IKVM.Reflection;

namespace MonoManagedToNative.Generators
{
    public abstract class Template : BlockGenerator
    {
        public Driver Driver { get; private set; }
        public Options Options { get; private set; }

        public IDiagnostics Diagnostics
        {
            get { return Driver.Diagnostics; }
        }

        public Assembly Assembly { get; set; }

        protected Template(Driver driver)
        {
            Driver = driver;
            Options = driver.Options;
        }

        public abstract string Name { get; }

        public abstract string FileExtension { get; }

        public abstract void Process();
    }
}
