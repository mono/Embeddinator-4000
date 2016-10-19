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
        TranslationUnit TranslationUnit;

        Dictionary<string, QualifiedType> Arrays;
        List<TypedefDecl> Declarations;

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

        public GenerateArrayTypes()
        {
            Arrays = new Dictionary<string, QualifiedType>();
            Declarations = new List<TypedefDecl>();
        }

        public override bool VisitTranslationUnit(TranslationUnit unit)
        {
            TranslationUnit = unit;

            var ret = base.VisitTranslationUnit(unit);

            unit.Declarations.InsertRange(0, Declarations);

            Declarations.Clear();
            Arrays.Clear();
            TranslationUnit = null;

            return ret;
        }

        QualifiedType GenerateArrayType(ArrayType array, Declaration decl)
        {
            var typeName = array.Visit(ArrayPrinter);
            var monoArrayType = new Class { Name = "MonoEmbedArray" };

            var @namespace = TranslationUnit;

            var typedef = new TypedefDecl
            {
                Name = string.Format("_{0}", typeName),
                Namespace = @namespace,
                QualifiedType = new QualifiedType(new TagType(monoArrayType))
            };

            Declarations.Add(typedef);

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
