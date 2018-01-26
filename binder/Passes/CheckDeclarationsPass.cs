using System;
using System.Collections.Generic;
using System.Linq;
using CppSharp;
using CppSharp.AST;
using CppSharp.Generators;
using CppSharp.Passes;
using CppSharp.AST.Extensions;

namespace Embeddinator.Passes
{
    public class CheckDeclarations : TranslationUnitPass
    {
        static string GetBaseName(Declaration decl)
        {
            // Remove get_ or set_ from names in case they are properties.
            var name = decl.Name.TrimStart("get_").TrimStart("set_");

            // Do the same for method names starting with get or set since they
            // might conflict with properties.
            name = decl.Name.TrimStart("get").TrimStart("set").
                             TrimStart("Get").TrimStart("Set");

            // Remove prefixes for explicit interface methods.
            name = name.Substring(decl.Name.LastIndexOf(".", StringComparison.Ordinal) + 1);

            return name;
        }

        HashSet<Declaration> processed = new HashSet<Declaration>();

        void CheckMemberDeclaration(Declaration decl)
        {
            var @class = decl.Namespace as Class;

            if (Options.GeneratorKind == GeneratorKind.Java)
            {
                var property = decl as Property;
                if (property != null)
                {
                    if (property.Name == "Class")
                        RenameForbidden(property);
                }

                var method = decl as Method;
                if (method != null)
                {
                    if (method.Name == "getClass" || method.Name == "GetClass")
                        RenameForbidden(method);

                    if ((method.Name == "toString" || method.Name == "ToString") &&
                        (method.Parameters.Count() != 0 ||
                        !method.ReturnType.Type.IsPrimitiveType(PrimitiveType.String)))
                       RenameForbidden(method);
                }
            }
        }

        private void RenameForbidden(Declaration decl)
        {
            int i = 0;

            decl.DefinitionOrder = (uint)++i;

            if (decl.AssociatedDeclaration != null)
                decl.AssociatedDeclaration.DefinitionOrder = decl.DefinitionOrder;

            Diagnostics.Debug("Found forbidden {0} name: {1}", decl.GetType().Name.ToLowerInvariant(),
                decl.QualifiedName);
        }

        public override bool VisitClassDecl(Class @class)
        {
            if (!VisitDeclaration(@class))
                return false;

            // Make sure base classes are visited first.
            foreach (var @base in @class.Bases)
                @base.Class.Visit(this);

            var members = @class.Declarations.Where(d => d is Field || d is Function || d is Property)
                .ToList();

            processed.Clear();

            foreach (var member in members)
                CheckMemberDeclaration(member);

            foreach (var member in members.Where(decl => decl.IsGenerated))
            {
                processed.Add(member);

                var duplicates = members.FindAll(decl => GetBaseName(member) == GetBaseName(decl))
                    .ToList();
                duplicates.RemoveAll(decl => !decl.IsGenerated);

                if (duplicates.Count == 0)
                    continue;

                if (Options.GeneratorKind == GeneratorKind.C)
                    HandleDuplicatesC(duplicates);
                else
                    HandleDuplicatesJava(duplicates);
            }

            return true;
        }

        public void HandleDuplicatesC(List<Declaration> duplicates)
        {
            duplicates.RemoveAll(decl => processed.Contains(decl));
            RenameDuplicates(duplicates);
        }

        private void RenameDuplicates(IEnumerable<Declaration> duplicates)
        {
            int i = 0;

            foreach (var duplicate in duplicates)
            {
                processed.Add(duplicate);

                duplicate.DefinitionOrder = (uint)++i;

                if (duplicate.AssociatedDeclaration != null)
                    duplicate.AssociatedDeclaration.DefinitionOrder = duplicate.DefinitionOrder;

                Diagnostics.Debug("Found duplicate {0}: {1}", duplicate.GetType().Name.ToLowerInvariant(),
                    duplicate.QualifiedName);
            }
        }

        static bool IsStaticDecl(Declaration decl)
        {
            var method = decl as Method;
            if (method != null && method.IsStatic)
                return true;

            var property = decl as Property;
            if (property != null && property.IsStatic)
                return true;

            return false;
        }

        static bool IsInstanceDecl(Declaration decl)
        {
            return !IsStaticDecl(decl);
        }

        public void HandleDuplicatesJava(List<Declaration> duplicates)
        {
            // Java does not allow static and instance overloads with the same name.
            // If this is the case, we rename the static overloads to have a suffix since
            // the instance overloads might be part of an interface and need to have their
            // original names to implement the interfaces.

            if (duplicates.Any(IsStaticDecl) && duplicates.Any(IsInstanceDecl))
            {
                var staticDecls = duplicates.Where(IsStaticDecl);
                RenameDuplicates(staticDecls);
                duplicates = duplicates.Except(staticDecls).ToList();
            }

            duplicates.RemoveAll(decl => decl.DefinitionOrder != 0);

            if (duplicates.Count() == 0)
                return;

            // If all members that remain are methods, then we're done.
            if (duplicates.All(d => d is Method))
                return;

            if (duplicates.Count() > 1)
                RenameDuplicates(duplicates);
        }

        public override bool VisitParameterDecl(Parameter param)
        {
            return true;
        }
    }
}
