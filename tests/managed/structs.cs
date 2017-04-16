using System;

namespace Structs {

	public struct Point {

		public static readonly Point Zero;

		public Point (float x, float y)
		{
			X = x;
			Y = y;
		}

		public float X { get; private set; }

		public float Y { get; private set; }

		public static bool operator == (Point left, Point right)
		{
			return ((left.X == right.X) && (left.Y == right.Y));
		}

		public static bool operator != (Point left, Point right)
		{
			return !(left == right);
		}

		public static Point operator + (Point left, Point right)
		{
			return new Point (left.X + right.X, left.Y + right.Y);
		}

		public static Point operator - (Point left, Point right)
		{
			return new Point (left.X - right.X, left.Y - right.Y);
		}
	}
}
