import managed_dll.*;
import managed_dll.properties.*;
import managed_dll.first.*;
import managed_dll.first.second.*;
import managed_dll.exceptions.*;
import managed_dll.constructors.*;
import managed_dll.enums.*;
import managed_dll.methods.*;

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
    public void testConstructors() {
        Unique unique = new Unique();
        assertEquals(1, unique.getId());

        Unique unique_init_id = new Unique(911);
        assertEquals(911, unique_init_id.getId());

        SuperUnique super_unique_default_init = new SuperUnique();
        assertEquals(411, super_unique_default_init.getId());

        Implicit implicit = new Implicit();
        assertEquals("OK", implicit.getTestResult());

        AllTypeCode all1 = new AllTypeCode(true, (char)USHRT_MAX, "Mono");
        assertTrue(all1.getTestResult());

        AllTypeCode all2 = new AllTypeCode(Byte.MAX_VALUE, Short.MAX_VALUE, Integer.MAX_VALUE, Long.MAX_VALUE);
        assertTrue(all2.getTestResult());

        // TODO: Use BigDecimal for unsigned 64-bit integer types.
        //AllTypeCode all3 = new AllTypeCode(new UnsignedByte(UCHAR_MAX), new UnsignedShort(USHRT_MAX),
        //    new UnsignedInt(UINT_MAX), new UnsignedLong(ULONG_MAX));
        //assertTrue(all3.getTestResult());

        AllTypeCode all4 = new AllTypeCode(Float.MAX_VALUE, Double.MAX_VALUE);
        assertTrue(all4.getTestResult());
    }

    @Test
    public void testMethods() {
        Static static_method = Static.create(1);
        assertEquals(1, static_method.getId());

        assertEquals(null, Parameters.concat(null, null));
        assertEquals("first", Parameters.concat("first", null));
        assertEquals("second", Parameters.concat(null, "second"));
        assertEquals("firstsecond", Parameters.concat("first", "second"));

        Ref<Boolean> b = new Ref<Boolean>(true);
        Ref<String> s = new Ref<String>(null);
        Parameters.ref(b, s);
        assertFalse(b.get());
        assertEquals("hello", s.get());

        Parameters.ref(b, s);
        assertTrue(b.get());
        assertEquals(null, s.get());

        Out<Integer> l = new Out<Integer>();
        Out<String> os = new Out<String>();
        Parameters.out(null, l, os);
        assertEquals(new Integer(0), l.get());
        assertEquals(null, os.get());

        Parameters.out("Xamarin", l, os);
        assertEquals(new Integer(7), l.get());
        assertEquals("XAMARIN", os.get());
    }

    @Test
    public void testEnums() {
        Ref<IntEnum> i = new Ref<IntEnum>(IntEnum.Min);
        Out<ShortEnum> s = new Out<ShortEnum>(ShortEnum.Min);
        ByteFlags f = Enumer.test(ByteEnum.Max, i, s);
        assertEquals(0x22, f.getValue());
        assertEquals(IntEnum.Max, i.get());
        assertEquals(ShortEnum.Max, s.get());

        f = Enumer.test(ByteEnum.Zero, i, s);
        assertEquals(IntEnum.Min, i.get());
        assertEquals(ShortEnum.Min, s.get());
    }

    static long UCHAR_MAX = (long)(Math.pow(2, Byte.SIZE) - 1);
    static long USHRT_MAX = (long)(Math.pow(2, Short.SIZE) - 1);
    static long UINT_MAX = (long)(Math.pow(2, Integer.SIZE) - 1);
    static long ULONG_MAX = (long)(Math.pow(2, Long.SIZE) - 1);

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
