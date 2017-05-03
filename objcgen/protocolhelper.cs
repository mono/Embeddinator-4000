using System;
using Embeddinator;

namespace ObjC {

	public class ProtocolHelper : ClassHelper {

		public ProtocolHelper (SourceWriter headers, SourceWriter implementation) :
			base (headers, implementation)
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
			implementation.WriteLine ($"@interface {WrapperName} : NSObject <{ProtocolName}> {{");
			implementation.Indent++;
			implementation.WriteLine ("@public MonoEmbedObject* _object;");
			implementation.Indent--;
			implementation.WriteLine ("}");
			implementation.WriteLine ("- (nullable instancetype)initForSuper;");
			implementation.WriteLine ("@end");
			implementation.WriteLine ();
			implementation.WriteLine ($"static MonoClass* {ProtocolName}_class = nil;");
			implementation.WriteLine ();
			base.BeginImplementation (WrapperName);
		}
	}
}
