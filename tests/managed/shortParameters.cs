using System;
namespace ShortParameters
{
    public class Class
    {
        public int X { get; set; }
        public int Y { get; set; }

        public string A { get; set; }
        public string B { get; set; }

        public double C { get; set; }
        public double D { get; set; }

        public float E { get; set; }
        public float F { get; set; }

        public uint G { get; set; }
        public uint H { get; set; }

        public short I { get; set; }
        public short J { get; set; }

        public ushort K { get; set; }
        public ushort L { get; set; }

        public long M { get; set; }
        public long N { get; set; }

        public ulong O { get; set; }
        public ulong P { get; set; }

        public char Q { get; set; }
        public char R { get; set; }

        public bool S { get; set; }
        public bool T { get; set; }

        public Class ()
        {
        }

        public bool NoDuplicateTypes(string a, double c, float e, uint g, short i, ushort k, long m, ulong o, char q, bool s)
        {
            A = a;
            C = c;
            E = e;
            G = g;
            I = i;
            K = k;
            M = m;
            O = o;
            Q = q;
            S = s;

            return true;
        }

        public int TwoInt(int x, int y)
        {
            X = x;
            Y = y;
            return x + y;
        }

        public string TwoString(string a, string b)
        {
            A = a;
            B = b;
            return a + b;
        }

        public double TwoDouble(double c, double d)
        {
            C = c;
            D = d;
            return c + d;
        }

        public float TwoFloat(float e, float f)
        {
            E = e;
            F = f;
            return e + f;
        }

        public uint TwoUint(uint g, uint h)
        {
            G = g;
            H = h;
            return g + h;
        }

        public int TwoShort (short i, short j)
        {
            I = i;
            J = j;
            return i + j;
        }

        public int TwoUshort (ushort k, ushort l)
        {
            K = k;
            L = l;
            return k + l;
        }

        public long TwoLong (long m, long n)
        {
            M = m;
            N = n;
            return m + n;
        }

        public ulong TwoUlong (ulong o, ulong p)
        {
            O = o;
            P = p;
            return o + p;
        }

        public int TwoChar (char q, char r)
        {
            Q = q;
            R = r;
            return q + r;
        }

        public bool TwoBool (bool s, bool t)
        {
            S = s;
            T = t;
            return s || t;
        }
	}
}
