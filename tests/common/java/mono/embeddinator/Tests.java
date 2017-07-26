package mono.embeddinator;

import managed.*;
import managed.properties.*;
import managed.first.*;
import managed.first.second.*;
import managed.exceptions.*;
import managed.constructors.*;
import managed.enums.*;
import managed.fields.*;
import managed.interfaces.*;
import managed.methods.*;
import managed.structs.*;
import managed.keywords.*;

import static org.junit.Assert.*;
import org.junit.*;

public class Tests {
    private static boolean doublesAreEqual(double value1, double value2) {
        return Double.doubleToLongBits(value1) == Double.doubleToLongBits(value2);
    }

    @Test
    public void testProperties() {
        
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

        managed.first.second.third.ClassWithNestedNamespace nestednamespaces2 = new managed.first.second.third.ClassWithNestedNamespace();
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

        int refId = Parameters.refClass(new Ref(static_method));
        assertEquals(static_method.getId(), refId);

        Item item = Factory.createItem(1);
        assertEquals(1, item.getInteger());

        Collection collection = new Collection();
        assertEquals(0, collection.getCount());
        collection.add(item);
        assertEquals(1, collection.getCount());
        assertEquals(item.getInteger(), collection.getItem(0).getInteger());

        Item item2 = Factory.createItem(2);
        collection.setItem(0, item2);
        assertEquals(1, collection.getCount());
        assertEquals(item2.getInteger(), collection.getItem(0).getInteger());

        collection.remove(item);
        assertEquals(1, collection.getCount());

        collection.remove(item2);
        assertEquals(0, collection.getCount());
    }

    @Test
    public void testStructs() {
        Point p1 = new Point(1.0f, -1.0f);
        doublesAreEqual(1.0f, p1.getX());
        doublesAreEqual(-1.0f, p1.getY());

        Point p2 = new Point(2.0f, -2.0f);
        doublesAreEqual(2.0f, p2.getX());
        doublesAreEqual(-2.0f, p2.getY());

        assertTrue(Point.opEquality(p1, p1));
        assertTrue(Point.opEquality(p2, p2));
        assertTrue(Point.opInequality(p1, p2));

        Point p3 = Point.opAddition(p1, p2);
        doublesAreEqual(3.0f, p3.getX());
        doublesAreEqual(-3.0f, p3.getY());

        Point p4 = Point.opSubtraction(p3, p2);
        assertTrue(Point.opEquality(p4, p1));

        Point z = Point.get_Zero();
        doublesAreEqual(0.0f, z.getX());
        doublesAreEqual(0.0f, z.getY());
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

    @Test
    public void testFieldsInReferences() {
        assertEquals(Long.MAX_VALUE, managed.fields.Class.get_MaxLong());

        assertEquals(0, managed.fields.Class.get_Integer());
        managed.fields.Class.set_Integer(1);
        assertEquals(1, managed.fields.Class.get_Integer());

        assertTrue(managed.fields.Class.get_Scratch().get_Boolean());

        managed.fields.Class.set_Scratch(new managed.fields.Class(false));
        assertFalse(managed.fields.Class.get_Scratch().get_Boolean());

        managed.fields.Class ref1 = new managed.fields.Class(true);
        assertTrue(ref1.get_Boolean());
        ref1.set_Boolean(false);
        assertFalse(ref1.get_Boolean());

        assertNotNull(ref1.get_Structure());
        assertFalse(ref1.get_Structure().get_Boolean());
        ref1.set_Structure(new managed.fields.Struct(true));
        assertTrue(ref1.get_Structure().get_Boolean());

        managed.fields.Class ref2 = new managed.fields.Class(false);
        assertNotNull(ref2.get_Structure());
        assertFalse(ref2.get_Structure().get_Boolean());
    }

    @Test
    public void testFieldsInValueTypes() {
        assertEquals(0, managed.fields.Struct.get_Integer());
        managed.fields.Struct.set_Integer(1);
        assertEquals(1, managed.fields.Struct.get_Integer());

        assertFalse(managed.fields.Struct.get_Scratch().get_Boolean());

        managed.fields.Struct.set_Scratch(new managed.fields.Struct(true));
        assertTrue(managed.fields.Struct.get_Scratch().get_Boolean());

        managed.fields.Struct empty = managed.fields.Struct.get_Empty();
        assertNotNull(empty);
        assertNull(empty.get_Class());

        managed.fields.Struct struct1 = new managed.fields.Struct(true);
        assertTrue(struct1.get_Boolean());
        struct1.set_Boolean(false);
        assertFalse(struct1.get_Boolean());

        assertNotNull(struct1.get_Class());
        assertFalse(struct1.get_Class().get_Boolean());
        struct1.set_Class(null);
        assertNull(struct1.get_Class());
        struct1.set_Class(new managed.fields.Class(true));
        assertTrue(struct1.get_Class().get_Boolean());

        managed.fields.Struct struct2 = new managed.fields.Struct(false);
        assertNotNull(struct2.get_Class());
        assertFalse(struct2.get_Boolean());
    }

    @Test
    public void testInterfaces() {
        IMakeItUp m = Supplier.create();
        assertEquals(true, m.getBoolean());
        assertEquals(false, m.getBoolean());

        assertEquals("0", m.convert(0));
        assertEquals("1", m.convert(1));

        ManagedAdder adder = new ManagedAdder();
        assertEquals(42, OpConsumer.doAddition(adder, 40, 2));
        assertEquals(true, OpConsumer.testManagedAdder(1, -1));
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

        //Just validate this doesn't crash for now
        assertNotNull(Type_DateTime.getNow());
    }

    @Test
    public void testKeywords() {
        Keywords keywords = new Keywords();
        keywords._assert();
    }
}
