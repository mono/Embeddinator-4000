#define CATCH_CONFIG_RUNNER
#include <catch.hpp>
#include <cstdint>
#include "managed.h"
#include "fsharpManaged.h"
#include "glib.h"
#include "c-support.h"

#include "float.h"

TEST_CASE("Types.C", "[C][Types]") {
    REQUIRE(Type_Char_get_Min() == 0);
    REQUIRE(Type_Char_get_Max() == UINT16_MAX);
    REQUIRE(Type_Char_get_Zero() == 0);

    REQUIRE(Type_SByte_get_Min() == INT8_MIN);
    REQUIRE(Type_SByte_get_Max() == INT8_MAX);

    REQUIRE(Type_Byte_get_Min() == 0);
    REQUIRE(Type_Byte_get_Max() == UINT8_MAX);

    REQUIRE(Type_Int16_get_Min() == INT16_MIN);
    REQUIRE(Type_Int16_get_Max() == INT16_MAX);

    REQUIRE(Type_Int32_get_Min() == INT32_MIN);
    REQUIRE(Type_Int32_get_Max() == INT32_MAX);

    REQUIRE(Type_Int64_get_Min() == INT64_MIN);
    REQUIRE(Type_Int64_get_Max() == INT64_MAX);
    
    REQUIRE(Type_UInt16_get_Min() == 0);
    REQUIRE(Type_UInt16_get_Max() == UINT16_MAX);

    REQUIRE(Type_UInt32_get_Min() == 0);
    REQUIRE(Type_UInt32_get_Max() == UINT32_MAX);

    REQUIRE(Type_UInt64_get_Min() == 0);
    REQUIRE(Type_UInt64_get_Max() == UINT64_MAX);

    REQUIRE(Type_Single_get_Min() == -FLT_MAX);
    REQUIRE(Type_Single_get_Max() == FLT_MAX);

    REQUIRE(Type_Double_get_Min() == -DBL_MAX);
    REQUIRE(Type_Double_get_Max() == DBL_MAX);

    REQUIRE(Type_String_get_NullString() == NULL);
    REQUIRE(strcmp(Type_String_get_EmptyString(), "") == 0);
    REQUIRE(strcmp(Type_String_get_NonEmptyString(), "Hello World") == 0);

    GString* result;

    MonoDecimal decimalmax = Type_Decimal_get_Max();
    result = mono_embeddinator_decimal_to_gstring(decimalmax);
    REQUIRE(strcmp(result->str, "79228162514264337593543950335") == 0);

    MonoDecimal decimalmin = Type_Decimal_get_Min();
    result = mono_embeddinator_decimal_to_gstring(decimalmin);
    REQUIRE(strcmp(result->str, "-79228162514264337593543950335") == 0);

    MonoDecimal decimalzero = Type_Decimal_get_Zero();
    result = mono_embeddinator_decimal_to_gstring(decimalzero);
    REQUIRE(strcmp(result->str, "0") == 0);

    MonoDecimal decimalone = Type_Decimal_get_One();
    result = mono_embeddinator_decimal_to_gstring(decimalone);
    REQUIRE(strcmp(result->str, "1") == 0);

    MonoDecimal decimalminusone = Type_Decimal_get_MinusOne();
    result = mono_embeddinator_decimal_to_gstring(decimalminusone);
    REQUIRE(strcmp(result->str, "-1") == 0);

    MonoDecimal decimalpi = Type_Decimal_get_Pi();
    result = mono_embeddinator_decimal_to_gstring(decimalpi);
    REQUIRE(strcmp(result->str, "3.14159265358979323846264") == 0);

    MonoDecimal decimalminustau = Type_Decimal_get_MinusTau();
    result = mono_embeddinator_decimal_to_gstring(decimalminustau);
    REQUIRE(strcmp(result->str, "-6.28318530717958647692") == 0);

    MonoDecimal decimalfortytwo = Type_Decimal_get_FortyTwo();
    result = mono_embeddinator_decimal_to_gstring(decimalfortytwo);
    REQUIRE(strcmp(result->str, "42") == 0);

    BuiltinTypes* bt = BuiltinTypes_new();
    BuiltinTypes_ReturnsVoid(bt);
    REQUIRE(BuiltinTypes_ReturnsBool(bt)   == true);
    REQUIRE(BuiltinTypes_ReturnsSByte(bt)  == -5);
    REQUIRE(BuiltinTypes_ReturnsByte(bt)   ==  5);
    REQUIRE(BuiltinTypes_ReturnsShort(bt)  == -5);
    REQUIRE(BuiltinTypes_ReturnsUShort(bt) ==  5);
    REQUIRE(BuiltinTypes_ReturnsInt(bt)    == -5);
    REQUIRE(BuiltinTypes_ReturnsUInt(bt)   ==  5);
    REQUIRE(BuiltinTypes_ReturnsLong(bt)   == -5);
    REQUIRE(BuiltinTypes_ReturnsULong(bt)  ==  5);
    REQUIRE(BuiltinTypes_ReturnsChar(bt) == 'a');
    REQUIRE(strcmp(BuiltinTypes_ReturnsString(bt), "Mono") == 0);

    REQUIRE(BuiltinTypes_PassAndReturnsBool(bt, true) == true);
    REQUIRE(BuiltinTypes_PassAndReturnsSByte(bt, -5) == -5);
    REQUIRE(BuiltinTypes_PassAndReturnsByte(bt, 5) == 5);
    REQUIRE(BuiltinTypes_PassAndReturnsShort(bt, -5) == -5);
    REQUIRE(BuiltinTypes_PassAndReturnsUShort(bt, 5) == 5);
    REQUIRE(BuiltinTypes_PassAndReturnsInt(bt, -5) == -5);
    REQUIRE(BuiltinTypes_PassAndReturnsUInt(bt, 5) == 5);
    REQUIRE(BuiltinTypes_PassAndReturnsLong(bt, -5) == -5);
    REQUIRE(BuiltinTypes_PassAndReturnsULong(bt, 5) == 5);
    REQUIRE(BuiltinTypes_PassAndReturnsChar(bt, 'a') == 'a');
    REQUIRE(strcmp(BuiltinTypes_PassAndReturnsString(bt, "Mono"), "Mono") == 0);

    int OutInt = 0;
    BuiltinTypes_PassOutInt(bt, &OutInt);
    REQUIRE(OutInt == 5);

    int RefInt = 0;
    BuiltinTypes_PassRefInt(bt, &RefInt);
    REQUIRE(RefInt == 10);

    GString* RefOut = g_string_new("");
    BuiltinTypes_PassOutString(bt, RefOut);
    REQUIRE(strcmp(RefOut->str, "Mono") == 0);

    GString* RefStr = g_string_new("monomono");
    BuiltinTypes_PassRefString(bt, RefStr);
    REQUIRE(strcmp(RefStr->str, "Mono") == 0);
}

