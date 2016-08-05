using System;
using IKVM.Reflection;
using CppSharp.AST;
using System.Collections.Generic;

namespace MonoManagedToNative.Generators
{
    public class AstGenerator
    {
        public TranslationUnit unit;
        Options options;

        public AstGenerator(Options options)
        {
            unit = new TranslationUnit();
            this.options = options;
        }

        public TranslationUnit Visit(Assembly assembly)
        {
            var name = options.LibraryName ?? assembly.GetName().Name;
            unit.Name = name;

            foreach (var type in assembly.ExportedTypes)
            {
                var decl = Visit(type.GetTypeInfo());
                unit.Declarations.Add(decl);
            }

            return unit;
        }

        public Declaration Visit(TypeInfo typeInfo)
        {
            if (typeInfo.IsClass || typeInfo.IsInterface || typeInfo.IsValueType)
                return VisitRecord(typeInfo);
            else if (typeInfo.IsEnum)
                return VisitEnum(typeInfo);

            throw new Exception("Could not visit type: " + typeInfo.ToString());
        }

        public Class VisitRecord(TypeInfo type)
        {
            var @class = new Class { Name = type.Name };
            VisitMembers(type, @class);

            return @class;
        }

        public Enumeration VisitEnum(TypeInfo @enum)
        {
            return null;
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
                var decl = VisitMethod(method, @class);
                @class.Declarations.Add(decl);
            }

            foreach (var field in type.DeclaredFields)
            {
                var decl = VisitField(field);
                @class.Declarations.Add(decl);
            }

            foreach (var @event in type.DeclaredEvents)
            {
                var decl = VisitEvent(@event);
                @class.Declarations.Add(decl);
            }

            foreach (var property in type.DeclaredProperties)
            {
                var decl = VisitProperty(property);
                @class.Declarations.Add(decl);
            }

            foreach (var decl in @class.Declarations)
                decl.Namespace = @class;
        }

        public Method VisitConstructor(ConstructorInfo ctor, Class @class)
        {
            var method = VisitMethodBase(ctor);
            var ptrType = new QualifiedType(
                new PointerType(new QualifiedType(new TagType(@class))));
            method.ReturnType = ptrType;
            method.Name = "new";
            return method;
        }

        string GetInternalTypeName(IKVM.Reflection.Type type)
        {
            switch(IKVM.Reflection.Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    if (type.FullName == "System.IntPtr")
                        return "intptr";
                    if (type.FullName == "System.UIntPtr")
                        return "uintptr";
                    return "object";
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
                case TypeCode.DBNull:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                    throw new NotSupportedException();
            }

            throw new NotSupportedException();
        }

        string GetInternalMethodName(MethodBase method)
        {
            var @params = new List<string>();
            foreach (var p in method.GetParameters())
            {
                var param = GetInternalTypeName(p.ParameterType);
                if (p.IsOut)
                    param += "&";
                else if (p.ParameterType.IsPointer)
                    param += "*";

                @params.Add(param);
            }

            var sig = method.ToString();
            return string.Format("{0}:{1}({2})", method.DeclaringType.FullName,
                method.Name, string.Join(", ", @params));
        }

        Method VisitMethod(MethodInfo methodInfo, Class @class)
        {
            var method = VisitMethodBase(methodInfo);
            method.ReturnType = VisitType(methodInfo.ReturnType);

            var ptrType = new QualifiedType(
                new PointerType(new QualifiedType(new TagType(@class))));
            var param = new Parameter { Name = "object", Namespace = @class,
                QualifiedType = ptrType };
            method.Parameters.Insert(0, param);

            return method;
        }

