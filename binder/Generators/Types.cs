using CppSharp.AST;

namespace MonoEmbeddinator4000.Generators
{
    public class ManagedArrayType : DecayedType
    {
        public ArrayType Array { get { return Decayed.Type as ArrayType; } }
        public TypedefType Typedef { get { return Original.Type as TypedefType; } }

        public ManagedArrayType(ArrayType array, TypedefType typedef)
        {
            Decayed = new QualifiedType(array);
            Original = new QualifiedType(typedef);
        }
    }
}