TEST_CASE("Properties.C", "[C][Properties]") {

    Platform_set_ExitCode(255);
    REQUIRE(Platform_get_ExitCode() == 255);

    REQUIRE(Properties_Query_get_UniversalAnswer() == 42);

    Properties_Query* prop = Properties_Query_new();
    REQUIRE(Properties_Query_get_IsGood(prop) == true);
    REQUIRE(Properties_Query_get_IsBad(prop) == false);
    REQUIRE(Properties_Query_get_Answer(prop) == 42);
    Properties_Query_set_Answer(prop, 911);
    REQUIRE(Properties_Query_get_Answer(prop) == 911);

    REQUIRE(Properties_Query_get_IsSecret(prop) == false);
    Properties_Query_set_Secret(prop, 1);
    REQUIRE(Properties_Query_get_IsSecret(prop) == true);
}

TEST_CASE("Namespaces.C", "[C][Namespaces]") {
    ClassWithoutNamespace* nonamespace = ClassWithoutNamespace_new();
    REQUIRE(strcmp(ClassWithoutNamespace_ToString(nonamespace), "ClassWithoutNamespace") == 0);

    First_ClassWithSingleNamespace* singlenamespace = First_ClassWithSingleNamespace_new();
    REQUIRE(strcmp(First_ClassWithSingleNamespace_ToString(singlenamespace), "First.ClassWithSingleNamespace") == 0);

    First_Second_ClassWithNestedNamespace* nestednamespaces = First_Second_ClassWithNestedNamespace_new();
    REQUIRE(strcmp(First_Second_ClassWithNestedNamespace_ToString(nestednamespaces), "First.Second.ClassWithNestedNamespace") == 0);

    First_Second_Third_ClassWithNestedNamespace* nestednamespaces2 = First_Second_Third_ClassWithNestedNamespace_new();
    REQUIRE(strcmp(First_Second_Third_ClassWithNestedNamespace_ToString(nestednamespaces2), "First.Second.Third.ClassWithNestedNamespace") == 0);
}

