﻿﻿﻿﻿﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using IKVM.Reflection;
using System.Text.RegularExpressions;

namespace Embeddinator.Generators
{
    public static class DeclarationExtensions
    {
        public static string ManagedQualifiedName(this Declaration decl)
        {
            // Replace + with / since that's what mono_class_from_name expects for nested types.
            return ASTGenerator.ManagedNames[decl].Replace("+", "/");
        }
    }

    public class ASTGenerator
    {
        ASTContext ASTContext { get; set; }
        Options Options { get; set; }

        private Assembly CurrentAssembly;

        public static Dictionary<Declaration, string> ManagedNames
            = new Dictionary<Declaration, string>();

        public static Dictionary<TranslationUnit, Assembly> ManagedAssemblies
            = new Dictionary<TranslationUnit, Assembly>();

        public ASTGenerator(ASTContext context, Options options)
        {
            ASTContext = context;
            Options = options;
        }

        TranslationUnit GetTranslationUnit(Assembly assembly)
        {
            var assemblyName = Options.LibraryName ?? Path.GetFileName (assembly.Location);

            var unit = ASTContext.TranslationUnits.Find(m => m.FileName.Equals(assemblyName));
            if (unit != null)
                return unit;

            unit = ASTContext.FindOrCreateTranslationUnit(assemblyName);
            unit.FilePath = assemblyName;

            ManagedAssemblies[unit] = assembly;

            return unit;
        }

        public TranslationUnit Visit(Assembly assembly)
        {
            CurrentAssembly = assembly;

            var unit = GetTranslationUnit(assembly);

            foreach (var type in assembly.ExportedTypes)
            {
                if (!type.IsPublic)
                    continue;

                var typeInfo = type.GetTypeInfo();
                Visit(typeInfo);
            }

            CurrentAssembly = null;

            return unit;
        }

        public Namespace VisitNamespace(TypeInfo typeInfo)
        {
            var unit = GetTranslationUnit(typeInfo.Assembly);
            if (string.IsNullOrWhiteSpace(typeInfo.Namespace))
                return unit;

            var namespaces = typeInfo.Namespace.Split('.');

            Namespace currentNamespace = unit;

            foreach (var @namespace in namespaces)
                currentNamespace = currentNamespace.FindCreateNamespace(@namespace);

            return currentNamespace;
        }

        public Declaration Visit(TypeInfo typeInfo)
        {
            var @namespace = VisitNamespace(typeInfo);
            var decl = @namespace.Declarations.FirstOrDefault(
                d => d.Name == UnmangleTypeName(typeInfo.Name));

            // If we have already processed this declaration, return it.
            if (decl != null)
                return decl;

            if (typeInfo.IsGenericType && !typeInfo.IsGenericTypeDefinition)
                return Visit(typeInfo.GetGenericTypeDefinition().GetTypeInfo());

            if (typeInfo.IsEnum)
                decl = VisitEnum(typeInfo);
            else if (typeInfo.IsClass || typeInfo.IsInterface || typeInfo.IsValueType)
                decl = VisitRecord(typeInfo);
            else
                throw new Exception ("Could not visit type: " + typeInfo.ToString ());

            if (decl.Namespace == null)
                throw new Exception("Declaration should have a namespace");

            if (typeInfo.IsGenericParameter || typeInfo.IsGenericType || typeInfo.IsAndroidSubclass())
                decl.GenerationKind = GenerationKind.None;

            return decl;
        }

        static string UnmangleTypeName(string name)
        {
            return string.IsNullOrEmpty(name) ? string.Empty :  
                Regex.Replace(name, @"[^\p{L}]+", "_");
        }

        public void HandleBaseType(IKVM.Reflection.Type type, Class @class)
        {
            if (type.FullName == "System.Object" || type.FullName == "System.ValueType")
                return;

            var baseClass = Visit(type.GetTypeInfo()) as Class;
            var specifier = new BaseClassSpecifier { Type = new TagType(baseClass) };
            @class.Bases.Add(specifier);
        }

        public Class VisitRecord(TypeInfo type)
        {
            var @class = new Class
            {
                Name = UnmangleTypeName(type.Name),
                Type = ClassType.RefType,
                IsFinal = type.IsSealed
            };

            if (type.IsInterface)
                @class.Type = ClassType.Interface;

            if (type.IsValueType)
                @class.Type = ClassType.ValueType;

            HandleNamespace(type, @class);
            VisitMembers(type, @class);

            if (type.BaseType != null)
                HandleBaseType(type.BaseType, @class);

            foreach (var @interface in type.ImplementedInterfaces)
                HandleBaseType(@interface, @class);

            ManagedNames[@class] = type.FullName;

            return @class;
        }

