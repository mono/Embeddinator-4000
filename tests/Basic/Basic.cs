using System;

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

#region Classes

public static class StaticClass
{
    public static void ReturnsVoid() { }
}

#endregion