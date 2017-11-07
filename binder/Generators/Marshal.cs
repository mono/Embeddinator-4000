using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;

namespace Embeddinator.Generators
{
    public class MarshalPrinter : AstVisitor
    {
        public MarshalPrinter(BindingContext context)
        {
            Context = context;
            Before = new TextGenerator();
            After = new TextGenerator ();
            Return = new TextGenerator();
            MarshalVarPrefix = string.Empty;
        }

        public BindingContext Context { get; private set; }

        public MarshalPrinter<MarshalContext> MarshalToNative;

        public TextGenerator Before { get; private set; }
        public TextGenerator After { get; private set; }
        public TextGenerator Return { get; private set; }

        public Declaration Declaration { get; set; }

        public string ReturnVarName { get; set; }
        public QualifiedType ReturnType { get; set; }

        public string ArgName { get; set; }
        public Parameter Parameter { get; set; }
        public int ParameterIndex { get; set; }
        public Function Function { get; set; }

        public string MarshalVarPrefix { get; set; }
    }
}

