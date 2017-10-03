﻿using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;

namespace Embeddinator.Generators
{
    public class MarshalContext
    {
        public MarshalContext(BindingContext context)
        {
            Context = context;
            SupportBefore = new TextGenerator();
            SupportAfter = new TextGenerator ();
            Return = new TextGenerator();
            MarshalVarPrefix = string.Empty;
        }

        public BindingContext Context { get; private set; }

        public MarshalPrinter<MarshalContext> MarshalToNative;

        public TextGenerator SupportBefore { get; private set; }
        public TextGenerator SupportAfter { get; private set; }
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

    public abstract class MarshalPrinter<T> : AstVisitor where T : MarshalContext
    {
        public T Context { get; private set; }

        protected MarshalPrinter(T ctx)
        {
            Context = ctx;
        }
    }
}

