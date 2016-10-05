using System;
using System.Linq;

#region Builtin types

public class BuiltinTypes
{
    public void ReturnsVoid() { }
    public bool ReturnsBool() { return true; }
    public sbyte ReturnsSByte() { return -5; }
    public byte ReturnsByte() { return 5; }
    public short ReturnsShort() { return -5; }
    public ushort ReturnsUShort() { return 5; }
    public int ReturnsInt() { return -5; }
    public uint ReturnsUInt() { return 5; }
    public long ReturnsLong() { return -5; }
    public ulong ReturnsULong() { return 5; }
    public string ReturnsString() { return "Mono"; }

    public bool PassAndReturnsBool(bool v) { return v; }
    public sbyte PassAndReturnsSByte(sbyte v) { return v; }
    public byte PassAndReturnsByte(byte v) { return v; }
    public short PassAndReturnsShort(short v) { return v; }
    public ushort PassAndReturnsUShort(ushort v) { return v; }
    public int PassAndReturnsInt(int v) { return v; }
    public uint PassAndReturnsUInt(uint v) { return v; }
    public long PassAndReturnsLong(long v) { return v; }
    public ulong PassAndReturnsULong(ulong v) { return v; }
    public string PassAndReturnsString(string v) { return v; }

    public void PassOutInt(out int v) { v = 5; }
    public void PassRefInt(ref int v) { v = 10; }
}

#endregion

#region Arrays

public static class ArrayTypes
{
    public static int SumByteArray(byte[] array) { return array.Sum(n => n); }
    
    public static int[] ReturnsIntArray() { return new int[] { 1, 2, 3 }; }

    public static string[] ReturnsStringArray() { return new string[] { "1", "2", "3" }; }

}

#endregion

#region Enums

public enum Enum
{
    Two = 2,
    Three
}

public enum EnumByte : byte
{
    Two = 2,
    Three
}

[Flags]
public enum EnumFlags
{
    FlagOne = 1 << 0,
    FlagTwo = 1 << 2
}

public static class EnumTypes
{
    public static int PassEnum(Enum e) { return (int)e; }
    public static byte PassEnumByte(EnumByte e) { return (byte)e; }
    public static int PassEnumFlags(EnumFlags e) { return (int)e; }
}

#endregion

#region Classes

public class NonStaticClass
{
    public static int StaticMethod() { return 0; }
}

public static class StaticClass
{
    public static int StaticMethod() { return 0; }
}

namespace NS1
{
    public class NamespacedClass
    {

    }
}

namespace NS2
{
    public class NamespacedClass
    {
        
    }
}

#endregion