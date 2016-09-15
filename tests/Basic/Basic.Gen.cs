using MonoManagedToNative.Generators;
using CppSharp.Generators;

namespace MonoManagedToNative.Tests
{
    public class BasicTestsGenerator : TestsGenerator
    {
        public BasicTestsGenerator(GeneratorKind kind)
            : base("Basic", kind)
        {
        }

        public static void Main(string[] args)
        {
            new BasicTestsGenerator(GeneratorKind.C).Generate();
        }
    }
}
