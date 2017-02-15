using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Generators.CSharp;
using System.Linq;

namespace MonoEmbeddinator4000.Generators
{
    public class JavaTypePrinterContext : CSharpTypePrinterContext
    {

    }

    public class JavaTypePrinterResult : CSharpTypePrinterResult
    {

    }

    public class JavaTypePrinter : CSharpTypePrinter
    {
        public JavaTypePrinter(BindingContext context) : base(context)
        {
        }
    }

    public class JavaManagedToNativeTypePrinter : CManagedToNativeTypePrinter
    {

    }
}