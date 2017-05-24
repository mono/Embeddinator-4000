﻿﻿﻿using System;
using System.Collections.Generic;
using System.Text;

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

		public override string ToString () => Assembly.ToString ();
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

		public bool IsClass => !IsEnum && !IsProtocol && !IsNativeReference;
		public bool IsEnum => Type.IsEnum && !IsNativeReference;
		public bool IsProtocol => Type.IsInterface && !IsNativeReference;

		// we can track types that we don't need/want to generate (e.g. linker requirements)
		public bool IsNativeReference { get; set; }

		public bool UserCode => Assembly.UserCode;

		public ProcessedType (Type type)
		{
			Type = type;
			TypeName = ObjC.NameGenerator.GetTypeName (Type);
			ObjCName = ObjC.NameGenerator.GetObjCName (Type);
		}

		public bool SignatureExists (ProcessedMemberWithParameters p)
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

		public override string ToString () => Type.ToString ();
	}

	public abstract class ProcessedMemberBase {

		protected Processor Processor;
		public bool FallBackToTypeName { get; set; }

		public ProcessedMemberBase (Processor processor)
		{
			Processor = processor;
		}

		// this format can be consumed by the linker xml files
		// adapted from ikvm reflection and cecil source code
		// FIXME: double check when we implement generics support
		public string ToString (MethodBase m)
		{
			StringBuilder sb = new StringBuilder ();
			var mi = m as MethodInfo;
			if (mi != null)
				sb.Append (mi.ReturnType.FullName).Append (' ');
			else
				sb.Append ("System.Void "); // ConstructorInfo
			sb.Append (m.Name);
			sb.Append ('(');
			var sep = String.Empty;
			foreach (var p in m.GetParameters ()) {
				sb.Append (sep).Append (p.ParameterType);
				sep = ",";
			}
			sb.Append (')');
			return sb.ToString();
		}
	}

	public enum MethodType {
		Normal,
		DefaultValueWrapper,
		NSObjectProcotolHash,
		NSObjectProcotolIsEqual,
		IEquatable,
	}

	public abstract class ProcessedMemberWithParameters : ProcessedMemberBase {
		public ProcessedMemberWithParameters (Processor processor) : base (processor)
		{
		}

		public abstract string BaseName { get; }

		public ParameterInfo[] Parameters { get; protected set; }

		string objCSignature;
		public string ObjCSignature {
			get {
				if (objCSignature == null)
					ComputeSignatures ();
				return objCSignature;
			}
			protected set {
				objCSignature = value;
			}
		}

		string monoSignature;
		public string MonoSignature {
			get {
				if (monoSignature == null)
					ComputeSignatures ();
				return monoSignature;
			}
			protected set {
				monoSignature = value;
			}
		}

		protected abstract void ComputeSignatures ();

		public int FirstDefaultParameter { get; set; }
	}

	public class ProcessedMethod : ProcessedMemberWithParameters {
		public MethodInfo Method { get; private set; }
		public bool IsOperator { get; set; }

		public string NameOverride { get; set; }
		public string ManagedName { get; set; }

		public override string BaseName {
			get {
				if (NameOverride != null)
					return NameOverride;
				return IsOperator ? Method.Name.Substring (3).CamelCase () : Method.Name.CamelCase ();
			}
		}

		public MethodType MethodType { get; set; }
		public ProcessedType DeclaringType { get; set; }
		public bool IsExtension { get; set; }

		public ProcessedMethod (MethodInfo method, Processor processor) : base (processor)
		{
			Method = method;
			MethodType = MethodType.Normal;
			Parameters = method.GetParameters ();
			FirstDefaultParameter = -1;
		}

		protected override void ComputeSignatures ()
		{
			ObjCSignature = GetObjcSignature ();
			MonoSignature = GetMonoSignature ();
		}

		public override string ToString () => ToString (Method);

		string GetMonoSignature ()
		{
			var mono = new StringBuilder (Method.Name);

			mono.Append ('(');

			var end = FirstDefaultParameter == -1 ? Parameters.Length : FirstDefaultParameter;
			for (int n = 0; n < end; ++n) {
				if (n > 0)
					mono.Append (',');
				mono.Append (NameGenerator.GetMonoName (Parameters[n].ParameterType));
			}

			mono.Append (')');

			return mono.ToString ();
		}

		string GetObjcSignature ()
		{
			string objName = BaseName;

			if (Method.IsSpecialName)
				objName = objName.Replace ("_", String.Empty);

			var objc = new StringBuilder (objName);

			var end = FirstDefaultParameter == -1 ? Parameters.Length : FirstDefaultParameter;
			for (int n = 0; n < end; ++n) {
				ParameterInfo p = Parameters[n];

				if (objc.Length > objName.Length)
					objc.Append (' ');

				string paramName = FallBackToTypeName ? NameGenerator.GetParameterTypeName (p.ParameterType) : p.Name;
				if (n > 0 || !IsExtension) {
					if (n == 0) {
						if (FallBackToTypeName || Method.IsConstructor || (!Method.IsSpecialName && !IsOperator))
							objc.Append (paramName.PascalCase ());
					} else
						objc.Append (paramName.CamelCase ());
				}

				if (n > 0 || !IsExtension) {
					string ptname = NameGenerator.GetObjCParamTypeName (p, Processor.Types);
					objc.Append (":(").Append (ptname).Append (")").Append (NameGenerator.GetExtendedParameterName (p, Parameters));
				}
			}

			return objc.ToString ();
		}
	}

	public class ProcessedProperty: ProcessedMemberBase {
		public PropertyInfo Property { get; private set; }

		public ProcessedProperty (PropertyInfo property, Processor processor) : base (processor)
		{
			Property = property;

			var g = Property.GetGetMethod ();
			if (g != null)
				GetMethod = new ProcessedMethod (g, Processor);

			var s = Property.GetSetMethod ();
			if (s != null)
				SetMethod = new ProcessedMethod (s, Processor);
		}

		public override string ToString () => Property.ToString ();

		public string Name => NameOverride != null ? NameOverride : Property.Name.CamelCase ();
		public string NameOverride { get; set; }

		public bool HasGetter => GetMethod != null;
		public bool HasSetter => SetMethod != null;

		public string GetterName {
			get {
				if (!HasGetter)
					return null;
				return (NameOverride ?? Property.Name).CamelCase ();
			}
		}

		public string SetterName {
			get {
				if (!HasSetter)
					return null;
				return "set" + (NameOverride ?? Property.Name).PascalCase ();
			}
		}

		public ProcessedMethod GetMethod { get; private set; }
		public ProcessedMethod SetMethod { get; private set; }
	}

	public enum ConstructorType {
		Normal,
		// a `init*` method generated wrapper should not be decorated with NS_DESIGNATED_INITIALIZER
		DefaultValueWrapper,
	}

	public class ProcessedConstructor : ProcessedMemberWithParameters {
		public ConstructorInfo Constructor { get; private set; }

		public bool Unavailable { get; set; }
		public override string BaseName {
			get {
				if (Parameters.Length == 0 || FirstDefaultParameter == 0)
					return "init";
				return "initWith";
			}
		}
		public ConstructorType ConstructorType { get; set; }

		public ProcessedConstructor (ConstructorInfo constructor, Processor processor) : base (processor)
		{
			Constructor = constructor;
			Parameters = Constructor.GetParameters ();
			FirstDefaultParameter = -1;
		}

		protected override void ComputeSignatures ()
		{
			ObjCSignature = GetObjcSignature ();
			MonoSignature = GetMonoSignature ();
		}

		public override string ToString () => ToString (Constructor);

		string GetMonoSignature ()
		{
			var mono = new StringBuilder (Constructor.Name);

			mono.Append ('(');

			var end = FirstDefaultParameter == -1 ? Parameters.Length : FirstDefaultParameter;
			for (int n = 0; n < end; ++n) {
				if (n > 0)
					mono.Append (',');
				mono.Append (NameGenerator.GetMonoName (Parameters[n].ParameterType));
			}

			mono.Append (')');

			return mono.ToString ();
		}

		string GetObjcSignature ()
		{
			var objc = new StringBuilder (BaseName);

			var end = FirstDefaultParameter == -1 ? Parameters.Length : FirstDefaultParameter;
			for (int n = 0; n < end; ++n) {
				ParameterInfo p = Parameters[n];

				if (objc.Length > BaseName.Length)
					objc.Append (' ');

				string paramName = FallBackToTypeName ? NameGenerator.GetParameterTypeName (p.ParameterType) : p.Name;
				if (n == 0)
					objc.Append (paramName.PascalCase ());
				else
					objc.Append (paramName.CamelCase ());

				string ptname = NameGenerator.GetObjCParamTypeName (p, Processor.Types);
				objc.Append (":(").Append (ptname).Append (")").Append (NameGenerator.GetExtendedParameterName (p, Parameters));
			}

			return objc.ToString ();
		}
	}

	public class ProcessedFieldInfo : ProcessedMemberBase {
		public FieldInfo Field { get; private set; }
		public string TypeName { get; private set; }
		public string ObjCName { get; private set; }

		public ProcessedFieldInfo (FieldInfo field, Processor processor) : base (processor)
		{
			Field = field;
			TypeName = ObjC.NameGenerator.GetTypeName (Field.DeclaringType);
			ObjCName = ObjC.NameGenerator.GetObjCName (Field.DeclaringType);
		}

		// linker compatible signature
		public override string ToString () => Field.FieldType.FullName + " " + Field.Name;
	}
}
