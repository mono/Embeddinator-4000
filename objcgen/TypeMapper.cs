using System;
using System.Collections.Generic;

namespace Embeddinator
{
	public class TypeMapper
	{
		Dictionary<ProcessedType, Dictionary<string, ProcessedMemberBase>> MappedTypes = new Dictionary<ProcessedType, Dictionary<string, ProcessedMemberBase>> ();
		
		IEnumerable<string> GetNames (ProcessedMemberBase member)
		{
			if (member is ProcessedMemberWithParameters) {
				yield return StripFullSignature (((ProcessedMemberWithParameters)member).ObjRawCSignature);
			} else if (member is ProcessedProperty) {
				ProcessedProperty property = (ProcessedProperty)member;
				if (property.HasGetter)
					yield return property.GetterName;
				if (property.HasSetter)
					yield return property.SetterName;
			} else if (member is ProcessedFieldInfo) {
				ProcessedFieldInfo field = (ProcessedFieldInfo)member;
				yield return field.GetterName;
				yield return field.SetterName;
			} else {
				throw new NotImplementedException ();
			}
		}

		string StripFullSignature (string s)
		{
			System.Console.WriteLine (s);
			return s;
		}

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

				foreach (var name in GetNames (member)) {
					if (typeRegistration.ContainsKey (name))
						return true;
				}
			}

			return false;
		}

		public IEnumerable <ProcessedMemberBase> WithSameSelector (ProcessedMemberBase member)
		{
			var typeRegistration = GetRegistrationForType (member.DeclaringType);
			if (typeRegistration != null) {
				foreach (var name in GetNames (member)) {
					ProcessedMemberBase registeredMember = null;
					if (typeRegistration.TryGetValue (name, out registeredMember))
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
			foreach (var name in GetNames (member)) {
				typeRegistration.Add (name, member);
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
