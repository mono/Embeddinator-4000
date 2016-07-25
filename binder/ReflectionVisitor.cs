using IKVM.Reflection;
using System;

namespace MonoManagedToNative.Generators
{
    public class ReflectionVisitor<T>
    {
        public virtual T Visit(Assembly assembly)
        {
            foreach (var type in assembly.ExportedTypes)
            {
                Visit(type.GetTypeInfo());
            }

            return default(T);
        }

        public T Visit(TypeInfo typeInfo)
        {
            if (typeInfo.IsClass)
                return VisitClass(typeInfo);
            else if (typeInfo.IsEnum)
                return VisitEnum(typeInfo);
            else if (typeInfo.IsInterface)
                return VisitInterface(typeInfo);
            else if (typeInfo.IsValueType)
                return VisitStruct(typeInfo);

            throw new Exception("Could not visit type " + typeInfo.ToString());
        }

        public virtual void VisitMembers(TypeInfo type)
        {
            foreach (var ctor in type.DeclaredConstructors)
                VisitConstructor(ctor);

            foreach (var method in type.DeclaredMethods)
                VisitMethod(method);

            foreach (var field in type.DeclaredFields)
                VisitField(field);

            foreach (var @event in type.DeclaredEvents)
                VisitEvent(@event);

            foreach (var property in type.DeclaredProperties)
                VisitProperty(property);
        }

        public virtual T VisitClass(TypeInfo @class)
        {
            return default(T);
        }

        public virtual T VisitEnum(TypeInfo @enum)
        {
            return default(T);
        }

        public virtual T VisitInterface(TypeInfo @interface)
        {
            return default(T);
        }

        public virtual T VisitStruct(TypeInfo @struct)
        {
            return default(T);
        }

        public virtual T VisitConstructor(ConstructorInfo ctor)
        {
            return default(T);
        }

        public virtual T VisitMethod(MethodInfo method)
        {
            return default(T);
        }

        public virtual T VisitField(FieldInfo field)
        {
            return default(T);
        }

        public virtual T VisitEvent(EventInfo @event)
        {
            return default(T);
        }

        public virtual T VisitProperty(PropertyInfo @property)
        {
            return default(T);
        }
    }
}
