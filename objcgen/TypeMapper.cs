using System;
using System.Collections.Generic;

namespace Embeddinator
{
	public class TypeMapper
	{
		Dictionary<ProcessedType, Dictionary<string, ProcessedMemberBase>> MappedTypes = new Dictionary<ProcessedType, Dictionary<string, ProcessedMemberBase>> ();

		Dictionary <string, ProcessedMemberBase> GetRegistrationForType (ProcessedType t)
		{
			Dictionary<string, ProcessedMemberBase> data;
			if (MappedTypes.TryGetValue (t, out data))
				return data;
			return null;
		}
		
		public bool IsSelectorTaken (ProcessedMemberBase member)
		{
			var typeRegistration = GetRegistrationForType (member.DeclaringType);
			if (typeRegistration != null) {
				foreach (var selector in member.Selectors) {
					if (typeRegistration.ContainsKey (selector))
						return true;
				}
			}

			return false;
		}

		public IEnumerable <ProcessedMemberBase> WithSameSelector (ProcessedMemberBase member)
		{
			var typeRegistration = GetRegistrationForType (member.DeclaringType);
			if (typeRegistration != null) {
				foreach (var selector in member.Selectors) {
					ProcessedMemberBase registeredMember = null;
					if (typeRegistration.TryGetValue (selector, out registeredMember))
						yield return registeredMember;
				}
			}
		}

		public void Register (ProcessedMemberBase member)
		{
			var typeRegistration = GetRegistrationForType (member.DeclaringType);
			if (typeRegistration == null) {
				typeRegistration = new Dictionary<string, ProcessedMemberBase> ();
				MappedTypes.Add (member.DeclaringType, typeRegistration);
			}
			foreach (var selector in member.Selectors) {
				typeRegistration.Add (selector, member);
			}
		}

		public void CheckForDuplicateSelectors (ProcessedMemberBase member)
		{
			if (IsSelectorTaken (member)) {
				foreach (var conflictMethod in WithSameSelector (member))
					conflictMethod.FallBackToTypeName = true;
				member.FallBackToTypeName = true;
			}
		}
	}
}