        private void HandleNamespace(TypeInfo type, Declaration decl)
        {
            var @namespace = VisitNamespace(type);

            decl.Namespace = @namespace;
            @namespace.Declarations.Add(decl);
        }

        public Enumeration VisitEnum(TypeInfo type)
        {
            var underlyingType = type.GetEnumUnderlyingType();
            var @enum = new Enumeration
            {
                Name = UnmangleTypeName(type.Name),
                BuiltinType = VisitType(underlyingType).Type as BuiltinType
            };
            HandleNamespace(type, @enum);

            if (Options.GeneratorKind == GeneratorKind.CPlusPlus)
                @enum.Modifiers |= Enumeration.EnumModifiers.Scoped;

            bool flags = type.HasCustomAttribute("System", "FlagsAttribute");
            if (flags)
                @enum.Modifiers |= Enumeration.EnumModifiers.Flags;

            foreach (var item in type.DeclaredFields)
            {
                if (!item.IsLiteral)
                    continue;

                var enumItem = new Enumeration.Item
                {
                    Namespace = @enum,
                    Name = item.Name,
                    ExplicitValue = true
                };

                var rawValue = item.GetRawConstantValue();

                if (@enum.BuiltinType.IsUnsigned)
                    enumItem.Value = Convert.ToUInt64(rawValue);
                else
                    enumItem.Value = (ulong) Convert.ToInt64(rawValue);

                @enum.AddItem(enumItem);
            }

            ManagedNames[@enum] = type.FullName;

            return @enum;
        }

        public bool IsSystemObjectMethod(MethodInfo method)
        {
            if (method.Match ("System.Int32", "CompareTo", "System.Object"))
                return true;

            if (method.Match ("System.Boolean", "Equals", "System.Object"))
                return true;

            if (method.Match ("System.Int32", "GetHashCode"))
                return true;

            return false;
        }

        public void VisitMembers(TypeInfo type, Class @class)
        {
            foreach (var ctor in type.DeclaredConstructors)
            {
                if (ctor.IsStatic)
                    continue;

                if (!ctor.IsPublic)
                    continue;

                var decl = VisitConstructor(ctor, @class);
                @class.Declarations.Add(decl);
            }

            foreach (var method in type.DeclaredMethods)
            {
                if (!method.IsPublic && !method.IsExplicitInterfaceMethod())
                {
                    continue;
                }

                if (method.IsGenericMethod)
                    continue;

                if (IsSystemObjectMethod(method))
                    continue;

                var decl = VisitMethod(method);
                @class.Declarations.Add(decl);
            }

            foreach (var field in type.DeclaredFields)
            {
                if (!field.IsPublic)
                    continue;

                var decl = VisitField(field);
                @class.Declarations.Add(decl);
            }

            foreach (var @event in type.DeclaredEvents)
            {
                //var decl = VisitEvent(@event);
                //@class.Declarations.Add(decl);
            }

            foreach (var property in type.DeclaredProperties)
            {
                var decl = VisitProperty(property, @class);
                @class.Declarations.Add(decl);
            }

            foreach (var decl in @class.Declarations)
                decl.Namespace = @class;
        }

        public Method VisitConstructor(ConstructorInfo ctor, Class @class)
        {
            var method = VisitMethodBase(ctor);
            method.Kind = CXXMethodKind.Constructor;
            method.ReturnType = new QualifiedType(new TagType(@class));

            if (method.ReturnType.Type == null)
                method.Ignore = true;

            method.Name = "new";

            if (Options.GeneratorKind == GeneratorKind.ObjectiveC)
                method.Name = "init";

            return method;
        }

