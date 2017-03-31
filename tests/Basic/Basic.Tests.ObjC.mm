#include <catch.hpp>

#include "Basic.Managed.h"
#include "glib.h"

TEST_CASE("BuiltinTypes", "[BuiltinTypes]") {
#if 0
    BuiltinTypes* bt = [[BuiltinTypes alloc] init];
    [bt returnsVoid];
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
#endif
}

TEST_CASE("ClassTypes", "[ClassTypes]") {
#if 0
    ClassProperties* prop = ClassProperties_new();
    REQUIRE(ClassProperties_get_Int(prop) == 0);
    ClassProperties_set_Int(prop, 10);
    REQUIRE(ClassProperties_get_Int(prop) == 10);

    REQUIRE(ClassProperties_get_ReadOnlyInt(prop) == 0);
#endif
}

TEST_CASE("StaticTypes", "[StaticTypes]") {
#if 0
    REQUIRE(NonStaticClass_StaticMethod() == 0);
    REQUIRE(StaticClass_StaticMethod() == 0);
#endif
}

TEST_CASE("EnumTypes", "[EnumTypes]") {
#if 0
    REQUIRE(EnumTypes_PassEnum(Enum_Two) == 2);
    REQUIRE(EnumTypes_PassEnum(Enum_Three) == 3);

    REQUIRE(EnumTypes_PassEnumByte(EnumByte_Two) == 2);
    REQUIRE(EnumTypes_PassEnumByte(EnumByte_Three) == 3);

    REQUIRE(EnumTypes_PassEnumFlags(EnumFlags_FlagOne) == (1 << 0));
    REQUIRE(EnumTypes_PassEnumFlags(EnumFlags_FlagTwo) == (1 << 2));
#endif
}

TEST_CASE("ArrayTypes", "[ArrayTypes]") {
#if 0
    char _byte_arr[] = { 1, 2, 3 };
    _UnsignedcharArray _byte;
    _byte.array = g_array_sized_new(/*zero_terminated=*/false,
        /*clear=*/true, sizeof(char), G_N_ELEMENTS(_byte_arr));
    g_array_append_vals (_byte.array, _byte_arr, G_N_ELEMENTS(_byte_arr));

    int _sum = ArrayTypes_SumByteArray(_byte);
    REQUIRE(_sum == 6);

    _IntArray _int = ArrayTypes_ReturnsIntArray();
    REQUIRE(_int.array->len == 3);
    REQUIRE(g_array_index(_int.array, int, 0) == 1);
    REQUIRE(g_array_index(_int.array, int, 1) == 2);
    REQUIRE(g_array_index(_int.array, int, 2) == 3);

    _CharArray _string = ArrayTypes_ReturnsStringArray();
    REQUIRE(_string.array->len == 3);
    REQUIRE(strcmp(g_array_index(_string.array, gchar*, 0), "1") == 0);
    REQUIRE(strcmp(g_array_index(_string.array, gchar*, 1), "2") == 0);
    REQUIRE(strcmp(g_array_index(_string.array, gchar*, 2), "3") == 0);
#endif
}