TEST_CASE("Exceptions.C", "[C][Exceptions]") {
    // .ctor that throws
    Exceptions_Throwers* throwers = Exceptions_Throwers_new();
    REQUIRE(throwers == 0);

    // .cctor that throw - can't be called directly but it makes the type unusable
    Exceptions_ThrowInStaticCtor* static_thrower = Exceptions_ThrowInStaticCtor_new();
    REQUIRE(static_thrower == 0);

    // .ctor chaining
    Exceptions_Super* sup1 = Exceptions_Super_new(false);
    REQUIRE(sup1 != 0);

    Exceptions_Super* sup2 = Exceptions_Super_new(true);
    REQUIRE(static_thrower == 0);
}

TEST_CASE("Constructors.C", "[C][Constructors]") {
    Constructors_Unique* unique = Constructors_Unique_new();
    REQUIRE(Constructors_Unique_get_Id(unique) == 1);

    Constructors_Unique* unique_init_id = Constructors_Unique_new_1(911);
    REQUIRE(Constructors_Unique_get_Id(unique_init_id) == 911);

    Constructors_SuperUnique* super_unique_default_init = Constructors_SuperUnique_new();
    REQUIRE(Constructors_Unique_get_Id(super_unique_default_init) == 411);

    Constructors_Implicit* implicit = Constructors_Implicit_new();
    REQUIRE(strcmp(Constructors_Implicit_get_TestResult(implicit), "OK") == 0);

    Constructors_AllTypeCode* all1 = Constructors_AllTypeCode_new(true, UINT16_MAX, "Mono");
    REQUIRE(Constructors_AllTypeCode_get_TestResult(all1) == true);

    Constructors_AllTypeCode* all2 = Constructors_AllTypeCode_new_1(INT8_MAX, INT16_MAX, INT32_MAX, INT64_MAX);
    REQUIRE(Constructors_AllTypeCode_get_TestResult(all2) == true);

    Constructors_AllTypeCode* all3 = Constructors_AllTypeCode_new_2(UINT8_MAX , UINT16_MAX, UINT32_MAX, UINT64_MAX);
    REQUIRE(Constructors_AllTypeCode_get_TestResult(all3) == true);

    Constructors_AllTypeCode* all4 = Constructors_AllTypeCode_new_3(FLT_MAX, DBL_MAX);
    REQUIRE(Constructors_AllTypeCode_get_TestResult(all4) == true);
}

