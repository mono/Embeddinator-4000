import static org.junit.Assert.*;
import org.junit.*;

public class Tests {
    private static boolean doublesAreEqual(double value1, double value2) {
        return Double.doubleToLongBits(value1) == Double.doubleToLongBits(value2);
    }

    @Test
    public void testTypes() {
        assertEquals(Byte.MIN_VALUE, Type_SByte.getMin());
        assertEquals(Byte.MAX_VALUE, Type_SByte.getMax());

        assertEquals(Short.MIN_VALUE, Type_Int16.getMin());
        assertEquals(Short.MAX_VALUE, Type_Int16.getMax());

        assertEquals(Integer.MIN_VALUE, Type_Int32.getMin());
        assertEquals(Integer.MAX_VALUE, Type_Int32.getMax());

        assertEquals(Long.MIN_VALUE, Type_Int64.getMin());
        assertEquals(Long.MAX_VALUE, Type_Int64.getMax());

        assertEquals(0, Type_Byte.getMin());
        assertEquals(Math.pow(2, Byte.SIZE) - 1, Type_Byte.getMax());

        assertEquals(0, Type_UInt16.getMin());
        assertEquals(Math.pow(2, Short.SIZE) - 1, Type_UInt16.getMax());

        assertEquals(0, Type_UInt32.getMin());
        assertEquals(Math.pow(2, Integer.SIZE) - 1, Type_UInt32.getMax());

        assertEquals(0, Type_UInt64.getMin());
        assertEquals(Math.pow(2, Long.SIZE) - 1, Type_UInt64.getMax());

        doublesAreEqual(-Float.MAX_VALUE, Type_Single.getMin());
        doublesAreEqual(Float.MAX_VALUE, Type_Single.getMax());

        doublesAreEqual(-Double.MAX_VALUE, Type_Double.getMin());
        doublesAreEqual(Double.MAX_VALUE, Type_Double.getMax());

        assertEquals(Character.MIN_VALUE, Type_Char.getMin());
        assertEquals(Character.MAX_VALUE, Type_Char.getMax());
        assertEquals(0, Type_Char.getZero());
    }
}
