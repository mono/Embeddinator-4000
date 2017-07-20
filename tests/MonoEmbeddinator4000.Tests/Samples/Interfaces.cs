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
        void Hello();
    }

    public class Conflicted : IBase, IConflict
    {
        void IBase.Hello() { }

        void IConflict.Hello() { }

        public void Hello() { }
    }
}