TEST_CASE("Methods.C", "[C][Methods]") {
    Methods_Static* static_method = Methods_Static_Create(1);
    REQUIRE(Methods_Static_get_Id(static_method) == 1);

    REQUIRE(Methods_Parameters_Concat(NULL, NULL) == NULL);
    REQUIRE(strcmp(Methods_Parameters_Concat("first", NULL), "first") == 0);
    REQUIRE(strcmp(Methods_Parameters_Concat(NULL, "second"), "second") == 0);
    REQUIRE(strcmp(Methods_Parameters_Concat("first", "second"), "firstsecond") == 0);

    bool b = true;
    GString* s = g_string_new(NULL);
    Methods_Parameters_Ref(&b, s);
    REQUIRE(b == false);
    REQUIRE(strcmp(s->str, "hello") == 0);

    Methods_Parameters_Ref(&b, s);
    REQUIRE(b == true);
    REQUIRE(s->str == 0);

    int l;
    Methods_Parameters_Out(NULL, &l, s);
    REQUIRE(l == 0);
    REQUIRE(s->str == 0);

    Methods_Parameters_Out("Xamarin", &l, s);
    REQUIRE(l == 7);
    REQUIRE(strcmp(s->str, "XAMARIN") == 0);

    Methods_Static* ref_static = static_method;
    int refClassId = Methods_Parameters_RefClass(&ref_static);
    REQUIRE(refClassId == Methods_Static_get_Id(ref_static));

    Methods_Parameters_RefClassAssignPlus(&ref_static, 2);
    REQUIRE(Methods_Static_get_Id(ref_static) == 3);

    Methods_Parameters_RefClassRetNull(&ref_static);
    REQUIRE(ref_static == NULL);

    Methods_Static* null_static = 0;
    bool isNull = Methods_Parameters_RefClassPassNull(&null_static);
    REQUIRE(isNull == true);

    Methods_Static* out_static = 0;
    Methods_Parameters_OutClass(&out_static, 1);
    REQUIRE(Methods_Static_get_Id(out_static) == 1);

    Methods_Item* item = Methods_Factory_CreateItem(1);
    REQUIRE(Methods_Item_get_Integer(item) == 1);

    Methods_Collection* collection = Methods_Collection_new();
    REQUIRE(Methods_Collection_get_Count(collection) == 0);

    Methods_Collection_Add(collection, item);
    REQUIRE(Methods_Collection_get_Count(collection) == 1);

    int int0 = Methods_Item_get_Integer(Methods_Collection_get_Item(collection, 0));
    REQUIRE(int0 == Methods_Item_get_Integer(item));

    Methods_Item* item2 = Methods_Factory_CreateItem(2);
    Methods_Collection_set_Item(collection, 0, item2);
    REQUIRE(Methods_Collection_get_Count(collection) == 1);

    int0 = Methods_Item_get_Integer(Methods_Collection_get_Item(collection, 0));
    REQUIRE(int0 == Methods_Item_get_Integer(item2));

    Methods_Collection_Remove(collection, item);
    REQUIRE(Methods_Collection_get_Count(collection) == 1);

    Methods_Collection_Remove(collection, item2);
    REQUIRE(Methods_Collection_get_Count(collection) == 0);
}

TEST_CASE("Structs.C", "[C][Structs]") {
    Structs_Point* p1 = Structs_Point_new(1.0f, -1.0f);
    REQUIRE(Structs_Point_get_X(p1) == 1.0f);
    REQUIRE(Structs_Point_get_Y(p1) == -1.0f);

    Structs_Point* p2 = Structs_Point_new(2.0f, -2.0f);
    REQUIRE(Structs_Point_get_X(p2) == 2.0f);
    REQUIRE(Structs_Point_get_Y(p2) == -2.0f);

    REQUIRE(Structs_Point_op_Equality(p1, p1) == true);
    REQUIRE(Structs_Point_op_Equality(p2, p2) == true);
    REQUIRE(Structs_Point_op_Inequality(p1, p2) == true);

    Structs_Point* p3 = Structs_Point_op_Addition(p1, p2);
    REQUIRE(Structs_Point_get_X(p3) == 3.0f);
    REQUIRE(Structs_Point_get_Y(p3) == -3.0f);

    Structs_Point* p4 = Structs_Point_op_Subtraction(p3, p2);
    REQUIRE(Structs_Point_op_Equality(p4, p1) == true);

    Structs_Point* z = Structs_Point_get_Zero();
    REQUIRE(Structs_Point_get_X(z) == 0.0f);
    REQUIRE(Structs_Point_get_Y(z) == 0.0f); 
}

