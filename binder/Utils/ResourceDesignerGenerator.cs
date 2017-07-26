using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using CppSharp;
using Microsoft.CSharp;

namespace MonoEmbeddinator4000
{
    /// <summary>
    /// This class is responsible for generating Resource.designer.dll
    /// - Due to the way Android Resources work, we need to re-map resource Ids from the final APK
    /// - We use JNI to read Java fields and copy their values over to C#
    /// - We are relying on Xamarin.Android's ResourceIdManager to pick up this assembly when needed: https://github.com/xamarin/xamarin-android/blob/master/src/Mono.Android/Android.Runtime/ResourceIdManager.cs
    /// - We also tell Xamarin.Android to load Resource.designer.dll at startup in AndroidImpl.java
    /// </summary>
    class ResourceDesignerGenerator
    {
        readonly CodeCompileUnit unit = new CodeCompileUnit();
        readonly CSharpCodeProvider csc = new CSharpCodeProvider();
        readonly CodeGeneratorOptions options = new CodeGeneratorOptions();

        public IList<IKVM.Reflection.Assembly> Assemblies { get; set; }

        public string MainAssembly { get; set; }

        public string OutputDirectory { get; set; }

        public string PackageName { get; set; }

        public static class Java
        {
            public static string ToJavaName(string name)
            {
                //NOTE: aapt seems to modify the names for Java, examples so far:
                // customView -> customview
                // Theme -> Theme
                // Theme_hello -> Theme_hello
                return name[0] + name.Substring(1, name.Length - 1).ToLowerInvariant();
            }
        }

        public void Generate()
        {
            unit.AssemblyCustomAttributes.Add(new CodeAttributeDeclaration("Android.Runtime.ResourceDesignerAttribute",
                new CodeAttributeArgument(new CodeSnippetExpression("\"__embeddinator__.Resource\"")),
                new CodeAttributeArgument("IsApplication", new CodeSnippetExpression("true"))));

            var ns = new CodeNamespace("__embeddinator__");
            ns.Imports.Add(new CodeNamespaceImport("System"));
            ns.Imports.Add(new CodeNamespaceImport("Android.Runtime"));
            unit.Namespaces.Add(ns);

            var resource = new CodeTypeDeclaration("Resource")
            {
                Attributes = MemberAttributes.Public,
            };
            ns.Types.Add(resource);

            resource.CustomAttributes.Add(new CodeAttributeDeclaration("System.CodeDom.Compiler.GeneratedCodeAttribute",
                new CodeAttributeArgument(new CodeSnippetExpression("\"Xamarin.Android.Build.Tasks\"")),
                new CodeAttributeArgument(new CodeSnippetExpression("\"1.0.0.0\""))));

            var readFieldInt = new CodeMemberMethod
            {
                Name = "ReadFieldInt",
                Attributes = MemberAttributes.Private | MemberAttributes.Static,
                ReturnType = new CodeTypeReference("Int32"),
            };
            readFieldInt.Parameters.Add(new CodeParameterDeclarationExpression("IntPtr", "R"));
            readFieldInt.Parameters.Add(new CodeParameterDeclarationExpression("String", "fieldName"));
            resource.Members.Add(readFieldInt);

            readFieldInt.Statements.Add(new CodeAssignStatement(
                new CodeSnippetExpression("IntPtr fieldId"),
                new CodeSnippetExpression("JNIEnv.GetStaticFieldID(R, fieldName, \"I\")")));
            readFieldInt.Statements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression("JNIEnv.GetStaticIntField(R, fieldId)")));

            var readFieldArray = new CodeMemberMethod
            {
                Name = "ReadFieldArray",
                Attributes = MemberAttributes.Private | MemberAttributes.Static,
                ReturnType = new CodeTypeReference("Int32[]"),
            };
            readFieldArray.Parameters.Add(new CodeParameterDeclarationExpression("IntPtr", "R"));
            readFieldArray.Parameters.Add(new CodeParameterDeclarationExpression("String", "fieldName"));
            resource.Members.Add(readFieldArray);