        string GetInternalTypeName(IKVM.Reflection.Type type)
        {
            // If true type is an array, a pointer, or is passed by reference.
            if (type.HasElementType)
            {
                var elementType = type.GetElementType();

                if (type.IsArray)
                    return GetInternalTypeName(elementType) + "[]";

                return GetInternalTypeName(elementType);
            }

            if (type.IsEnum)
                return type.FullName;

            switch(IKVM.Reflection.Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    if (type.FullName == "System.IntPtr")
                        return "intptr";
                    if (type.FullName == "System.UIntPtr")
                        return "uintptr";
                    if (type.FullName == "System.Object")
                        return "object";
                    return type.FullName;
                case TypeCode.Boolean:
                    return "bool";
                case TypeCode.Char:
                    return "char";
                case TypeCode.SByte:
                    return "sbyte";
                case TypeCode.Byte:
                    return "byte";
                case TypeCode.Int16:
                    return "int16";
                case TypeCode.UInt16:
                    return "uint16";
                case TypeCode.Int32:
                    return "int";
                case TypeCode.UInt32:
                    return "uint";
                case TypeCode.Int64:
                    return "long";
                case TypeCode.UInt64:
                    return "ulong";
                case TypeCode.Single:
                    return "single";
                case TypeCode.Double:
                    return "double";
                case TypeCode.String:
                    return "string";
                case TypeCode.Empty:
                    return "null";  
                case TypeCode.DBNull:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                    return type.FullName;
            }

            throw new NotImplementedException("No implementation for " + type);
        }

        string GetInternalMethodName(MethodBase method)
        {
            var @params = new List<string>();
            foreach (var p in method.GetParameters())
            {
                var param = GetInternalTypeName(p.ParameterType);
                if (p.IsOut || p.ParameterType.IsByRef)
                    param += "&";
                else if (p.ParameterType.IsPointer)
                    param += "*";

                @params.Add(param);
            }

            return string.Format("{0}:{1}({2})", method.DeclaringType.FullName,
                method.Name, string.Join(",", @params));
        }

        Method VisitMethod(MethodInfo methodInfo)
        {
            var method = VisitMethodBase(methodInfo);
            method.ReturnType = VisitType(methodInfo.ReturnType);

            if (method.ReturnType.Type == null
             || method.ReturnType.Type is UnsupportedType)
                method.Ignore = true;

            return method;
        }

        QualifiedType VisitType(IKVM.Reflection.Type managedType)
        {
            var isString = managedType.HasElementType && IKVM.Reflection.Type.GetTypeCode(
                managedType.GetElementType()) == TypeCode.String;

            if (managedType.IsByRef && isString)
                managedType = managedType.GetElementType();

            // If true type is an array, a pointer, or is passed by reference.
            if (managedType.HasElementType)
            {
                var managedElementType = managedType.GetElementType();
                var elementType = VisitType(managedElementType);

                if (managedType.IsByRef || managedType.IsPointer)
                {
                    var ptrType = new PointerType(elementType)
                    {
                        Modifier = (Options.GeneratorKind == GeneratorKind.CPlusPlus) ?
                            PointerType.TypeModifier.LVReference :
                            PointerType.TypeModifier.Pointer
                    };

                    return new QualifiedType(ptrType);
                }
                else if (managedType.IsArray)
                {
                    if (elementType.Type.IsClass() || Options.GeneratorKind == GeneratorKind.Java)
                        return new QualifiedType(new UnsupportedType { Description = managedType.FullName });

                    var array = new ArrayType
                    {
                        SizeType = ArrayType.ArraySize.Variable,
                        QualifiedType = elementType
                    };

                    return new QualifiedType(array);
                }

                throw new NotImplementedException();
            }

            if (managedType.IsEnum)
            {
                var @enum = Visit(managedType.GetTypeInfo());
                return new QualifiedType(new TagType(@enum));
            }

            CppSharp.AST.Type type = null;
            TypeQualifiers qualifiers = new TypeQualifiers();
            switch (IKVM.Reflection.Type.GetTypeCode(managedType))
            {
            case TypeCode.Empty:
                type = new BuiltinType(PrimitiveType.Null);
                break;
            case TypeCode.Object:
            case TypeCode.DateTime:
                if (managedType.FullName == "System.Void")
                {
                    type = new BuiltinType(PrimitiveType.Void);
                    break;
                }
                var currentUnit = GetTranslationUnit(CurrentAssembly);
                if (managedType.Assembly != ManagedAssemblies[currentUnit]
                    || managedType.IsGenericType)
                {
                    type = new UnsupportedType { Description = managedType.FullName };
                    break;
                }
                var decl = Visit(managedType.GetTypeInfo());
                type = new TagType(decl);
                break;
            case TypeCode.DBNull:
                type = new UnsupportedType() { Description = "DBNull" };
                break;
            case TypeCode.Boolean:
                type = new BuiltinType(PrimitiveType.Bool);
                break;
            case TypeCode.Char:
                type = new BuiltinType(PrimitiveType.Char);
                break;
            case TypeCode.SByte:
                type = new BuiltinType(PrimitiveType.SChar);
                break;
            case TypeCode.Byte:
                type = new BuiltinType(PrimitiveType.UChar);
                break;
            case TypeCode.Int16:
                type = new BuiltinType(PrimitiveType.Short);
                break;
            case TypeCode.UInt16:
                type = new BuiltinType(PrimitiveType.UShort);
                break;
            case TypeCode.Int32:
                type = new BuiltinType(PrimitiveType.Int);
                break;
            case TypeCode.UInt32:
                type = new BuiltinType(PrimitiveType.UInt);
                break;
            case TypeCode.Int64:
                type = new BuiltinType(PrimitiveType.Long);
                break;
            case TypeCode.UInt64:
                type = new BuiltinType(PrimitiveType.ULong);
                break;
            case TypeCode.Single:
                type = new BuiltinType(PrimitiveType.Float);
                break;
            case TypeCode.Double:
                type = new BuiltinType(PrimitiveType.Double);
                break;
            case TypeCode.String:
                type = new BuiltinType(PrimitiveType.String);
                break;
            case TypeCode.Decimal:
                type = new BuiltinType(PrimitiveType.Decimal);
                break;
            }

            return new QualifiedType(type, qualifiers);
        }

