using System;
using System.Collections.Generic;
//using System.Linq;

using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

using ObjC;

namespace Embeddinator {

	// While processing user assemblies, we may come across conditions that will affect
	// final code generation that we need to pass to the generation pass

	public class ProcessedAssembly : IEquatable<Assembly>, IEquatable<ProcessedAssembly> {

		public Assembly Assembly { get; private set; }

		public string Name { get; private set; }
		public string SafeName { get; private set; }

		public bool UserCode { get; set; }

		public ProcessedAssembly (Assembly assembly)
		{
			Assembly = assembly;
			Name = assembly.GetName ().Name;
			SafeName = Name.Sanitize ();
		}

		public bool Equals (Assembly other)
		{
			return Assembly == other;
		}

		public bool Equals (ProcessedAssembly other)
		{
			return Assembly == other?.Assembly;
		}

		public override bool Equals (object obj)
		{
			return Assembly.Equals (obj);
		}

		public override int GetHashCode ()
		{
			return Assembly.GetHashCode ();
		}

		public static bool operator == (ProcessedAssembly a, ProcessedAssembly b)
		{
			return a?.Assembly == b?.Assembly;
		}

		public static bool operator != (ProcessedAssembly a, ProcessedAssembly b)
		{
			return a?.Assembly != b?.Assembly;
		}

		public override string ToString ()
		{
			return Assembly.ToString ();
		}
	}

	public class ProcessedType {
		public Type Type { get; private set; }
		public string TypeName { get; set; }
		public string ObjCName { get; private set; }

		public ProcessedAssembly Assembly { get; set; }
		public List<ProcessedConstructor> Constructors { get; set; }
		public List<ProcessedFieldInfo> Fields { get; set; }
		public List<ProcessedMethod> Methods { get; set; }
		public List<ProcessedProperty> Properties { get; set; }

		public bool HasConstructors => Constructors != null && Constructors.Count > 0;
		public bool HasFields => Fields != null && Fields.Count > 0;
		public bool HasMethods => Methods != null && Methods.Count > 0;
		public bool HasProperties => Properties != null && Properties.Count > 0;

		public bool IsClass => !IsEnum && !IsProtocol;
		public bool IsEnum => Type.IsEnum;
		public bool IsProtocol => Type.IsInterface;

		public bool UserCode => Assembly.UserCode;

		public ProcessedType (Type type)
		{
			Type = type;
			TypeName = ObjC.NameGenerator.GetTypeName (Type);
			ObjCName = ObjC.NameGenerator.GetObjCName (Type);
		}

		public bool SignatureExists (ProcessedMemberBase p)
		{
			foreach (var pc in Constructors) {
				if (p.ObjCSignature == pc.ObjCSignature)
					return true;
			}
			foreach (var pm in Methods) {
				if (p.ObjCSignature == pm.ObjCSignature)
					return true;
			}
			// FIXME signature clashes can happen on properties, fields (turned into properties) ...
			return false;
		}

		public override string ToString ()
		{
			return Type.ToString ();
		}
	}

	public abstract class ProcessedMemberBase {
		public bool FallBackToTypeName { get; set; }
		public int FirstDefaultParameter { get; set; }

		public string ObjCSignature { get; set; }
		public string MonoSignature { get; set; }

		public abstract void ComputeSignatures (Processor p);
	}

	public enum MethodType {
		Normal,
		DefaultValueWrapper,
	}

	public class ProcessedMethod : ProcessedMemberBase {
		public MethodInfo Method { get; private set; }
		public bool IsOperator { get; set; }
		public string NameOverride { get; set; }

		public string BaseName {
			get {
				if (NameOverride != null)
					return NameOverride;
				return IsOperator? Method.Name.Substring (3).CamelCase () : Method.Name.CamelCase ();
			}
		}

		public MethodType MethodType { get; set; }

		public ProcessedMethod (MethodInfo method)
		{
			Method = method;
			MethodType = MethodType.Normal;
		}

		public override void ComputeSignatures (Processor p)
		{
			// FIXME this is a quite crude hack waiting for a correct move of the signature code
			string objcsig;
			string monosig;
			(p as ObjCProcessor).GetSignatures (BaseName, Method.Name, Method, Method.GetParameters (), FallBackToTypeName, false, out objcsig, out monosig);
			ObjCSignature = objcsig;
			MonoSignature = monosig;
		}
	}

	public class ProcessedProperty: ProcessedMemberBase {
		public PropertyInfo Property { get; private set; }

		public ProcessedProperty (PropertyInfo property)
		{
			Property = property;
		}

		public override void ComputeSignatures (Processor p)
		{
			throw new NotImplementedException ();
		}
	}

	public enum ConstructorType {
		Normal,
		// a `init*` method generated wrapper should not be decorated with NS_DESIGNATED_INITIALIZER
		DefaultValueWrapper,
	}

	public class ProcessedConstructor : ProcessedMemberBase {
		public ConstructorInfo Constructor { get; private set; }

		public bool Unavailable { get; set; }

		public ConstructorType ConstructorType { get; set; }

		public ProcessedConstructor (ConstructorInfo constructor)
		{
			Constructor = constructor;
		}

		public override void ComputeSignatures (Processor p)
		{
			// FIXME this is a quite crude hack waiting for a correct move of the signature code
			string objcsig;
			string monosig;
			(p as ObjCProcessor).GetSignatures (Constructor.ParameterCount == 0 ? "init" : "initWith", Constructor.Name, Constructor, Constructor.GetParameters (), FallBackToTypeName, false, out objcsig, out monosig);
			ObjCSignature = objcsig;
			MonoSignature = monosig;
		}

		public override string ToString ()
		{
			return Constructor.ToString ();
		}
	}

	public class ProcessedFieldInfo : ProcessedMemberBase {
		public FieldInfo Field { get; private set; }
		public string TypeName { get; private set; }
		public string ObjCName { get; private set; }

		public ProcessedFieldInfo (FieldInfo field)
		{
			Field = field;
			TypeName = ObjC.NameGenerator.GetTypeName (Field.DeclaringType);
			ObjCName = ObjC.NameGenerator.GetObjCName (Field.DeclaringType);
		}

		public override void ComputeSignatures (Processor p)
		{
			throw new NotImplementedException ();
		}
	}
}