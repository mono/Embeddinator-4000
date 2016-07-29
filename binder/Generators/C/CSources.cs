using CppSharp.AST;
using CppSharp.AST.Extensions;
using System.Linq;

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
            WriteLine("#include <mono/metadata/object.h>");
            WriteLine("#include <mono/metadata/mono-config.h>");
            WriteLine("#include <mono/metadata/debug-helpers.h>");
            WriteLine("#include <stdlib.h>");
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock();
            WriteLine("MonoDomain* {0};", GeneratedIdentifier("mono_domain"));
            WriteLine("bool {0};", GeneratedIdentifier("mono_initialized"));

            WriteLine("MonoAssembly* {0}_assembly;", AssemblyId);
            WriteLine("MonoImage* {0}_image;", AssemblyId);
            PopBlock(NewLineKind.BeforeNextBlock);

            GenerateMonoInitialization();
            GenerateAssemblyLoad();

            VisitDeclContext(Unit);
        }

        public override bool VisitClassDecl(Class @class)
        {
            PushBlock();
            WriteLine("static MonoClass* {0}_class = 0;", @class.QualifiedName);
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock();
            WriteLine("struct {0}", @class.Name);
            WriteStartBraceIndent();

            WriteLine("MonoClass* _class;", @class.QualifiedName);
            WriteLine("uint32_t _handle;", @class.QualifiedName);

            PopIndent();
            WriteLine("};");
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
            var assemblyLookupId = GeneratedIdentifier(string.Format("lookup_assembly_{0}",
                assemblyName.Replace('.', '_')));

            PushBlock();
            WriteLine("static void {0}()", assemblyLookupId);
            WriteStartBraceIndent();

            var monoAssemblyName = string.Format("{0}_assembly", AssemblyId);
            WriteLine("if ({0})", monoAssemblyName);
            WriteLineIndent("return;");

            WriteLine("{0} = mono_domain_assembly_open({1}, \"{2}.dll\");",
                monoAssemblyName, GeneratedIdentifier("mono_domain"), assemblyName);

            var monoImageName = string.Format("{0}_image", AssemblyId);
            WriteLine("{0} = mono_assembly_get_image({1});", monoImageName,
                monoAssemblyName);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateClassLookup(Method method)
        {
            PushBlock();

            var @class = method.Namespace as Class;
            var classId = string.Format("{0}_class", @class.QualifiedName);

            WriteLine("if ({0} == 0)", classId);
            WriteStartBraceIndent();

            WriteLine("{0}();", GeneratedIdentifier("initialize_mono"));

            var assemblyName = Assembly.GetName().Name;
            var assemblyLookupId = GeneratedIdentifier(string.Format("lookup_assembly_{0}",
                assemblyName.Replace('.', '_')));
            WriteLine("{0}();", assemblyLookupId);

            var @namespace = string.Empty;
            var ids = string.Join(", ",
                @class.QualifiedName.Split('.').Select(n => string.Format("\"{0}\"", n)));

            var monoImageName = string.Format("{0}_image", AssemblyId);
            WriteLine("{0} = mono_class_from_name({1}, \"{2}\", \"{3}\");",
                classId, monoImageName, @namespace, @class.OriginalName);
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
            var instanceId = GeneratedIdentifier("instance");

            var @class = method.Namespace as Class;
            var classId = string.Format("{0}_class", @class.QualifiedName);
            if (method.IsConstructor)
            {
                WriteLine("{0}* object = ({0}*) malloc(sizeof({0}));", @class.Name);
                WriteLine("MonoObject* {0} = mono_object_new({1}, {2});",
                    instanceId, GeneratedIdentifier("mono_domain"), classId);
                WriteLine("object->_handle = mono_gchandle_new({0}, /*pinned=*/false);",
                    instanceId);
            }
            else
            {
                WriteLine("MonoObject* {0} = mono_gchandle_get_target(object->_handle);",
                    instanceId);
            }

            var argsId = GeneratedIdentifier("args");
            WriteLine("void* {0} = 0;", argsId);

            var exceptionId = GeneratedIdentifier("exception");
            WriteLine("MonoObject* {0} = 0;", exceptionId);

            var objectId = method.IsConstructor ? "NULL" : instanceId;

            var resultId = GeneratedIdentifier("result");
            WriteLine("MonoObject* {0};", resultId);

            var methodId = GeneratedIdentifier("method");
            WriteLine("{0} = mono_runtime_invoke({1}, {2}, {3}, &{4});", resultId,
                methodId, objectId, argsId, exceptionId);
        }

        public override bool VisitMethodDecl(Method method)
        {
            PushBlock();

            GenerateMethodSignature(method);
            NewLine();
            WriteStartBraceIndent();

            if (method.IsConstructor)
                GenerateClassLookup(method);

            GenerateMethodLookup(method);

            var retType = method.ReturnType;
            var needsReturn = !retType.Type.IsPrimitiveType(PrimitiveType.Void);

            GenerateMethodInvocation(method);

            var exceptionId = GeneratedIdentifier("exception");
            //WriteLine("if ({0})", exceptionId);
            //WriteLineIndent("return 0;");

            var returnVar = method.IsConstructor ? "object" : "0";

            if (needsReturn)
                WriteLine("return {0};", returnVar);

            WriteCloseBraceIndent();

            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }
    }
}
