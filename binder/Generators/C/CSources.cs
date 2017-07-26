using System;
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

        Options EmbedOptions => Options as Options;

        public override void WriteHeaders()
        {
            WriteLine("#include \"{0}.h\"", Unit.FileNameWithoutExtension);
            WriteInclude("glib.h");
            WriteInclude("mono_embeddinator.h");
            WriteInclude("c-support.h");
        }

        void RemoveTypedefNodes()
        {
            Unit.Declarations.RemoveAll(d => d is TypedefDecl);
        }

        public override void Process()
        {
            RemoveTypedefNodes();

            GenerateFilePreamble(CommentKind.BCPL, "Embeddinator-4000");

            PushBlock();
            WriteHeaders();
            PopBlock(NewLineKind.BeforeNextBlock);

            PushBlock();
            WriteLine("mono_embeddinator_context_t {0};", GeneratedIdentifier("mono_context"));
            WriteLine("MonoImage* {0}_image;", CGenerator.AssemblyId(Unit));
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
            if (!VisitDeclaration(@class))
                return false;

            GenerateClassLookup(@class);

            VisitDeclContext(@class);

            return true;
        }

        public void GenerateObjectDeclarations()
        {
            var referencedClasses = new GetReferencedDecls();
            Unit.Visit(referencedClasses);

            var classNames = referencedClasses.Declarations
                .Where(c => c != GenerateObjectTypesPass.MonoEmbedObject)
                .Where(c => c is Class || c is Enumeration)
                .Select(c => c.QualifiedName)
                .Distinct();

            foreach (var @class in classNames)
            {
                PushBlock();
                WriteLine($"static MonoClass* class_{@class} = 0;");
                PopBlock(NewLineKind.Never);
            }

            NewLine();
        }

        public void GenerateMonoInitialization()
        {
            PushBlock();
            WriteLine($"static void {GeneratedIdentifier("initialize_mono")}()");
            WriteStartBraceIndent();

            var contextId = GeneratedIdentifier("mono_context");
            WriteLine($"if ({contextId}.domain)");
            WriteLineIndent("return;");

            var domainName = "mono_embeddinator_binding";
            WriteLine($"mono_embeddinator_init(&{contextId}, \"{domainName}\");");

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateAssemblyLoad()
        {
            var assemblyName = Unit.FileName;
            var assemblyLookupId = GeneratedIdentifier($"lookup_assembly_{assemblyName.Replace('.', '_')}");

            PushBlock();
            WriteLine($"static void {assemblyLookupId}()");
            WriteStartBraceIndent();

            var monoImageName = string.Format("{0}_image", CGenerator.AssemblyId(Unit));
            WriteLine("if ({0})", monoImageName);
            WriteLineIndent("return;");

            WriteLine("{0} = mono_embeddinator_load_assembly(&{1}, \"{2}\");",
                monoImageName, GeneratedIdentifier("mono_context"), assemblyName);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public static string GenerateMonoClassFromNameCall(Declaration decl)
        {
            var namespaces = Declaration.GatherNamespaces(decl.Namespace)
                .Where(ns => !(ns is TranslationUnit));

            var @namespace = string.Join(".", namespaces);
            var ids = string.Join(", ",
                decl.QualifiedName.Split('.').Select(n => string.Format("\"{0}\"", n)));

            var monoImageName = string.Format("{0}_image", CGenerator.AssemblyId(decl.TranslationUnit));
            var managedName = decl.ManagedQualifiedName();

            var dotIndex = managedName.LastIndexOf(".", StringComparison.Ordinal);
            if (dotIndex > 0)
                managedName = managedName.Substring(managedName.LastIndexOf(".", StringComparison.Ordinal) + 1);

            return $"mono_class_from_name({monoImageName}, \"{@namespace}\", \"{managedName}\");";
        }

        public void GenerateClassLookup(Class @class)
        {
            PushBlock();

            var classLookupId = GeneratedIdentifier($"lookup_class_{@class.QualifiedName.Replace('.', '_')}");
            WriteLine("static void {0}()", classLookupId);
            WriteStartBraceIndent();

            var classId = $"class_{@class.QualifiedName}";
            WriteLine($"if ({classId} == 0)");
            WriteStartBraceIndent();

            WriteLine($"{GeneratedIdentifier("initialize_mono")}();");

            var assemblyName = Unit.FileName;
            var assemblyLookupId = GeneratedIdentifier($"lookup_assembly_{assemblyName.Replace('.', '_')}");
            WriteLine($"{assemblyLookupId}();");

            WriteLine($"{classId} = {GenerateMonoClassFromNameCall(@class)}");
            WriteCloseBraceIndent();
            WriteCloseBraceIndent();

            PopBlock(NewLineKind.BeforeNextBlock);
        }

        public void GenerateMethodLookup(Method method)
        {
            var methodNameId = GeneratedIdentifier("method_name");
            WriteLine($"const char {methodNameId}[] = \"{method.ManagedQualifiedName()}\";");

            var methodId = GeneratedIdentifier("method");
            WriteLine($"static MonoMethod *{methodId} = 0;");

            NewLine();

            WriteLine($"if (!{methodId})");
            WriteStartBraceIndent();

            var @class = method.Namespace as Class;
            var classLookupId = GeneratedIdentifier($"lookup_class_{@class.QualifiedName.Replace('.', '_')}");
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
            var objectId = GeneratedIdentifier("object");

            if (method.IsConstructor)
            {
                var alloc = GenerateClassObjectAlloc(@class.QualifiedName);
                WriteLine($"{@class.QualifiedName}* {objectId} = {alloc};");

                var classId = $"class_{@class.QualifiedName}";
                WriteLine("MonoObject* {0} = mono_object_new({1}.domain, {2});",
                    instanceId, GeneratedIdentifier("mono_context"), classId);

                if (Options.GeneratorKind == GeneratorKind.C)
                    WriteLine($"mono_embeddinator_init_object({objectId}, {instanceId});");
                else
                    WriteLine($"{objectId}->{0} = ({1}*) mono_embeddinator_create_object({2});",
                        CGenerator.ObjectInstanceId, GenerateObjectTypesPass.MonoEmbedObject.Name,
                        instanceId);
            }
            else if (!method.IsStatic)
            {
                var handle = GetMonoObjectField(Options, MonoObjectFieldUsage.Instance,
                    method.Parameters[0].Name, "_handle");
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
                param.Visit(marshal);

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

            if (method.IsVirtual)
            {
                var virtualMethodId = GeneratedIdentifier("virtual_method");
                WriteLine($"MonoMethod* {virtualMethodId} = mono_object_get_virtual_method({instanceId}, {methodId});");
                methodId = virtualMethodId;
            }

            var @class = method.Namespace as Class;
            if (@class.IsValueType && !method.IsStatic)
            {
                var unboxedId = CGenerator.GenId("unboxed");
                WriteLine($"void* {unboxedId} = mono_object_unbox({instanceId});");
                instanceId = unboxedId;
            }

            WriteLine("{0} = mono_runtime_invoke({1}, {2}, {3}, &{4});", resultId,
                methodId, instanceId, argsId, exceptionId);
            NewLine();

            WriteLine($"if ({exceptionId})");
            WriteStartBraceIndent();

            if (method.IsConstructor)
                WriteLine($"free({GeneratedIdentifier("object")});");

            WriteLine($"mono_embeddinator_throw_exception({exceptionId});");

            if (method.IsConstructor)
                WriteLine("return 0;");

            WriteCloseBraceIndent();

            foreach (var marshalContext in contexts)
            {
                if (!string.IsNullOrWhiteSpace(marshalContext.SupportAfter))
                    Write(marshalContext.SupportAfter);
            }
        }

        public override bool VisitMethodDecl(Method method)
        {
            if (!VisitDeclaration(method))
                return false;

            PushBlock();

            GenerateMethodSpecifier(method, method.Namespace as Class);
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
            if (!method.IsConstructor && needsReturn)
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
                returnCode = GeneratedIdentifier("object");
            }

            if (method.IsConstructor || needsReturn)
                WriteLine("return {0};", returnCode);

            WriteCloseBraceIndent();
            PopBlock(NewLineKind.BeforeNextBlock);

            return true;
        }

        public void GenerateFieldLookup(Field field)
        {
            var fieldId = GeneratedIdentifier("field");
            WriteLine($"static MonoClassField *{fieldId} = 0;");

            WriteLine($"if (!{fieldId})");
            WriteStartBraceIndent();

            var @class = field.Namespace as Class;
            var classLookupId = GeneratedIdentifier($"lookup_class_{@class.QualifiedName.Replace('.', '_')}");
            WriteLine($"{classLookupId}();");

            var fieldNameId = GeneratedIdentifier("field_name");
            WriteLine($"const char {fieldNameId}[] = \"{field.Name}\";");

            var classId = $"class_{@class.QualifiedName}";
            WriteLine($"{fieldId} = mono_class_get_field_from_name({classId}, {fieldNameId});");

            WriteCloseBraceIndent();
        }

        public override bool VisitProperty(Property property)
        {
            if (!VisitDeclaration(@property))
                return false;

            if (property.Field == null)
                return false;

            GenerateFieldGetter(property);
            NewLine();

            GenerateFieldSetter(property);
            NewLine();

            return true;
        }

        void GenerateFieldGetter(Property property)
        {
            var getter = property.GetMethod;

            GenerateMethodSpecifier(getter, getter.Namespace as Class);
            NewLine();
            WriteStartBraceIndent();

            var field = property.Field;
            GenerateFieldLookup(field);

            var instanceId = field.IsStatic ? "0" : GeneratedIdentifier("instance");
            var resultId = GeneratedIdentifier("result");

            if (!field.IsStatic)
            {
                var handle = GetMonoObjectField(Options, MonoObjectFieldUsage.Instance,
                    FixMethodParametersPass.ObjectParameterId, "_handle");
                WriteLine($"MonoObject* {instanceId} = mono_gchandle_get_target({handle});");
            }

            var fieldId = GeneratedIdentifier("field");
            var domainId = $"{GeneratedIdentifier("mono_context")}.domain";

            WriteLine($"MonoObject* {resultId} = mono_field_get_value_object({domainId}, {fieldId}, {instanceId});");

            var ctx = new MarshalContext(Context)
            {
                ArgName = resultId,
                ReturnVarName = resultId,
                ReturnType = property.QualifiedType
            };

            var marshal = new CMarshalManagedToNative(EmbedOptions, ctx);
            property.QualifiedType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                Write(marshal.Context.SupportBefore);

            WriteLine($"return {marshal.Context.Return.ToString()};");

            WriteCloseBraceIndent();
        }

        void GenerateFieldSetter(Property property)
        {
            var setter = property.SetMethod;
            var @class = property.Namespace as Class;

            GenerateMethodSpecifier(setter, setter.Namespace as Class);
            NewLine();
            WriteStartBraceIndent();

            var field = property.Field;
            var fieldId = GeneratedIdentifier("field");

            GenerateFieldLookup(field);

            var ctx = new MarshalContext(Context) { ArgName = "value" };
            var marshal = new CMarshalNativeToManaged(EmbedOptions, ctx);
            property.QualifiedType.Visit(marshal);

            if (!string.IsNullOrWhiteSpace(marshal.Context.SupportBefore))
                Write(marshal.Context.SupportBefore);

            var valueId = GeneratedIdentifier("value");
            WriteLine($"void* {valueId} = {marshal.Context.Return.ToString()};");

            if (field.IsStatic)
            {
                var vtableId = GeneratedIdentifier("vtable");
                var domainId = $"{GeneratedIdentifier("mono_context")}.domain";
                var classId = $"class_{@class.QualifiedName}";

                WriteLine ($"MonoVTable* {vtableId} = mono_class_vtable({domainId}, {classId});");
                WriteLine ($"mono_field_static_set_value({vtableId}, {fieldId}, {valueId});");
            }
            else
            {
                var instanceId = GeneratedIdentifier("instance");
                var handle = GetMonoObjectField(Options, MonoObjectFieldUsage.Instance,
                    FixMethodParametersPass.ObjectParameterId, "_handle");

                WriteLine($"MonoObject* {instanceId} = mono_gchandle_get_target({handle});");
                WriteLine ($"mono_field_set_value({instanceId}, {fieldId}, {valueId});");
            }

            WriteCloseBraceIndent();
        }
    }
}
