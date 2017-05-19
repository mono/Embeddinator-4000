using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using IKVM.Reflection;
using Type = IKVM.Reflection.Type;

namespace Embeddinator {

	public abstract class Generator {

		public bool GenerateLinkerExclusions { get; set; }

		public virtual void Generate ()
		{
			if (Processor == null)
				throw ErrorHelper.CreateError (99, "Internal error `Invalid state for Generate`. Please file a bug report with a test case (https://github.com/mono/Embeddinator-4000/issues).");

			// FIXME: remove asap
			var op = (Processor as ObjC.ObjCProcessor);
			extensions_methods = op.extensions_methods;
			subscriptProperties = op.subscriptProperties;
			icomparable = op.icomparable;

			foreach (var a in Processor.Assemblies) {
				Generate (a);
			}
		}

		public Processor Processor { get; set; }

		public bool HasClass (Type t)
		{
			return Processor.Types.HasClass (t);
		}

		public bool HasProtocol (Type t)
		{
			return Processor.Types.HasProtocol (t);
		}

		protected abstract void Generate (ProcessedAssembly a);
		protected abstract void Generate (ProcessedType t);
		protected abstract void Generate (ProcessedProperty property);
		protected abstract void Generate (ProcessedMethod method);

		protected void WriteFile (string name, string content)
		{
			Console.WriteLine ($"\tGenerated: {name}");
			File.WriteAllText (name, content);
		}

		string ToXml (object o)
		{
			return SecurityElement.Escape (o.ToString ());
		}

		public virtual void Write (string outputDirectory)
		{
			if (!GenerateLinkerExclusions)
				return;
			var xml = new SourceWriter ();
			xml.WriteLine ("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
			xml.WriteLine ("<linker>");
			xml.Indent++;
			foreach (var a in Processor.Assemblies.Where ((arg) => !arg.UserCode)) {
				xml.WriteLine ($"<assembly fullname=\"{a.Name}\">");
				xml.Indent++;
				foreach (var t in Processor.Types.Where ((arg) => arg.Assembly == a)) {
					xml.WriteLine ($"<type fullname=\"{t}\">");
					xml.Indent++;
					if (t.HasConstructors) {
						foreach (var c in t.Constructors)
							xml.WriteLine ($"<method signature=\"{ToXml (c)}\"/>");
					}
					if (t.HasMethods) {
						foreach (var m in t.Methods)
							xml.WriteLine ($"<method signature=\"{ToXml (m)}\"/>");
					}
					if (t.HasProperties) {
						foreach (var p in t.Properties) {
							var prop = p.Property;
							var getter = prop.GetGetMethod ();
							if (getter != null)
								xml.WriteLine ($"<method signature=\"{ToXml (p.ToString (getter))}\"/>");
							var setter = prop.GetSetMethod ();
							if (setter != null)
								xml.WriteLine ($"<method signature=\"{ToXml (p.ToString (setter))}\"/>");
						}
					}
					if (t.HasFields) {
						foreach (var f in t.Fields)
							xml.WriteLine ($"<field signature=\"{ToXml (f)}\"/>");
					}
					xml.Indent--;
					xml.WriteLine ("</type>");
				}
				xml.Indent--;
				xml.WriteLine ("</assembly>");
			}
			xml.Indent--;
			xml.WriteLine ("</linker>");
			WriteFile (Path.Combine (outputDirectory, "bindings.xml"), xml.ToString ());
		}

		// to be removed / replaced
		public Dictionary<Type, Dictionary<Type, List<ProcessedMethod>>> extensions_methods { get; private set; }
		public Dictionary<Type, List<ProcessedProperty>> subscriptProperties { get; private set; }
		public Dictionary<Type, MethodInfo> icomparable { get; private set; }
	}
}