        QualifiedType VisitType(IKVM.Reflection.Type managedType)
        {
            // If true type is an array, a pointer, or is passed by reference.
            if (managedType.HasElementType)
            {
                var elementType = managedType.GetElementType();

                if (managedType.IsByRef || managedType.IsPointer)
                {
                    var ptrElementType = VisitType(elementType);
                    var ptrType = new PointerType(ptrElementType)
                    {
                        Modifier = (options.Language == GeneratorKind.CPlusPlus) ?
                            PointerType.TypeModifier.LVReference :
                            PointerType.TypeModifier.Pointer
                    };

                    return new QualifiedType(ptrType);
                }
                else if (managedType.IsArray)
                {
                    throw new NotImplementedException();
                }

                throw new NotImplementedException();
            }

            CppSharp.AST.Type type = null;
            switch (IKVM.Reflection.Type.GetTypeCode(managedType))
            {
            case TypeCode.Empty:
                type = new BuiltinType(PrimitiveType.Null);
                break;
            case TypeCode.Object:
                if (managedType.FullName == "System.Void")
                {
                    type = new BuiltinType(PrimitiveType.Void);
                    break;
                }
                throw new NotSupportedException();
            case TypeCode.DBNull:
                throw new NotSupportedException();
            case TypeCode.Boolean:
                type = new BuiltinType(PrimitiveType.Bool);
                break;
            case TypeCode.Char:
                type = new BuiltinType(PrimitiveType.WideChar);
                break;
            case TypeCode.SByte:
                type = new BuiltinType(PrimitiveType.Char);
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
                type = new BuiltinType(PrimitiveType.LongLong);
                break;
            case TypeCode.UInt64:
                type = new BuiltinType(PrimitiveType.ULongLong);
                break;
            case TypeCode.Single:
                type = new BuiltinType(PrimitiveType.Float);
                break;
            case TypeCode.Double:
                type = new BuiltinType(PrimitiveType.Double);
                break;
            case TypeCode.Decimal:
                type = new CILType(typeof(decimal));
                break;
            case TypeCode.DateTime:
                type = new CILType(typeof(DateTime));
                break;
            case TypeCode.String:
                type = new CILType(typeof(string));
                break;
            }

            return new QualifiedType(type);
        }

        /// <summary>
        /// Converts from a .NET member acccess mask to a C/C++ access specifier.
        /// </summary>
        /// <returns></returns>
        static AccessSpecifier ConvertToAccessSpecifier(MethodAttributes mask)
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
                    CXXMethodKind.Constructor : CXXMethodKind.Normal
            };
            method.Name = methodBase.Name;
            method.OriginalName = GetInternalMethodName(methodBase);

            foreach (var param in methodBase.GetParameters())
            {
                var paramDecl = VisitParameter(param);
                method.Parameters.Add(paramDecl);
            }

            method.IsStatic = methodBase.IsStatic;
            method.IsVirtual = methodBase.IsVirtual;
            method.IsPure = methodBase.IsAbstract;
            //method.IsFinal = methodBase.IsFinal;
            var memberAccessMask = (methodBase.Attributes & MethodAttributes.MemberAccessMask);
            method.Access = ConvertToAccessSpecifier(memberAccessMask);

            return method;
        }

        /// <summary>
        /// Converts from a .NET parameter to parameter usage specifier.
        /// </summary>
        /// <returns></returns>
        static ParameterUsage ConvertToParameterUsage(ParameterInfo param)
        {
            if (param.IsIn && param.IsOut)
                return ParameterUsage.InOut;
            else if (param.IsOut)
                return ParameterUsage.Out;

            return ParameterUsage.In;
        }

        public Parameter VisitParameter(ParameterInfo paramInfo)
        {
            var param = new Parameter()
            {
                Name = paramInfo.Name,
                Usage = ConvertToParameterUsage(paramInfo),
                HasDefaultValue = paramInfo.HasDefaultValue,
                QualifiedType = VisitType(paramInfo.ParameterType)
            };

            return param;
        }

        public Field VisitField(FieldInfo field)
        {
            throw new NotImplementedException();
        }

        public Event VisitEvent(EventInfo @event)
        {
            throw new NotImplementedException();
        }

        public Property VisitProperty(PropertyInfo @property)
        {
            throw new NotImplementedException();
        }
    }
}
