﻿﻿using System;
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

		public override string ToString () => Type.ToString ();
	}

	public abstract class ProcessedMemberBase {

		protected Processor Processor;

		public bool FallBackToTypeName { get; set; }
		public int FirstDefaultParameter { get; set; }

		public string ObjCSignature { get; set; }
		public string MonoSignature { get; set; }

		public ProcessedMemberBase (Processor processor)
		{
			Processor = processor;
			FirstDefaultParameter = -1;
		}

		public abstract void ComputeSignatures ();

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

		// get a name that is safe to use from ObjC code

		// HACK - This should take a ProcessedMemberBase and not much of this stuff - https://github.com/mono/Embeddinator-4000/issues/276
		public void GetSignatures (string objName, string monoName, MemberInfo info, ParameterInfo[] parameters, bool isExtension, out string objcSignature, out string monoSignature)
		{
			// FIXME - GetSignatures likley should be specialized in subclasses
			bool isOperator = (this is ProcessedMethod) ? ((ProcessedMethod)this).IsOperator : false;
			var method = (info as MethodBase); // else it's a PropertyInfo
											   // special case for setter-only - the underscore looks ugly
			if ((method != null) && method.IsSpecialName)
				objName = objName.Replace ("_", String.Empty);

			var objc = new StringBuilder (objName);
			var mono = new StringBuilder (monoName);

			mono.Append ('(');

			var end = FirstDefaultParameter == -1 ? parameters.Length : FirstDefaultParameter;
			for (int n = 0; n < end; ++n) {
				ParameterInfo p = parameters[n];

				if (objc.Length > objName.Length) {
					objc.Append (' ');
					mono.Append (',');
				}

				string paramName = FallBackToTypeName ? NameGenerator.GetParameterTypeName (p.ParameterType) : p.Name;
				if ((method != null) && (n > 0 || !isExtension)) {
					if (n == 0) {
						if (FallBackToTypeName || method.IsConstructor || (!method.IsSpecialName && !isOperator))
							objc.Append (paramName.PascalCase ());
					}
					else
						objc.Append (paramName.CamelCase ());
				}

				if (n > 0 || !isExtension) {
					string ptname = NameGenerator.GetObjCParamTypeName (p, Processor.Types);
					objc.Append (":(").Append (ptname).Append (")").Append (NameGenerator.GetExtendedParameterName (p, parameters));
				}
				mono.Append (NameGenerator.GetMonoName (p.ParameterType));
			}

			mono.Append (')');

			objcSignature = objc.ToString ();
			monoSignature = mono.ToString ();
		}
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

		public ProcessedMethod (MethodInfo method, Processor processor) : base (processor)
		{
			Method = method;
			MethodType = MethodType.Normal;
		}

		public override void ComputeSignatures ()
		{
			// FIXME this is a quite crude hack waiting for a correct move of the signature code
			string objcsig;
			string monosig;
			GetSignatures (BaseName, Method.Name, Method, Method.GetParameters (), false, out objcsig, out monosig);
			ObjCSignature = objcsig;
			MonoSignature = monosig;
		}

		public override string ToString () => ToString (Method);
	}

	public class ProcessedProperty: ProcessedMemberBase {
		public PropertyInfo Property { get; private set; }

		public ProcessedProperty (PropertyInfo property, Processor processor) : base (processor)
		{
			Property = property;
			if (HasGetter)
				GetMethod = new ProcessedMethod (Property.GetGetMethod (), Processor);
			if (HasSetter)
				SetMethod = new ProcessedMethod (Property.GetSetMethod (), Processor);
		}

		public override void ComputeSignatures ()
		{
			throw new NotImplementedException ();
		}

		public override string ToString () => Property.ToString ();

		public bool HasGetter => Property.GetGetMethod () != null;
		public bool HasSetter => Property.GetSetMethod () != null;
		public ProcessedMethod GetMethod { get; private set; }
		public ProcessedMethod SetMethod { get; private set; }
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

		public ProcessedConstructor (ConstructorInfo constructor, Processor processor) : base (processor)
		{
			Constructor = constructor;
		}

		public override void ComputeSignatures ()
		{
			// FIXME this is a quite crude hack waiting for a correct move of the signature code
			string objcsig;
			string monosig;
			GetSignatures (Constructor.ParameterCount == 0 ? "init" : "initWith", Constructor.Name, Constructor, Constructor.GetParameters (), false, out objcsig, out monosig);
			ObjCSignature = objcsig;
			MonoSignature = monosig;
		}

		public override string ToString () => ToString (Constructor);
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

		public override void ComputeSignatures ()
		{
			throw new NotImplementedException ();
		}

		// linker compatible signature
		public override string ToString () => Field.FieldType.FullName + " " + Field.Name;
	}
}