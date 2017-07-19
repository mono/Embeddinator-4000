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
}