TEST_CASE("Enums.C", "[C][Enums]") {
    Enums_IntEnum i = Enums_IntEnum_Min;
    Enums_ShortEnum s;
    Enums_ByteFlags f = Enums_Enumer_Test(Enums_ByteEnum_Max, &i, &s);

    REQUIRE(f == 0x22);
    REQUIRE(i == Enums_IntEnum_Max);
    REQUIRE(s == Enums_ShortEnum_Max);

    f = Enums_Enumer_Test(Enums_ByteEnum_Zero, &i, &s);
    REQUIRE(i == Enums_IntEnum_Min);
    REQUIRE(s == Enums_ShortEnum_Min);
}

TEST_CASE("FieldsInReference.C", "[C][Fields]") {
    REQUIRE(Fields_Class_get_MaxLong() == INT64_MAX);

    REQUIRE(Fields_Class_get_Integer() == 0);
    Fields_Class_set_Integer(1);
    REQUIRE(Fields_Class_get_Integer() == 1);

    Fields_Class* scratch = Fields_Class_get_Scratch();
    REQUIRE(Fields_Class_get_Boolean(scratch) == true);

    scratch = Fields_Class_new(/*enabled=*/false);
    Fields_Class_set_Scratch(scratch);
    REQUIRE(Fields_Class_get_Boolean(scratch) == false);

    Fields_Class* ref1 = Fields_Class_new(/*enabled=*/true);
    REQUIRE(Fields_Class_get_Boolean(ref1) == true);
    Fields_Class_set_Boolean(ref1, false);
    REQUIRE(Fields_Class_get_Boolean(ref1) == false);

    Fields_Struct* struct1 = Fields_Class_get_Structure(ref1);
    REQUIRE(struct1 != NULL);
    REQUIRE(Fields_Struct_get_Boolean(struct1) == false);
    struct1 = Fields_Struct_new(/*enabled=*/true);
    REQUIRE(Fields_Struct_get_Boolean(struct1) == true);

    Fields_Class* ref2 = Fields_Class_new(/*enabled=*/false);
    Fields_Struct* struct2 = Fields_Class_get_Structure(ref2);
    REQUIRE(struct2 != NULL);
    REQUIRE(Fields_Class_get_Boolean(ref2) == false);
}

TEST_CASE("FieldsInValueType.C", "[C][Fields]") {
    REQUIRE(Fields_Struct_get_Integer() == 0);
    Fields_Struct_set_Integer(1);
    REQUIRE(Fields_Struct_get_Integer() == 1);

    Fields_Struct* scratch = Fields_Struct_get_Scratch();
    REQUIRE(Fields_Struct_get_Boolean(scratch) == false);

    scratch = Fields_Struct_new(/*enabled=*/true);
    Fields_Struct_set_Scratch(scratch);
    REQUIRE(Fields_Struct_get_Boolean(scratch) == true);

    Fields_Struct* empty = Fields_Struct_get_Empty();
    REQUIRE(empty != NULL);
    REQUIRE(Fields_Struct_get_Class(empty) == NULL);

    Fields_Struct* struct1 = Fields_Struct_new(/*enabled=*/true);
    REQUIRE(Fields_Struct_get_Boolean(struct1) == true);
    Fields_Struct_set_Boolean(struct1, false);
    REQUIRE(Fields_Struct_get_Boolean(struct1) == false);

    Fields_Class* struct1_class = Fields_Struct_get_Class(struct1);
    REQUIRE(struct1_class != NULL);
    REQUIRE(Fields_Class_get_Boolean(struct1_class) == false);
    Fields_Struct_set_Class(struct1, NULL);
    REQUIRE(Fields_Struct_get_Class(struct1) == NULL);
    struct1_class = Fields_Class_new(/*enabled=*/true);
    REQUIRE(Fields_Class_get_Boolean(struct1_class) == true);

    Fields_Struct* struct2 = Fields_Struct_new(/*enabled=*/false);
    Fields_Class* struct2_class = Fields_Struct_get_Class(struct2);
    REQUIRE(struct2_class != NULL);
    REQUIRE(Fields_Class_get_Boolean(struct2_class) == false);
}

