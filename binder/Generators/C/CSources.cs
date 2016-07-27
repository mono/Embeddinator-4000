using CppSharp.AST;
using CppSharp.AST.Extensions;

namespace MonoManagedToNative.Generators
{
    public class CSources : CTemplate
    {
        public CSources(Driver driver, TranslationUnit unit) : base(driver, unit)
        {
        }

        public override string FileExtension
        {
            get { return "c"; }
        }

        public string AssemblyId
        {
            get
            {
                return GeneratedIdentifier(Assembly.GetName().Name).Replace('.', '_');
            }
        }

        public override void Process()
        {
            GenerateFilePreamble();

            PushBlock();
            WriteLine("#include \"{0}.h\"", Unit.Name);
            WriteLine("#include <mono/jit/jit.h>");
            WriteLine("#include <mono/metadata/assembly.h>");
            //WriteLine("#include <mono/metadata/domain.h>");
            WriteLine("#include <mono/metadata/object.h>");
            WriteLine("#include <mono/metadata/mono-config.h>");
            WriteLine("#include <mono/metadata/debug-helpers.h>");
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock();
            WriteLine("extern MonoDomain* {0};", GeneratedIdentifier("mono_domain"));
            WriteLine("extern bool {0};", GeneratedIdentifier("mono_initialized"));

            WriteLine("extern MonoAssembly* {0}_assembly;", AssemblyId);
            PopBlock(NewLineKind.BeforeNextBlock);

            GenerateMonoInitialization();
            GenerateAssemblyLoad();

            VisitDeclContext(Unit);
        }

        public override bool VisitClassDecl(Class @class)
        {
            PushBlock();
            WriteLine("static MonoClass* {0}_class = 0;", @class.QualifiedName);
            WriteLine("static MonoObject* {0}_instance = 0;", @class.QualifiedName);
            PopBlock(NewLineKind.BeforeNextBlock);

            VisitDeclContext(@class);

            return true;
        }

        public void GenerateMonoInitialization()
        {
            PushBlock();
            WriteLine("static void {0}()", GeneratedIdentifier("initialize_mono"));
            WriteStartBraceIndent();

            WriteLine("if ({0})", GeneratedIdentifier("mono_initialized"));
            WriteLineIndent("return;");

            WriteLine("mono_config_parse(NULL);");

            var domainName = "mono_managed_to_native_binding";
            var version = "v4.0.30319";
            WriteLine("{0} = mono_jit_init_version(\"{1}\", \"{2}\");",
                GeneratedIdentifier("mono_domain"), domainName, version);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateAssemblyLoad()
        {
            var assemblyName = Assembly.GetName().Name;
            var assemblyLookupId = GeneratedIdentifier(string.Format("lookup_{0}_assembly",
                assemblyName.Replace('.', '_')));

            PushBlock();
            WriteLine("static void {0}()", assemblyLookupId);
            WriteStartBraceIndent();

            var monoAssemblyName = string.Format("{0}_assembly", AssemblyId);
            WriteLine("if ({0})", monoAssemblyName);
            WriteLineIndent("return;");

            WriteLine("{0} = mono_domain_assembly_open({1}, \"{2}.dll\");",
                monoAssemblyName, GeneratedIdentifier("mono_domain"), assemblyName);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateMethodLookup(Method method)
        {
            var methodNameId = GeneratedIdentifier("method_name");
            WriteLine("const char {0}[] = \"{1}\";", methodNameId, method.OriginalName);
            var descId = GeneratedIdentifier("desc");

            WriteLine("MonoMethodDesc* {0} = mono_method_desc_new({1}, /*include_namespace=*/true);",
                descId, methodNameId);

            var methodId = GeneratedIdentifier("method");

            var @class = method.Namespace as Class;
            var classId = string.Format("{0}_class", @class.QualifiedName);

            WriteLine("MonoMethod* {0} = mono_method_desc_search_in_class({1}, {2});",
                methodId, descId, classId);

            WriteLine("mono_method_desc_free({0});", descId);
        }

        public void GenerateMethodInvocation(Method method)
        {
            var resultId = GeneratedIdentifier("result");
            WriteLine("MonoObject {0};", resultId);

            var argsId = GeneratedIdentifier("args");
            WriteLine("void* {0} = 0;", argsId);

            var exceptionId = GeneratedIdentifier("exception");
            WriteLine("MonoObject* {0} = 0;", exceptionId);

            var methodId = GeneratedIdentifier("method");
            WriteLine("mono_runtime_invoke({0}, &{1}, {2}, &{3});", methodId, resultId,
                argsId, exceptionId);
        }

        public override bool VisitMethodDecl(Method method)
        {
            PushBlock();

            GenerateMethodSignature(method);
            NewLine();
            WriteStartBraceIndent();

            GenerateMethodLookup(method);

            var retType = method.ReturnType;
            var needsReturn = !retType.Type.IsPrimitiveType(PrimitiveType.Void);

            GenerateMethodInvocation(method);

            var exceptionId = GeneratedIdentifier("exception");
            //WriteLine("if ({0})", exceptionId);
            //WriteLineIndent("return 0;");

            if (needsReturn)
                WriteLine("return 0;");

            WriteCloseBraceIndent();

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }
    }
}
