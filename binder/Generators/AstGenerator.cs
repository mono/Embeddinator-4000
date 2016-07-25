using System;
using IKVM.Reflection;
using CppSharp.AST;

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

        string GetInternalMethodName(MethodBase method)
        {
            var @params = string.Empty;
            var sig = method.ToString();
            return string.Format("{0}:{1}({2})", method.DeclaringType.FullName,
                method.Name, @params);
        }

        Method VisitMethod(MethodInfo methodInfo, Class @class)
        {
            var method = VisitMethodBase(methodInfo);
            method.ReturnType = VisitType(methodInfo.ReturnType);

            var ptrType = new QualifiedType(
                new PointerType(new QualifiedType(new TagType(@class))));
            var param = new Parameter { Name = "object", Namespace = @class,
                QualifiedType = ptrType };
            method.Parameters.Add(param);

            return method;
        }

        QualifiedType VisitType(IKVM.Reflection.Type managedType)
        {
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
        AccessSpecifier ConvertToAccessSpecifier(MethodAttributes mask)
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

        public Parameter VisitParameter(ParameterInfo param)
        {
            throw new NotImplementedException();
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
