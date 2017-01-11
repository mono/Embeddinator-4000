using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using System.Linq;
using System.Collections.Generic;
using MonoEmbeddinator4000.Passes;

namespace MonoEmbeddinator4000.Generators
{
    public class CSources : CTemplate
    {
        public CSources(BindingContext context, Options options, TranslationUnit unit)
         : base(context, options, unit)
        {
        }

        public override string FileExtension
        {
            get { return "c"; }
        }

        public string AssemblyId
        {
            get { return CGenerator.AssemblyId(Unit); }
        }

        public override void WriteHeaders()
        {
            WriteLine("#include \"{0}.h\"", Unit.FileNameWithoutExtension);
            WriteLine("#include \"glib.h\"");
            WriteLine("#include <mono/jit/jit.h>");
            WriteLine("#include <mono/metadata/assembly.h>");
            WriteLine("#include <mono/metadata/object.h>");
            WriteLine("#include <mono/metadata/mono-config.h>");
            WriteLine("#include <mono/metadata/debug-helpers.h>");
        }

        void RemoveTypedefNodes()
        {
            Unit.Declarations.RemoveAll(d => d is TypedefDecl);
        }

        public override void Process()
        {
            RemoveTypedefNodes();

            GenerateFilePreamble();

            PushBlock();
            WriteHeaders();
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock();
            WriteLine("mono_m2n_context_t {0};", GeneratedIdentifier("mono_context"));
            WriteLine("MonoImage* {0}_image;", AssemblyId);
            PopBlock(NewLineKind.BeforeNextBlock);

            GenerateObjectDeclarations();

            GenerateMonoInitialization();
            GenerateAssemblyLoad();

            VisitDeclContext(Unit);
        }

        public override bool VisitEnumDecl(Enumeration @enum)
        {
            return true;
        }

        public override bool VisitClassDecl(Class @class)
        {
            GenerateClassLookup(@class);

            VisitDeclContext(@class);

            return true;
        }

        public void GenerateObjectDeclarations()
        {
            var referencedClasses = new GetReferencedClasses();
            Unit.Visit(referencedClasses);

            foreach (var @class in referencedClasses.Classes)
            {
                if (@class == GenerateObjectTypesPass.MonoEmbedObject)
                    continue;

                PushBlock();
                WriteLine("static MonoClass* {0}_class = 0;", @class.QualifiedName);
                PopBlock(NewLineKind.Never);
            }

            NewLine();
        }

