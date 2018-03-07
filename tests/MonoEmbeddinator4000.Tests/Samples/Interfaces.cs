using System;

namespace Example
{
    public interface IBase
    {
        void Hello();
    }

    public interface IMore : IBase
    {
        void World();
    }

    public class MoreExplicit : IMore
    {
        void IBase.Hello() { }

        void IMore.World() { }
    }

    public interface IConflict
    {
        string TestProperty { get; }

        string TestField { get; }

        void Hello();
    }

    public class Conflicted : IBase, IConflict
    {
        void IBase.Hello() { }

        void IConflict.Hello() { }

        public void Hello() { }

        string IConflict.TestProperty
        {
            get { return "IConflict.TestProperty"; }
        }

        public static string TestProperty
        {
            get { return "TestProperty"; }
        }

        string IConflict.TestField
        {
            get { return "IConflict.TestField"; }
        }

        public static string TestField = "TestField";
    }
}
