using System;

namespace EnumsLib {

	public enum Pokemon {
		Pikachu,
		Charmander		
	}

	public class EnumsTest {
		public EnumsTest ()
		{
		}

		public Pokemon GetPokemon () => Pokemon.Pikachu;
	}
}