TEST_CASE("Interfaces.C", "[C][Interfaces]") {
    Interfaces_IMakeItUp* m = Interfaces_Supplier_Create();
    REQUIRE(Interfaces_IMakeItUp_get_Boolean(m) == true);
    REQUIRE(Interfaces_IMakeItUp_get_Boolean(m) == false);

    REQUIRE(strcmp(Interfaces_IMakeItUp_Convert(m, 0), "0") == 0);
    REQUIRE(strcmp(Interfaces_IMakeItUp_Convert_1(m, 1), "1") == 0);

    Interfaces_ManagedAdder *adder = Interfaces_ManagedAdder_new();
    REQUIRE(Interfaces_OpConsumer_DoAddition(adder, 40, 2) == 42);
    REQUIRE(Interfaces_OpConsumer_TestManagedAdder(1, -1) == true);
}

TEST_CASE("Arrays.C", "[C][Arrays]") {
    char _byte_arr[] = { 1, 2, 3 };
    _ByteArray _byte;
    _byte.array = g_array_sized_new(/*zero_terminated=*/false,
        /*clear=*/true, sizeof(char), G_N_ELEMENTS(_byte_arr));
    g_array_append_vals (_byte.array, _byte_arr, G_N_ELEMENTS(_byte_arr));

    int _sum = Arrays_Arr_SumByteArray(_byte);
    REQUIRE(_sum == 6);

    _Int32Array _int = Arrays_Arr_ReturnsIntArray();
    REQUIRE(_int.array->len == 3);
    REQUIRE(g_array_index(_int.array, int, 0) == 1);
    REQUIRE(g_array_index(_int.array, int, 1) == 2);
    REQUIRE(g_array_index(_int.array, int, 2) == 3);

    _StringArray _string = Arrays_Arr_ReturnsStringArray();
    REQUIRE(_string.array->len == 3);
    REQUIRE(strcmp(g_array_index(_string.array, gchar*, 0), "1") == 0);
    REQUIRE(strcmp(g_array_index(_string.array, gchar*, 1), "2") == 0);
    REQUIRE(strcmp(g_array_index(_string.array, gchar*, 2), "3") == 0);

    Arrays_Enum _enum_arr[] = { Arrays_Enum_A, Arrays_Enum_B, Arrays_Enum_C };
    _Arrays_EnumArray _enum;
    _enum.array = g_array_sized_new(/*zero_terminated=*/false,
        /*clear=*/true, sizeof(Arrays_Enum), G_N_ELEMENTS(_enum_arr));
    g_array_append_vals (_enum.array, _enum_arr, G_N_ELEMENTS(_enum_arr));

    Arrays_Enum _last = Arrays_Arr_EnumArrayLast(_enum);
    REQUIRE(_last == Arrays_Enum_C);
    
    Arrays_Arr* arr = Arrays_Arr_new();
    _Arrays_ValueHolderArray valueHoldersArray = Arrays_Arr_get_ValueHolderArr(arr);
    REQUIRE(valueHoldersArray.array->len == 3);
    for (uint32_t i = 0; i < valueHoldersArray.array->len; i++) {
        Arrays_ValueHolder* value = g_array_index(valueHoldersArray.array, Arrays_ValueHolder*, i);
        REQUIRE(Arrays_ValueHolder_get_IntValue(value) == (i + 1));
    }
    _Arrays_ValueHolderArray resultArray = Arrays_Arr_ValueHolderArrMethod_1(arr, valueHoldersArray);
    REQUIRE(resultArray.array->len == 3);
    for (uint32_t i = 0; i < resultArray.array->len; i++) {
        Arrays_ValueHolder* value = g_array_index(resultArray.array, Arrays_ValueHolder*, i);
        REQUIRE(Arrays_ValueHolder_get_IntValue(value) == (i + 1));
    }
    _Arrays_ValueTypeArray valueTypeArray = Arrays_Arr_ValueTypeArrMethod(arr);
    REQUIRE(valueTypeArray.array->len == 3);
    for (uint32_t i = 0; i < valueTypeArray.array->len; i++) {
        Arrays_ValueType* value = g_array_index(valueTypeArray.array, Arrays_ValueType*, i);
        REQUIRE(Arrays_ValueType_get_IntValue(value) == (i + 1));
    }
    _Arrays_ValueTypeArray resultValueTypeArray = Arrays_Arr_ValueTypeArrMethod_1(arr, valueTypeArray);
    REQUIRE(resultValueTypeArray.array->len == 3);
    for (uint32_t i = 0; i < resultValueTypeArray.array->len; i++) {
        Arrays_ValueType* value = g_array_index(resultValueTypeArray.array, Arrays_ValueType*, i);
        REQUIRE(Arrays_ValueType_get_IntValue(value) == (i + 1));
    }
}

