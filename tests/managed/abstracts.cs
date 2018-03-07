namespace Abstracts
{
    public abstract class AbstractClass : Interfaces.IMakeItUp {

        bool result;

        public static bool ProcessStaticAbstract(AbstractClass @abstract)
        {
            return @abstract.AbstractMethod();
        }

        public abstract bool AbstractMethod();

        public bool Boolean {
            get {
                result = !result;
                return result;
            }
        }

        public string Convert (int integer)
        {
            return integer.ToString ();
        }

        // overload
        public string Convert (long longint)
        {
            return longint.ToString ();
        }
    }

    public class ConcreteAbstractClass : AbstractClass
    {
        public override bool AbstractMethod()
        {
            return true;
        }

        public static AbstractClass Create()
        {
            return new ConcreteAbstractClass();
        }
    }
}
