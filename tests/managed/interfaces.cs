﻿using System;

namespace Interfaces {

	public interface IMakeItUp {

		bool Boolean { get; }

		string Convert (int integer);

		string Convert (long longint);
	}

	// not public - only the contract is exposed thru a static type
	class MakeItUp : IMakeItUp {

		bool result;
		
		public bool Boolean {
			get {
				result = !result;
				return result;
			}
		}

		public string Convert (int integer)
		{
			return integer.ToString ();
		}

		// overload
		public string Convert (long longint)
		{
			return longint.ToString ();
		}
	}

	public static class Supplier {

		static public IMakeItUp Create ()
		{
			return new MakeItUp ();
		}
	}
}
