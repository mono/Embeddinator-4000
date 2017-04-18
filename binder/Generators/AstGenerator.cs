using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CppSharp.AST;
using CppSharp.AST.Extensions;
using CppSharp.Generators;
using IKVM.Reflection;

namespace MonoEmbeddinator4000.Generators
{
    public static class DeclarationExtensions
    {
        public static string ManagedQualifiedName(this Declaration decl)
        {
            return ASTGenerator.ManagedNames[decl];
        }
    }

    public class ASTGenerator
    {
        ASTContext ASTContext { get; set; }
        Options Options { get; set; }

        private Assembly CurrentAssembly;

        public static Dictionary<Declaration, string> ManagedNames
            = new Dictionary<Declaration, string>();

        public ASTGenerator(ASTContext context, Options options)
        {
            ASTContext = context;
            Options = options;
        }

        TranslationUnit GetTranslationUnit(Assembly assembly)
        {
            var assemblyName = Path.GetFileName(assembly.Location);
            return GetTranslationUnit(assemblyName);
        }

        TranslationUnit GetTranslationUnit(string assemblyName)
        {
            var unit = ASTContext.TranslationUnits.Find(m => m.FileName.Equals(assemblyName));
            if (unit != null)
                return unit;

            unit = ASTContext.FindOrCreateTranslationUnit(assemblyName);
            unit.FilePath = assemblyName;

            return unit;
        }

        public TranslationUnit Visit(Assembly assembly)
        {
            CurrentAssembly = assembly;

            var assemblyName = Path.GetFileName (assembly.Location);
            var name = Options.LibraryName ?? assemblyName;

            var unit = GetTranslationUnit(name);

            foreach (var type in assembly.ExportedTypes)
            {
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
                d => d.Name == typeInfo.Name);

            // If we have already processed this declaration, return it.
            if (decl != null)
                return decl;

            if (typeInfo.IsEnum)
                decl = VisitEnum(typeInfo);
            else if (typeInfo.IsClass || typeInfo.IsInterface || typeInfo.IsValueType)
                decl = VisitRecord(typeInfo);
            else
                throw new Exception ("Could not visit type: " + typeInfo.ToString ());

            if (decl.Namespace == null)
                throw new Exception("Declaration should have a namespace");

            return decl;
        }

        static string UnmangleTypeName(string name)
        {
            return string.IsNullOrEmpty(name) ? string.Empty :
                         name.Replace(new char[] {'`', '<', '>' }, "_");
        }

        public Class VisitRecord(TypeInfo type)
        {
            var @class = new Class
            {
                Name = UnmangleTypeName(type.Name),
                Type = type.IsInterface ? ClassType.Interface : ClassType.RefType,
                IsFinal = type.IsSealed
            };

            HandleNamespace(type, @class);
            VisitMembers(type, @class);

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
                @enum.Modifiers = Enumeration.EnumModifiers.Scoped;

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

            return @enum;
        }

        public void VisitMembers(TypeInfo type, Class @class)
        {
            foreach (var ctor in type.DeclaredConstructors)
            {
                var decl = VisitConstructor(ctor, @class);
                @class.Declarations.Add(decl);
            }

            foreach (var method in type.DeclaredMethods)
            {
                if (method.IsGenericMethod)
                    continue;

                var decl = VisitMethod(method, @class);
                @class.Declarations.Add(decl);
            }

            foreach (var field in type.DeclaredFields)
            {
                var decl = VisitField(field);
                
                // TODO: Ignore fields until we implement usage of FieldToPropertyPass
                //@class.Declarations.Add(decl);
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

        Method VisitMethod(MethodInfo methodInfo, Class @class)
        {
            var method = VisitMethodBase(methodInfo);
            method.ReturnType = VisitType(methodInfo.ReturnType);

            if (method.ReturnType.Type == null)
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
            case TypeCode.Decimal:
            case TypeCode.DateTime:
                if (managedType.FullName == "System.Void")
                {
                    type = new BuiltinType(PrimitiveType.Void);
                    break;
                }
                var currentUnit = GetTranslationUnit(CurrentAssembly);
                if (managedType.Assembly.GetName().Name != currentUnit.FileNameWithoutExtension
                    || managedType.IsGenericType)
                {
                    type = new UnsupportedType { Description = managedType.FullName };
                    break;
                }
                var decl = Visit(managedType.GetTypeInfo());
                type = new TagType(decl);
                break;
            case TypeCode.DBNull:
                throw new NotSupportedException();
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
            //method.IsFinal = methodBase.IsFinal;
            var accessMask = (methodBase.Attributes & MethodAttributes.MemberAccessMask);
            method.Access = ConvertMemberAttributesToAccessSpecifier(accessMask);

            return method;
        }

        /// <summary>
        /// Converts from a .NET parameter to parameter usage specifier.
        /// </summary>
        /// <returns></returns>
        static ParameterUsage ConvertToParameterUsage(ParameterInfo param)
        {
            if (param.ParameterType.IsByRef)
                return ParameterUsage.InOut;
            else if (param.IsOut)
                return ParameterUsage.Out;

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

            if (type == null || (type.IsPointer() && type.GetFinalPointee() == null))
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
            var field = new Field()
            {
                Name = UnmangleTypeName(fieldInfo.Name),
                QualifiedType = VisitType(fieldInfo.FieldType)
            };

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
            var property = new Property()
            {
                Name = UnmangleTypeName(propertyInfo.Name),
                QualifiedType = VisitType(propertyInfo.PropertyType),
            };

            if (propertyInfo.GetMethod != null)
                property.GetMethod = VisitMethod(propertyInfo.GetMethod, @class);

            if (propertyInfo.SetMethod != null)
                property.SetMethod = VisitMethod(propertyInfo.SetMethod, @class);

            return property;
        }
    }
}