            readFieldArray.Statements.Add(new CodeAssignStatement(
                new CodeSnippetExpression("IntPtr fieldId"),
                new CodeSnippetExpression("JNIEnv.GetStaticFieldID(R, fieldName, \"[I\")")));
            readFieldArray.Statements.Add(new CodeAssignStatement(
                new CodeSnippetExpression("IntPtr value"),
                new CodeSnippetExpression("JNIEnv.GetStaticObjectField(R, fieldId)")));
            readFieldArray.Statements.Add(new CodeMethodReturnStatement(new CodeSnippetExpression("JNIEnv.GetArray<Int32>(value)")));

            var updateIdValues = new CodeMemberMethod
            {
                Name = "UpdateIdValues",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
            };
            resource.Members.Add(updateIdValues);

            updateIdValues.Statements.Add(new CodeVariableDeclarationStatement("IntPtr", "R"));

            foreach (var assembly in Assemblies)
            {
                foreach (var type in assembly.DefinedTypes)
                {
                    if (type.Name == "Resource" &&
                        type.CustomAttributes.Any(a =>
                                                  a.AttributeType.FullName == "System.CodeDom.Compiler.GeneratedCodeAttribute" &&
                                                  a.ConstructorArguments.Count > 0 &&
                                                  a.ConstructorArguments[0].Value.ToString() == "Xamarin.Android.Build.Tasks"))
                    {
                        foreach (var nested in type.DeclaredNestedTypes)
                        {
                            if (nested.DeclaredFields.Any())
                            {
                                string innerClass = nested.Name == "Attribute" ? "attr" : nested.Name.ToLowerInvariant();
                                updateIdValues.Statements.Add(new CodeAssignStatement(
                                    new CodeSnippetExpression("R"),
                                    new CodeSnippetExpression($"JNIEnv.FindClass(\"{PackageName}.R${innerClass}\")")));

                                foreach (var field in nested.DeclaredFields)
                                {
                                    string javaName = nested.Name == "String" ? field.Name : Java.ToJavaName(field.Name);

                                    CodeSnippetExpression right, left = new CodeSnippetExpression(type.FullName + "." + nested.Name + "." + field.Name);
                                    if (field.FieldType.FullName == "System.Int32")
                                    {
                                        right = new CodeSnippetExpression($"{readFieldInt.Name}(R, \"{javaName}\")");
                                    }
                                    else if (field.FieldType.FullName == "System.Int32[]")
                                    {
                                        right = new CodeSnippetExpression($"{readFieldArray.Name}(R, \"{javaName}\")");
                                    }
                                    else
                                    {
                                        throw new Exception($"Type {field.FieldType.FullName} from member {nested.FullName}.{field.Name} not supported for Resource fields!");
                                    }
                                    updateIdValues.Statements.Add(new CodeAssignStatement(left, right));
                                }
                            }
                        }
                    }
                }
            }


        }

        public bool WriteAssembly()
        {
            var parameters = new CompilerParameters
            {
                //NOTE: we place this assembly in the output directory, the linker will move it to the final folder
                OutputAssembly = Path.Combine(OutputDirectory, "Resource.designer.dll"),
            };
            parameters.ReferencedAssemblies.Add(XamarinAndroid.FindAssembly("System.dll"));
            parameters.ReferencedAssemblies.Add(XamarinAndroid.FindAssembly("System.Runtime.dll"));
            parameters.ReferencedAssemblies.Add(XamarinAndroid.FindAssembly("Java.Interop.dll"));
            parameters.ReferencedAssemblies.Add(XamarinAndroid.FindAssembly("Mono.Android.dll"));
            parameters.ReferencedAssemblies.Add(MainAssembly);

            var results = csc.CompileAssemblyFromDom(parameters, unit);
            if (results.Errors.HasErrors)
            {
                foreach (var error in results.Errors)
                {
                    Diagnostics.Error("Error: {0}", error);
                }
                return false;
            }

            return true;
        }

        public string WriteSource()
        {
            string resourcePath = Path.Combine(OutputDirectory, "Resource.designer.cs");
            using (var stream = File.Create(resourcePath))
            using (var writer = new StreamWriter(stream))
            {
                csc.GenerateCodeFromCompileUnit(unit, writer, options);
            }
            return resourcePath;
        }

        public string ToSource()
        {
            var builder = new StringBuilder();
            using (var writer = new StringWriter(builder))
            {
                csc.GenerateCodeFromCompileUnit(unit, writer, options);
            }
            return builder.ToString();
        }
    }
}