using MonoEmbeddinator4000.Generators;
using CppSharp.Generators;

namespace MonoEmbeddinator4000.Tests
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
