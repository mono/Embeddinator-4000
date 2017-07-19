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
}
