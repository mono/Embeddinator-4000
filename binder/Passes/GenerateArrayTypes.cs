using CppSharp.AST;
using CppSharp.Passes;
using MonoManagedToNative.Generators;
using System.Collections.Generic;

namespace MonoManagedToNative.Passes
{
    /// <summary>
    /// This pass is responsible for gathering a unique set of array types,
    /// and generating a struct to handle returning and passing them as return
    /// or parameter types from methods and properties.
    /// </summary>
    public class GenerateArrayTypes : TranslationUnitPass
    {
        public Dictionary<string, QualifiedType> Arrays { get; private set; }
        public List<PendingDeclaration> PendingDeclarations { get; private set; }

        public GenerateArrayTypes()
        {
            Arrays = new Dictionary<string, QualifiedType>();
            PendingDeclarations = new List<PendingDeclaration>();
        }

        static CArrayTypePrinter ArrayPrinter
        {
            get
            {
                return new CArrayTypePrinter
                {
                    PrintScopeKind = CppTypePrintScopeKind.Qualified,
                };
            }
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            var ret = base.VisitTranslationUnit(unit);

            foreach (var pending in PendingDeclarations)
            {
                var @namespace = pending.ReferenceDeclaration.Namespace;
                var index = @namespace.Declarations.IndexOf(pending.ReferenceDeclaration);
                @namespace.Declarations.Insert(index, pending.NewDeclaration);
            }

            PendingDeclarations.Clear();

            return ret;
        }

        QualifiedType GenerateArrayType(ArrayType array, Declaration decl)
        {
            var typeName = array.Visit(ArrayPrinter);
            var monoArrayType = new Class { Name = "MonoEmbedArray" };

            var @namespace = decl.Namespace;

            var typedef = new TypedefDecl
            {
                Name = string.Format("_{0}", typeName),
                QualifiedType = new QualifiedType { Type = new TagType(monoArrayType) }
            };

            var pending = new PendingDeclaration
            {
                NewDeclaration = typedef,
                ReferenceDeclaration = decl
            };
            PendingDeclarations.Add(pending);

            var typedefType = new TypedefType(typedef);
            var arrayType = new ManagedArrayType(array, typedefType);

            return new QualifiedType { Type = arrayType };
        }

        public bool CheckArrayType(ArrayType array, Declaration decl,
            out QualifiedType type)
        {
            var typeName = array.Type.Visit(ArrayPrinter);

            // Search if we already have a signature compatible array struct.
            if (Arrays.ContainsKey(typeName))
            {
                type = Arrays[typeName];
                return true;
            }

            type = GenerateArrayType(array, decl);
            Arrays[typeName] = type;

            return true;
        }

        public bool CheckType(Type type, Declaration decl, out QualifiedType newType)
        {
            newType = new QualifiedType();

            var arrayType = type as ArrayType;
            if (arrayType == null)
                return false;

            if (!CheckArrayType(arrayType, decl, out newType))
                return false;

            return true;
        }

        public override bool VisitFunctionDecl(Function function)
        {
            QualifiedType newType;

            var retType = function.ReturnType;
            if (CheckType(retType.Type, function, out newType))
                function.ReturnType = newType;

            foreach (var param in function.Parameters)
            {
                if (CheckType(param.Type, function, out newType))
                    param.QualifiedType = newType;
            }

            return true;
        }
    }
}
