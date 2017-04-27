using System;

namespace ConstructorsLib {
	public class Parent {
		public Parent () : this (1)
		{
		}

		public Parent (int id)
		{
		}
	}

	public class Child : Parent {
		public Child () : base (10)
		{
		}
	}
}
