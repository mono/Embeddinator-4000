using System;
using Embeddinator;

namespace ObjC {

	public class ProtocolHelper : ClassHelper {

		public ProtocolHelper (SourceWriter headers, SourceWriter implementation, SourceWriter privateHeaders) :
			base (headers, implementation, privateHeaders)
		{
		}

		string protocolName;
		public string ProtocolName {
			get { return protocolName; }
			set {
				protocolName = value;
				Name = value;
				WrapperName = $"__{value}Wrapper";
			}
		}

		public string WrapperName { get; set; }

		public override void BeginHeaders ()
		{
			headers.WriteLine ();
			headers.WriteLine ($"/** Protocol {ProtocolName}");
			headers.WriteLine ($" *  Corresponding .NET Qualified Name: `{AssemblyQualifiedName}`");
			headers.WriteLine (" *");
			headers.WriteLine (" * @warning This expose a managed (.net) interface. Conforming to this protocol from Objective-C code");
			headers.WriteLine (" * does not allow interop with managed code. https://mono.github.io/Embeddinator-4000/Limitations#Subclassing");
			headers.WriteLine (" */");
			headers.WriteLine ($"@protocol {ProtocolName} <NSObject>");
			headers.WriteLine ();
		}

		public override void EndHeaders ()
		{
			headers.WriteLine ("@end");
			headers.WriteLine ();
		}

		public override void BeginImplementation (string implementationName = null)
		{
			private_headers.WriteLine ($"@interface {WrapperName} : NSObject <{ProtocolName}> {{");
			private_headers.Indent++;
			private_headers.WriteLine ("@public MonoEmbedObject* _object;");
			private_headers.Indent--;
			private_headers.WriteLine ("}");
			private_headers.WriteLine ("- (nullable instancetype)initForSuper;");
			private_headers.WriteLine ("@end");
			private_headers.WriteLine ();
			implementation.WriteLine ($"static MonoClass* {ProtocolName}_class = nil;");
			implementation.WriteLine ();
			base.BeginImplementation (WrapperName);
		}
	}
}
