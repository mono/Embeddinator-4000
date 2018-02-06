using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;

namespace Embeddinator.Generators
{
    public class Marshaler : AstVisitor
    {
        public Marshaler(BindingContext context)
        {
            Context = context;
            Before = new TextGenerator();
            After = new TextGenerator();
            Return = new TextGenerator();
        }

        public BindingContext Context { get; private set; }

        public TextGenerator Before { get; private set; }
        public TextGenerator After { get; private set; }
        public TextGenerator Return { get; private set; }

        public string ReturnVarName { get; set; }
        public QualifiedType ReturnType { get; set; }

        public string ArgName { get; set; }
        public Parameter Parameter { get; set; }
        public int ParameterIndex { get; set; }
    }
}