        /// <summary>
        /// Converts from a .NET member acccess mask to a C/C++ access specifier.
        /// </summary>
        /// <returns></returns>
        static AccessSpecifier ConvertMemberAttributesToAccessSpecifier(
            MethodAttributes mask)
        {
            switch (mask)
            {
            case MethodAttributes.PrivateScope:
            case MethodAttributes.Private:
                return AccessSpecifier.Private;
            case MethodAttributes.FamANDAssem:
            case MethodAttributes.Assembly:
            case MethodAttributes.Family:
            case MethodAttributes.FamORAssem:
                return AccessSpecifier.Internal;
            case MethodAttributes.Public:
                return AccessSpecifier.Public;
            }

            throw new NotImplementedException();
        }

        public Method VisitMethodBase(MethodBase methodBase)
        {
            var method = new Method
            {
                Kind = methodBase.IsConstructor ?
                    CXXMethodKind.Constructor : CXXMethodKind.Normal,
                IsFinal = methodBase.IsFinal
            };
            method.Name = UnmangleTypeName(methodBase.Name);

            ManagedNames[method] = GetInternalMethodName(methodBase);

            var parameters = methodBase.GetParameters();
            foreach (var param in parameters)
            {
                var paramDecl = VisitParameter(param);
                method.Parameters.Add(paramDecl);

                if (paramDecl.Ignore)
                    method.Ignore = true;
            }

            method.IsStatic = methodBase.IsStatic;
            method.IsVirtual = methodBase.IsVirtual;
            method.IsPure = methodBase.IsAbstract;

            var accessMask = (methodBase.Attributes & MethodAttributes.MemberAccessMask);
            method.Access = ConvertMemberAttributesToAccessSpecifier(accessMask);

            //NOTE: if this is an explicit interface method, mark it public and modify the name
            if (!methodBase.DeclaringType.IsAndroidSubclass() && methodBase.IsExplicitInterfaceMethod())
            {
                //We also need to check for collisions
                string name = method.Name.Split('.').Last();
                if (!methodBase.DeclaringType.GetMethods().Any(m => m.IsPublic && !m.IsStatic && m.Name == name))
                {
                    method.Access = AccessSpecifier.Public;
                    method.OriginalName =
                        method.Name = name;
                }
            }

            return method;
        }

        /// <summary>
        /// Converts from a .NET parameter to parameter usage specifier.
        /// </summary>
        /// <returns></returns>
        static ParameterUsage ConvertToParameterUsage(ParameterInfo param)
        {
            if (param.IsOut)
                return ParameterUsage.Out;

            if (param.ParameterType.IsByRef)
                return ParameterUsage.InOut;

            return ParameterUsage.In;
        }

        public Parameter VisitParameter(ParameterInfo paramInfo)
        {
            var param = new Parameter()
            {
                Name = UnmangleTypeName(paramInfo.Name),
                Usage = ConvertToParameterUsage(paramInfo),
                HasDefaultValue = paramInfo.HasDefaultValue,
                QualifiedType = VisitType(paramInfo.ParameterType)
            };

            var type = param.QualifiedType.Type;

            if (type == null || (type.IsPointer() && type.GetFinalPointee() == null) ||
                type is UnsupportedType)
                param.Ignore = true;

            if (paramInfo.ParameterType.ContainsGenericParameters)
                param.Ignore = true;

            return param;
        }

