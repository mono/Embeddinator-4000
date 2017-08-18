using System.Collections.Generic;
using System.Linq;
using CppSharp.AST;

namespace Embeddinator.Passes
{
    public class InterfacesPass : GetReferencedDecls
    {
        public List<Class> InterfaceImplementations;

        public InterfacesPass()
        {
            InterfaceImplementations = new List<Class>();
            ClearVisitedDeclarations = false;
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            var ret = base.VisitTranslationUnit(unit);

            var interfaces = Classes.Where(c => c.IsInterface && c.IsGenerated);
            foreach (var @interface in interfaces)
                    HandleInterface(@interface);

            unit.Declarations.InsertRange(0, InterfaceImplementations);
            InterfaceImplementations.Clear();

            Declarations.Clear();

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

            var methods = new List<Method>(@interface.Declarations.OfType<Method>());
            foreach (var baseInterface in @interface.Bases)
            {
                methods.AddRange(baseInterface.Class.Declarations.OfType<Method>());
            }

            foreach (var method in methods)
            {
                //NOTE: skip methods such as __getObject
                if (method.IsImplicit)
                    continue;

                var methodImpl = new Method(method)
                {
                    IsPure = false,
                    IsImplicit = true,
                    IsOverride = true,
                    SynthKind = FunctionSynthKind.InterfaceInstance,
                    ExplicitInterfaceImpl = @interface,
                    Namespace = impl,
                    CompleteDeclaration = method
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
