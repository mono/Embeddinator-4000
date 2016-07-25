using System;

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
}

public static class StaticClass
{
    public static void ReturnsVoid() { }
}