TEST_CASE("FSharpTypes.C", "[C][FSharp Types]") {
    managed_UserRecord* userRecord = managed_UserRecord_new("Test");
    REQUIRE(strcmp(managed_UserRecord_get_UserDescription(userRecord), "Test") == 0);
    managed_UserRecord* defaultUserRecord = managed_FSharp_getDefaultUserRecord();
    REQUIRE(strcmp(managed_UserRecord_get_UserDescription(defaultUserRecord), "Cherry") == 0);
    _Managed_UserRecordArray userRecordArray = managed_ArrayTest_getDefaultUserRecordArray(10);
    uint32_t length = userRecordArray.array->len;
    REQUIRE(length == 10);
    for(uint32_t i = 0; i < length; i++){
        managed_UserRecord* entry = g_array_index(userRecordArray.array, managed_UserRecord*, i);
        REQUIRE(strcmp(managed_UserRecord_get_UserDescription(entry), "Cherry") == 0);
    }

    //managed_UserRecord* resultUserRecord = managed_FSharp_useUserRecord(userRecord);
    //REQUIRE(strcmp(managed_UserRecord_get_UserDescription(resultUserRecord), "Test") == 0);

    //_Managed_UserStructArray userStructArray = managed_ArrayTest_getDefaultUserStructArray(10);
    //length = userStructArray.array->len;
    //REQUIRE(length == 10);
    //for (uint32_t i = 0; i < length; i++) {
    //    managed_UserStruct* entry = g_array_index(userStructArray.array, managed_UserStruct*, i);
    //    REQUIRE(strcmp(managed_UserStruct_get_UserDefinition(entry), "Fun!") == 0);
    //}
    managed_UserStruct* userStruct = managed_FSharp_getDefaultUserStruct();
    REQUIRE(strcmp(managed_UserStruct_get_UserDefinition(userStruct), "Fun!") == 0);
    //managed_UserStruct* resultUserStruct = managed_FSharp_useUserStruct(userStruct);
    //REQUIRE(strcmp(managed_UserStruct_get_UserDefinition(resultUserStruct), "Fun!") == 0);
}

TEST_CASE("FSharpModules.C", "[C][FSharp Modules]") {
   REQUIRE(strcmp(managed_NestedModuleTest_get_nestedConstant(), "Hello from a nested F# module") == 0);
   REQUIRE(strcmp(managed_NestedModuleTest_nestedFunction(), "Hello from a nested F# module") == 0);
}

int main( int argc, char* argv[] )
{
    // Setup a null error handler so we can test exceptions.
    mono_embeddinator_install_error_report_hook(0);

    int result = Catch::Session().run(argc, argv);
    return (result < 0xff ? result : 0xff);
}