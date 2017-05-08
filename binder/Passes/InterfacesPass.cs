using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;

namespace MonoEmbeddinator4000.Passes
{
    public class InterfacesPass : GetReferencedClasses
    {
        public List<Class> InterfaceImplementations;

        public InterfacesPass()
        {
            InterfaceImplementations = new List<Class>();
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            var ret = base.VisitTranslationUnit(unit);

            var interfaces = Classes.Where(c => c.IsInterface);
            foreach (var @interface in interfaces)
                HandleInterface(@interface);

            unit.Declarations.InsertRange(0, InterfaceImplementations);
            InterfaceImplementations.Clear();

            Classes.Clear();

            return ret;
        }

        void HandleInterface(Class @interface)
        {
            // If we are binding to Java, then we need to generate a class 
            // implementation for each interface that is used when a managed
            // method returns an interface object.
            CreateInterfaceImplementation(@interface);

            // We also add a __getObject() method to the interface that returns
            // the Embeddinator runtime object for the interface to be used when
            // doing marshaling.
            AddObjectGetterToInterface(@interface);
        }

        void CreateInterfaceImplementation(Class @interface)
        {
            var impl = new Class
            {
                Name = @interface.Name + "Impl",
                Namespace = @interface.Namespace,
                Type = ClassType.RefType,
                IsFinal = true,
            };

            var @base = new BaseClassSpecifier { Type = new TagType(@interface) };
            impl.Bases.Add(@base);

            var methods = @interface.Declarations.Where(d => d is Method).Cast<Method>();
            foreach (var method in methods)
            {
                var methodImpl = new Method(method)
                {
                    IsPure = false,
                    IsImplicit = true,
                    IsOverride = true,
                    SynthKind= FunctionSynthKind.InterfaceInstance,
                    ExplicitInterfaceImpl = @interface
                };

                impl.Declarations.Add(methodImpl);
            }

            InterfaceImplementations.Add(impl);
        }

        void AddObjectGetterToInterface(Class @interface)
        {
            var type = new BuiltinType(PrimitiveType.IntPtr);
        
            var method = new Method
            {
                Name = "__getObject",
                Namespace = @interface,
                Access = AccessSpecifier.Public,
                ReturnType = new QualifiedType(type),
                IsImplicit = true,
                IsPure = true,
            };

            @interface.Declarations.Add(method);
        }
    }
}