        /// <summary>
        /// Converts from a .NET field acccess mask to a C/C++ access specifier.
        /// </summary>
        /// <returns></returns>
        static AccessSpecifier ConvertFieldAttributesToAccessSpecifier(
            FieldAttributes mask)
        {
            switch (mask)
            {
                case FieldAttributes.PrivateScope:
                case FieldAttributes.Private:
                    return AccessSpecifier.Private;
                case FieldAttributes.FamANDAssem:
                case FieldAttributes.Assembly:
                case FieldAttributes.Family:
                case FieldAttributes.FamORAssem:
                    return AccessSpecifier.Internal;
                case FieldAttributes.Public:
                    return AccessSpecifier.Public;
            }

            throw new NotImplementedException();
        }

        public Field VisitField(FieldInfo fieldInfo)
        {
            var field = new Field
            {
                Name = UnmangleTypeName(fieldInfo.Name),
                Namespace = Visit(fieldInfo.DeclaringType.GetTypeInfo()) as Class,
                QualifiedType = VisitType(fieldInfo.FieldType),
                IsStatic = fieldInfo.IsStatic
            };

            if (field.Type is UnsupportedType)
                field.Ignore = true;

            var accessMask = (fieldInfo.Attributes & FieldAttributes.FieldAccessMask);
            field.Access = ConvertFieldAttributesToAccessSpecifier(accessMask);

            return field;
        }

        public Event VisitEvent(EventInfo @event)
        {
            throw new NotImplementedException();
        }

        public Property VisitProperty(PropertyInfo propertyInfo, Class @class)
        {
            var property = new Property
            {
                Name = UnmangleTypeName(propertyInfo.Name),
                Namespace = Visit(propertyInfo.DeclaringType.GetTypeInfo()) as Class,
                QualifiedType = VisitType(propertyInfo.PropertyType),
            };

            if (property.Type is UnsupportedType)
                property.Ignore = true;

            if (propertyInfo.GetMethod != null)
            {
                property.GetMethod = VisitMethod(propertyInfo.GetMethod);
                property.GetMethod.Namespace = property.Namespace;
            }

            if (propertyInfo.SetMethod != null)
            {
                property.SetMethod = VisitMethod(propertyInfo.SetMethod);
                property.SetMethod.Namespace = property.Namespace;
            }

            return property;
        }
    }

    public static class TypeExtensions {
        
        public static bool Is (this IKVM.Reflection.Type self, string @namespace, string name)
        {
            return (self.Namespace == @namespace) && (self.Name == name);
        }

        public static bool Match (this MethodInfo self, string returnType, string name,
            params string[] parameterTypes)
        {
            if (self.Name != name)
                return false;
            var parameters = self.GetParameters ();
            var pc = parameters.Length;
            if (pc != parameterTypes.Length)
                return false;
            if (self.ReturnType.FullName != returnType)
                return false;
            for (int i = 0; i < pc; i++) {
                // parameter type not specified, useful for generics
                if (parameterTypes [i] == null)
                    continue;
                if (parameterTypes [i] != parameters [i].ParameterType.FullName)
                    return false;
            }
            return true;
        }

        public static bool HasCustomAttribute (this IKVM.Reflection.Type self, string @namespace, string name)
        {
            foreach (var ca in self.CustomAttributes) {
                if (ca.AttributeType.Is (@namespace, name))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// NOTE: Explicit interface implementations will be IsVirtual=True and IsFinal=True
        /// See https://msdn.microsoft.com/en-us/library/system.reflection.methodbase.isfinal(v=vs.110).aspx
        /// </summary>
        public static bool IsExplicitInterfaceMethod(this MethodBase method)
        {
            return !method.IsPublic && method.IsVirtual && method.IsFinal;
        }

        public static bool IsAndroidSubclass (this IKVM.Reflection.Type type)
        {
            foreach (var @interface in type.GetInterfaces())
            {
                if (@interface.Assembly.IsAndroidAssembly())
                    return true;
            }

            do
            {
                if (type == null)
                    return false;
                if (type.Assembly.IsAndroidAssembly())
                    return true;

                type = type.BaseType;

            } while (true);
        }

        public static bool IsAndroidAssembly(this Assembly assembly)
        {
            return assembly.FullName.StartsWith("Mono.Android, ", StringComparison.Ordinal) || assembly.FullName.StartsWith("Java.Interop, ", StringComparison.Ordinal);
        }
    }
}