        public void GenerateMonoInitialization()
        {
            PushBlock();
            WriteLine("static void {0}()", GeneratedIdentifier("initialize_mono"));
            WriteStartBraceIndent();

            var contextId = GeneratedIdentifier("mono_context");
            WriteLine("if ({0}.domain)", contextId);
            WriteLineIndent("return;");

            var domainName = "mono_embeddinator_binding";
            WriteLine("mono_m2n_init(&{0}, \"{1}\");", contextId, domainName);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateAssemblyLoad()
        {
            var assemblyName = Unit.FileName;
            var assemblyLookupId = GeneratedIdentifier(string.Format("lookup_assembly_{0}",
                assemblyName.Replace('.', '_')));

            PushBlock();
            WriteLine("static void {0}()", assemblyLookupId);
            WriteStartBraceIndent();

            var monoImageName = string.Format("{0}_image", AssemblyId);
            WriteLine("if ({0})", monoImageName);
            WriteLineIndent("return;");

            WriteLine("{0} = mono_m2n_load_assembly(&{1}, \"{2}\");",
                monoImageName, GeneratedIdentifier("mono_context"), assemblyName);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateClassLookup(Class @class)
        {
            PushBlock();

            var classLookupId = GeneratedIdentifier(string.Format("lookup_class_{0}",
                @class.QualifiedName.Replace('.', '_')));
            WriteLine("static void {0}()", classLookupId);
            WriteStartBraceIndent();

            var classId = string.Format("{0}_class", @class.QualifiedName);

            WriteLine("if ({0} == 0)", classId);
            WriteStartBraceIndent();

            WriteLine("{0}();", GeneratedIdentifier("initialize_mono"));

            var assemblyName = Unit.FileName;
            var assemblyLookupId = GeneratedIdentifier(string.Format("lookup_assembly_{0}",
                assemblyName.Replace('.', '_')));
            WriteLine("{0}();", assemblyLookupId);

            var namespaces = Declaration.GatherNamespaces(@class.Namespace);
            namespaces.Reverse();
            namespaces.Remove(namespaces.First());

            var @namespace = string.Join(".", namespaces);
            var ids = string.Join(", ",
                @class.QualifiedName.Split('.').Select(n => string.Format("\"{0}\"", n)));

            var monoImageName = string.Format("{0}_image", AssemblyId);
            WriteLine("{0} = mono_class_from_name({1}, \"{2}\", \"{3}\");",
                classId, monoImageName, @namespace, @class.OriginalName);
            WriteCloseBraceIndent();
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

            var retType = method.ReturnType;

            WriteLine("if ({0} == 0)", methodId);
            WriteStartBraceIndent();

            var errorId = GeneratedIdentifier("error");
            WriteLine("mono_m2n_error_t {0};", errorId);
            WriteLine("{0}.type = MONO_M2N_METHOD_LOOKUP_FAILED;", errorId);
            WriteLine("{0}.string = {1};", errorId, methodNameId);
            WriteLine("mono_m2n_error({0});", errorId);

            WriteCloseBraceIndent();
        }

        public static string GetMonoObjectField(Options options, string @object, string field)
        {
            switch (options.Language)
            {
            case GeneratorKind.C:
                return $"{@object}->{field}";
            default:
                return $"{@object}->_object->{field}";
            }
        }

        public void GenerateMethodGCHandleLookup(Method method)
        {
            var @class = method.Namespace as Class;
            var classId = string.Format("{0}_class", @class.QualifiedName);
            var instanceId = GeneratedIdentifier("instance");
            var handle = GetMonoObjectField(Options, "object", "_handle");

            if (method.IsConstructor)
            {
                WriteLine("{0}* object = ({0}*) calloc(1, sizeof({0}));", @class.QualifiedName);
                WriteLine("MonoObject* {0} = mono_object_new({1}.domain, {2});",
                    instanceId, GeneratedIdentifier("mono_context"), classId);

                WriteLine($"{handle} = mono_gchandle_new({instanceId}, /*pinned=*/false);");
            }
            else if (!method.IsStatic)
            {
                WriteLine($"MonoObject* {instanceId} = mono_gchandle_get_target({handle});");
            }
        }

        public void GenerateMethodInvocation(Method method)
        {
            GenerateMethodGCHandleLookup(method);

            var paramsToMarshal = method.Parameters.Where(p => !p.IsImplicit);
            var numParamsToMarshal = paramsToMarshal.Count();

            var argsId = "0";

            if (numParamsToMarshal > 0)
            {
                argsId = GeneratedIdentifier("args");
                WriteLine("void* {0}[{1}];", argsId, numParamsToMarshal);
            }

            var contexts = new List<MarshalContext>();

            int paramIndex = 0;
            foreach (var param in paramsToMarshal)
            {
                var ctx = new MarshalContext(Context)
                {
                    ArgName = param.Name,
                    Parameter = param,
                    ParameterIndex = paramIndex
                };
                contexts.Add(ctx);

                var marshal = new CMarshalNativeToManaged(Options, ctx);
                param.QualifiedType.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine("{0}[{1}] = {2};", argsId, paramIndex++,
                    marshal.Context.Return.ToString());
            }

            var exceptionId = GeneratedIdentifier("exception");
            WriteLine("MonoObject* {0} = 0;", exceptionId);

            var resultId = GeneratedIdentifier("result");
            WriteLine("MonoObject* {0};", resultId);

            var methodId = GeneratedIdentifier("method");
            var instanceId = method.IsStatic ? "0" : GeneratedIdentifier("instance");

            WriteLine("{0} = mono_runtime_invoke({1}, {2}, {3}, &{4});", resultId,
                methodId, instanceId, argsId, exceptionId);

            WriteLine("if ({0} != 0)", exceptionId);
            WriteStartBraceIndent();
            var errorId = GeneratedIdentifier("error");
            WriteLine("mono_m2n_error_t {0};", errorId);
            WriteLine("{0}.type = MONO_M2N_EXCEPTION_THROWN;", errorId);
            WriteLine("{0}.exception = (MonoException*) {1};", errorId, exceptionId);
            WriteLine("mono_m2n_error({0});", errorId);
            WriteCloseBraceIndent();

            foreach (var marshalContext in contexts)
            {
                if (!string.IsNullOrWhiteSpace (marshalContext.SupportAfter))
                    Write (marshalContext.SupportAfter);
            }
        }

        public override bool VisitMethodDecl(Method method)
        {
            PushBlock();

            GenerateMethodSignature(method);
            NewLine();
            WriteStartBraceIndent();

            var @class = method.Namespace as Class;
            var classLookupId = GeneratedIdentifier(string.Format("lookup_class_{0}",
                @class.QualifiedName.Replace('.', '_')));
            WriteLine("{0}();", classLookupId);

            GenerateMethodLookup(method);

            var retType = method.ReturnType;
            var needsReturn = !retType.Type.IsPrimitiveType(PrimitiveType.Void);

            GenerateMethodInvocation(method);

            string returnCode = "0";

            // Marshal the method result to native code.
            if (!method.IsConstructor)
            {
                var resultId = GeneratedIdentifier("result");
                var ctx = new MarshalContext(Context)
                {
                    ArgName = resultId,
                    ReturnVarName = resultId,
                    ReturnType = retType
                };

                var marshal = new CMarshalManagedToNative(Options, ctx);
                retType.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                returnCode = marshal.Context.Return.ToString();
            }
            else
            {
                returnCode = "object";
            }

            if (method.IsConstructor || needsReturn)
                WriteLine("return {0};", returnCode);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }
    }
}
