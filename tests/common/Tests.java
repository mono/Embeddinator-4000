import managed_dll.*;
import managed_dll.properties.*;
import managed_dll.first.*;
import managed_dll.first.second.*;
import managed_dll.exceptions.*;

import mono.embeddinator.*;

import static org.junit.Assert.*;
import org.junit.*;

public class Tests {
    private static boolean doublesAreEqual(double value1, double value2) {
        return Double.doubleToLongBits(value1) == Double.doubleToLongBits(value2);
    }

    @Test
    public void testProperties() {
        assertFalse(Platform.getIsWindows());

        Platform.setExitCode(255);
        assertEquals(Platform.getExitCode(), 255);

        assertEquals(Query.getUniversalAnswer(), 42);

        Query query = new Query();
        assertTrue(query.getIsGood());
        assertFalse(query.getIsBad());
        assertEquals(query.getAnswer(), 42);
        query.setAnswer(911);
        assertEquals(query.getAnswer(), 911);

        assertFalse(query.getIsSecret());
        query.setSecret(1);
        assertTrue(query.getIsSecret());
    }

    @Test
    public void testNamespaces() {
        ClassWithoutNamespace nonamespace = new ClassWithoutNamespace();
        assertEquals(nonamespace.toString(), "ClassWithoutNamespace");

        ClassWithSingleNamespace singlenamespace = new ClassWithSingleNamespace();
        assertEquals(singlenamespace.toString(), "First.ClassWithSingleNamespace");

        ClassWithNestedNamespace nestednamespaces = new ClassWithNestedNamespace();
        assertEquals(nestednamespaces.toString(), "First.Second.ClassWithNestedNamespace");

        managed_dll.first.second.third.ClassWithNestedNamespace nestednamespaces2 = new managed_dll.first.second.third.ClassWithNestedNamespace();
        assertEquals(nestednamespaces2.toString(), "First.Second.Third.ClassWithNestedNamespace");
    }

    @Test
    public void testExceptions() {
        Throwable e = null;

        try {
            Throwers throwers = new Throwers();
        } catch(Exception ex) {
            e = ex;
        }
        assertTrue(e instanceof mono.embeddinator.RuntimeException);

        e = null;
        try {
            ThrowInStaticCtor static_thrower = new ThrowInStaticCtor();
        } catch(Exception ex) {
            e = ex;
        }
        assertTrue(e instanceof mono.embeddinator.RuntimeException);

        try {
            Super sup1 = new Super(false);
        } catch(Exception ex) {
            fail();
        }

        e = null;
        try {
            Super sup2 = new Super(true);
        } catch(Exception ex) {
            e = ex;
        }
        assertTrue(e instanceof mono.embeddinator.RuntimeException);
    }

    @Test
    static long UCHAR_MAX = (long)(Math.pow(2, Byte.SIZE) - 1);
    static long USHRT_MAX = (long)(Math.pow(2, Short.SIZE) - 1);
    static long UINT_MAX = (long)(Math.pow(2, Integer.SIZE) - 1);
    static long ULONG_MAX = (long)(Math.pow(2, Long.SIZE) - 1);

    public void testTypes() {
        assertEquals(Byte.MIN_VALUE, Type_SByte.getMin());
        assertEquals(Byte.MAX_VALUE, Type_SByte.getMax());

        assertEquals(Short.MIN_VALUE, Type_Int16.getMin());
        assertEquals(Short.MAX_VALUE, Type_Int16.getMax());

        assertEquals(Integer.MIN_VALUE, Type_Int32.getMin());
        assertEquals(Integer.MAX_VALUE, Type_Int32.getMax());

        assertEquals(Long.MIN_VALUE, Type_Int64.getMin());
        assertEquals(Long.MAX_VALUE, Type_Int64.getMax());

        assertEquals(0, Type_Byte.getMin().longValue());
        assertEquals(UCHAR_MAX, Type_Byte.getMax().longValue());

        assertEquals(0, Type_UInt16.getMin().longValue());
        assertEquals(USHRT_MAX, Type_UInt16.getMax().longValue());

        assertEquals(0, Type_UInt32.getMin().longValue());
        assertEquals(UINT_MAX, Type_UInt32.getMax().longValue());

        assertEquals(0, Type_UInt64.getMin().longValue());
        // TODO: Use BigDecimal for unsigned 64-bit integer types.
        //assertEquals(ULONG_MAX, Type_UInt64.getMax().longValue());

        doublesAreEqual(-Float.MAX_VALUE, Type_Single.getMin());
        doublesAreEqual(Float.MAX_VALUE, Type_Single.getMax());

        doublesAreEqual(-Double.MAX_VALUE, Type_Double.getMin());
        doublesAreEqual(Double.MAX_VALUE, Type_Double.getMax());

        assertEquals(Character.MIN_VALUE, Type_Char.getMin());
        assertEquals(Character.MAX_VALUE, Type_Char.getMax());
        assertEquals(0, Type_Char.getZero());
    }
}
