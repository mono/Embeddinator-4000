using System.Collections.Generic;
using System.Linq;
using CppSharp;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using MonoEmbeddinator4000.Passes;

namespace MonoEmbeddinator4000.Generators
{
    public class CSources : CCodeGenerator
    {
        public CSources(BindingContext context, TranslationUnit unit)
         : base(context, unit)
        {
        }

        public override string FileExtension => "c";

        public string AssemblyId => CGenerator.AssemblyId(Unit);

        Options EmbedOptions => Options as Options;

        public override void WriteHeaders()
        {
            WriteLine("#include \"{0}.h\"", Unit.FileNameWithoutExtension);
            WriteLine("#include \"glib.h\"");
            WriteLine("#include <mono/jit/jit.h>");
            WriteLine("#include <mono/metadata/assembly.h>");
            WriteLine("#include <mono/metadata/object.h>");
            WriteLine("#include <mono/metadata/mono-config.h>");
        }

        void RemoveTypedefNodes()
        {
            Unit.Declarations.RemoveAll(d => d is TypedefDecl);
        }

        public override void Process()
        {
            RemoveTypedefNodes();

            GenerateFilePreamble(CommentKind.BCPL);

            PushBlock();
            WriteHeaders();
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock();
            WriteLine("mono_embeddinator_context_t {0};", GeneratedIdentifier("mono_context"));
            WriteLine("MonoImage* {0}_image;", AssemblyId);
            PopBlock(NewLineKind.BeforeNextBlock);

            GenerateObjectDeclarations();

            GenerateGlobalMethods();

            VisitDeclContext(Unit);
        }

        public virtual void GenerateGlobalMethods()
        {
            GenerateMonoInitialization();
            GenerateAssemblyLoad();
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
                WriteLine($"static MonoClass* class_{@class.QualifiedName} = 0;");
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
            WriteLine("mono_embeddinator_init(&{0}, \"{1}\");", contextId, domainName);

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

            WriteLine("{0} = mono_embeddinator_load_assembly(&{1}, \"{2}\");",
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

            var classId = $"class_{@class.QualifiedName}";
            WriteLine("if ({0} == 0)", classId);
            WriteStartBraceIndent();

            WriteLine("{0}();", GeneratedIdentifier("initialize_mono"));

            var assemblyName = Unit.FileName;
            var assemblyLookupId = GeneratedIdentifier(string.Format("lookup_assembly_{0}",
                assemblyName.Replace('.', '_')));
            WriteLine("{0}();", assemblyLookupId);

            var namespaces = Declaration.GatherNamespaces(@class.Namespace)
                .Where(ns => !(ns is TranslationUnit));

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

            var methodId = GeneratedIdentifier("method");
            WriteLine($"static MonoMethod *{methodId} = 0;");

            NewLine();

            WriteLine($"if (!{methodId})");
            WriteStartBraceIndent();

            var @class = method.Namespace as Class;
            var classLookupId = GeneratedIdentifier(string.Format("lookup_class_{0}",
                @class.QualifiedName.Replace('.', '_')));
            WriteLine($"{classLookupId}();");

            var classId = $"class_{@class.QualifiedName}";
            WriteLine($"{methodId} = mono_embeddinator_lookup_method({methodNameId}, {classId});");

            WriteCloseBraceIndent();
        }

        public enum MonoObjectFieldUsage
        {
            Parameter,
            Instance
        }

        public static string GetMonoObjectField(DriverOptions options, MonoObjectFieldUsage usage,
            string @object, string field)
        {
            switch (options.GeneratorKind)
            {
            case GeneratorKind.C:
                return $"{@object}->{field}";
            default:
                var objExpr = usage == MonoObjectFieldUsage.Instance ? string.Empty : $"{@object}->";
                return $"{objExpr}{CGenerator.ObjectInstanceId}->{field}";
            }
        }

        public virtual void GenerateMethodInitialization(Method method)
        {
            var @class = method.Namespace as Class;
            var instanceId = GeneratedIdentifier("instance");

            if (method.IsConstructor)
            {
                var alloc = GenerateClassObjectAlloc(@class.QualifiedName);
                WriteLine($"{@class.QualifiedName}* object = {alloc};");

                var classId = $"class_{@class.QualifiedName}";
                WriteLine("MonoObject* {0} = mono_object_new({1}.domain, {2});",
                    instanceId, GeneratedIdentifier("mono_context"), classId);

                if (Options.GeneratorKind == GeneratorKind.C)
                    WriteLine($"mono_embeddinator_init_object(object, {instanceId});");
                else
                    WriteLine("object->{0} = ({1}*) mono_embeddinator_create_object({2});",
                        CGenerator.ObjectInstanceId, GenerateObjectTypesPass.MonoEmbedObject.Name,
                        instanceId);
            }
            else if (!method.IsStatic)
            {
                var handle = GetMonoObjectField(Options, MonoObjectFieldUsage.Instance,
                    FixMethodParametersPass.ObjectParameterId, "_handle");
                WriteLine($"MonoObject* {instanceId} = mono_gchandle_get_target({handle});");
            }
        }

        public void GenerateMethodInvocation(Method method)
        {
            GenerateMethodInitialization(method);
            NewLine();

            var paramsToMarshal = method.Parameters.Where(p => !p.IsImplicit);
            var numParamsToMarshal = paramsToMarshal.Count();

            var argsId = "0";

            if (numParamsToMarshal > 0)
            {
                argsId = GeneratedIdentifier("args");
                WriteLine($"void* {argsId}[{numParamsToMarshal}];");
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

                var marshal = new CMarshalNativeToManaged(EmbedOptions, ctx);
                param.QualifiedType.Visit(marshal);

                if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                    Write(marshal.Context.SupportBefore);

                WriteLine($"{argsId}[{paramIndex++}] = {marshal.Context.Return};");
            }

            var exceptionId = GeneratedIdentifier("exception");
            WriteLine($"MonoObject* {exceptionId} = 0;");

            var resultId = GeneratedIdentifier("result");
            WriteLine($"MonoObject* {resultId};");

            var methodId = GeneratedIdentifier("method");
            var instanceId = method.IsStatic ? "0" : GeneratedIdentifier("instance");

            WriteLine("{0} = mono_runtime_invoke({1}, {2}, {3}, &{4});", resultId,
                methodId, instanceId, argsId, exceptionId);
            NewLine();

            WriteLine($"if ({exceptionId})");
            WriteLineIndent($"mono_embeddinator_throw_exception({exceptionId});");

            foreach (var marshalContext in contexts)
            {
                if (!string.IsNullOrWhiteSpace(marshalContext.SupportAfter))
                    Write(marshalContext.SupportAfter);
            }
        }

        public override bool VisitMethodDecl(Method method)
        {
            PushBlock();

            GenerateMethodSignature(method);
            NewLine();
            WriteStartBraceIndent();

            GenerateMethodLookup(method);
            NewLine();

            var retType = method.ReturnType;
            var needsReturn = !retType.Type.IsPrimitiveType(PrimitiveType.Void);

            GenerateMethodInvocation(method);
            NewLine();

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

                var marshal = new CMarshalManagedToNative(EmbedOptions, ctx);
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
