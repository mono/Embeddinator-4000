#import <XCTest/XCTest.h>
#if defined (TEST_FRAMEWORK)
#if defined (XAMARIN_MAC_MODERN)
#include "managed-macos-modern/managed-macos-modern.h"
#elif defined (XAMARIN_MAC_FULL)
#include "managed-macos-full/managed-macos-full.h"
#elif defined (XAMARIN_MAC_SYSTEM)
#include "managed-macos-system/managed-macos-system.h"
#elif defined (XAMARIN_IOS)
#include "managed-ios/managed-ios.h"
#elif defined (XAMARIN_TVOS)
#include "managed-tvos/managed-tvos.h"
#else
#include "managed/managed.h"
#endif
#else
#include "bindings.h"
#endif

@interface Tests : XCTestCase

@end

@implementation Tests

+ (void)setUp {
	[super setUp];
#if !defined (TEST_FRAMEWORK)
	NSBundle *bundle = [NSBundle bundleForClass:[self class]];
	NSString *path = [bundle pathForResource:@"managed" ofType:@"dll"];
	mono_embeddinator_set_assembly_path ([path UTF8String]);
#endif
}

- (void)setUp {
	[super setUp];
}

- (void)tearDown {
	[super tearDown];
}

#pragma clang diagnostic push
// our unit tests are _abusing_ nil since we know the internals of the managed code we call
#pragma clang diagnostic ignored "-Wnonnull"

- (void)testProperties {
	XCTAssertFalse ([Platform isWindows], "static class property getter only");
	XCTAssert ([Platform exitCode] == 0, "static class property getter");

	Platform.exitCode = 255;
	XCTAssert ([Platform exitCode] == 255, "static class property setter check");
	Platform.exitCode = 0; // set back to original value

	XCTAssert ([Properties_Query universalAnswer] == 42, "static property getter only");

	Properties_Query* query = [[Properties_Query alloc] init];
	XCTAssertTrue ([query isGood], "instance property getter only 1");
	XCTAssertFalse ([query isBad], "instance property getter only 2");
	XCTAssert ([query answer] == 42, "instance property getter");
	query.answer = 911;
	XCTAssert ([query answer] == 911, "instance property setter check");
	query.answer = 42; // set back to original value

	XCTAssertFalse ([query isSecret], "instance property getter only 3");
	// setter only property turned into method, so different syntax
	[query setSecret: 1];
	XCTAssertTrue ([query isSecret], "instance property getter only 4");
	[query setSecret: 0]; // set back to original value
}

- (void)testNamespaces {
	id nonamespace = [[ClassWithoutNamespace alloc] init];
	XCTAssertTrue ([[nonamespace description] containsString:@"<ClassWithoutNamespace:"], "nonamespace");
	XCTAssertEqualObjects (@"ClassWithoutNamespace", [nonamespace toString], "nonamespace toString");

	id singlenamespace = [[First_ClassWithSingleNamespace alloc] init];
	XCTAssertTrue ([[singlenamespace description] containsString:@"<First_ClassWithSingleNamespace:"], "singlenamespace");
	XCTAssertEqualObjects (@"First.ClassWithSingleNamespace", [singlenamespace toString], "singlenamespace toString");

	id nestednamespaces = [[First_Second_ClassWithNestedNamespace alloc] init];
	XCTAssertTrue ([[nestednamespaces description] containsString:@"<First_Second_ClassWithNestedNamespace:"], "nestednamespaces");
	XCTAssertEqualObjects (@"First.Second.ClassWithNestedNamespace", [nestednamespaces toString], "nestednamespaces toString");

	id nestednamespaces2 = [[First_Second_Third_ClassWithNestedNamespace alloc] init];
	XCTAssertTrue ([[nestednamespaces2 description] containsString:@"<First_Second_Third_ClassWithNestedNamespace:"], "nestednamespaces");
	XCTAssertEqualObjects (@"First.Second.Third.ClassWithNestedNamespace", [nestednamespaces2 toString], "nestednamespaces toString");
}

- (void)testExceptions {
	// .ctor that throws
	id throwers = [[Exceptions_Throwers alloc] init];
	XCTAssertNil (throwers, "Exceptions_Throwers init");

	// .cctor that throw - can't be called directly but it makes the type unusable
	id static_thrower = [[Exceptions_ThrowInStaticCtor alloc] init];
	XCTAssertNil (static_thrower, "Exceptions_ThrowInStaticCtor init");

	// .ctor chaining
	id sup1 = [[Exceptions_Super alloc] initWithBroken:false];
	XCTAssertNotNil (sup1, "not broken (as expected)");

	// beside chaining this can detect (possible, it's fine) leaks (e.g. of CGHandle)
	id sup2 = [[Exceptions_Super alloc] initWithBroken:true];
	XCTAssertNil (sup2, "broken (exception thrown in managed code)");
}

- (void)testConstructors {
	id unique_default_init = [[Constructors_Unique alloc] init];
	XCTAssert ([unique_default_init id] == 1, "default id");

	id unique_init_id = [[Constructors_Unique alloc] initWithId:911];
	XCTAssert ([unique_init_id id] == 911, "id");

	id super_unique_default_init = [[Constructors_SuperUnique alloc] init];
	XCTAssert ([super_unique_default_init id] == 411, "super id");

	Constructors_Implicit* implicit = [[Constructors_Implicit alloc] init];
	XCTAssertEqualObjects (@"OK", [implicit testResult], "implicit");

	Constructors_AllTypeCode* all1 = [[Constructors_AllTypeCode alloc] initWithB1:true c2:USHRT_MAX s:@"Mono"];
	XCTAssertTrue ([all1 testResult], "all1");

	Constructors_AllTypeCode* all2 = [[Constructors_AllTypeCode alloc] initWithI8:SCHAR_MAX i16:SHRT_MAX i32:INT_MAX i64:LONG_MAX];
	XCTAssertTrue ([all2 testResult], "all2");

	Constructors_AllTypeCode* all3 = [[Constructors_AllTypeCode alloc] initWithU8:UCHAR_MAX u16:USHRT_MAX u32:UINT_MAX u64:ULONG_MAX];
	XCTAssertTrue ([all3 testResult], "all3");

	Constructors_AllTypeCode* all4 = [[Constructors_AllTypeCode alloc] initWithF32:FLT_MAX f64:DBL_MAX];
	XCTAssertTrue ([all4 testResult], "all4");

	Constructors_DefaultValues* dv0 = [[Constructors_DefaultValues alloc] init];
	XCTAssertTrue ([dv0 isDefault], "default value 0");

	Constructors_DefaultValues* dv1 = [[Constructors_DefaultValues alloc] initWithB:0];
	XCTAssertTrue ([dv1 isDefault], "default value 1");

	Constructors_DefaultValues* dv2 = [[Constructors_DefaultValues alloc] initWithB:0 s:1];
	XCTAssertTrue ([dv2 isDefault], "default value 2");

	Constructors_DefaultValues* dv3 = [[Constructors_DefaultValues alloc] initWithB:0 s:1 i:2];
	XCTAssertTrue ([dv3 isDefault], "default value 3");

	Constructors_DefaultValues* dv4 = [[Constructors_DefaultValues alloc] initWithB:0 s:1 i:2 l:3];
	XCTAssertTrue ([dv4 isDefault], "default value 4");

	Constructors_DefaultValues* dvx = [[Constructors_DefaultValues alloc] initWithB:3 s:2 i:1 l:0];
	XCTAssertFalse ([dvx isDefault], "default value X");

	Constructors_DefaultValues* dvn0 = [[Constructors_DefaultValues alloc] initWithNonDefault:0];
	XCTAssertTrue ([dvn0 isDefault], "default value 2 / 0");

	Constructors_DefaultValues* dvn1 = [[Constructors_DefaultValues alloc] initWithNonDefault:1 s:@""];
	XCTAssertTrue ([dvn1 isDefault], "default value 2 / 1");

	Constructors_DefaultValues* dvn2 = [[Constructors_DefaultValues alloc] initWithNonDefault:2 s:@"" f:NAN];
	XCTAssertTrue ([dvn2 isDefault], "default value 2 / 2");

	Constructors_DefaultValues* dvn3 = [[Constructors_DefaultValues alloc] initWithNonDefault:3 s:@"" f:NAN d:INFINITY];
	XCTAssertTrue ([dvn3 isDefault], "default value 2 / 3");

	Constructors_DefaultValues* dvn4 = [[Constructors_DefaultValues alloc] initWithNonDefault:4 s:@"" f:NAN d:INFINITY e:Enums_ByteEnumMax];
	XCTAssertTrue ([dvn4 isDefault], "default value 2 / 4");
}

- (void)testMethods {
	id static_method = [Methods_Static createId: 1];
	XCTAssert ([static_method id] == 1, "create id");

	XCTAssertNil ([Methods_Parameters concatFirst:nil second:nil], "string input nil + nil]");
	XCTAssertEqualObjects (@"first", [Methods_Parameters concatFirst:@"first" second:nil], "string first + nil");
	XCTAssertEqualObjects (@"second", [Methods_Parameters concatFirst:nil second:@"second"], "string nil + second");
	XCTAssertEqualObjects (@"firstsecond", [Methods_Parameters concatFirst:@"first" second:@"second"], "string first + second");

	bool b = true;
	NSString* s = nil;
	[Methods_Parameters refBoolean:&b string:&s];
	XCTAssertFalse (b, "ref bool 1");
	XCTAssertEqualObjects (@"hello", s, "ref string 1");

	[Methods_Parameters refBoolean:&b string:&s];
	XCTAssertTrue (b, "ref bool 2");
	XCTAssertNil (s, "ref string 2");

	int l;
	[Methods_Parameters outString:nil length:&l upper:&s];
	XCTAssert (l == 0, "out int 1");
	XCTAssertNil (s, "out string 1");

	[Methods_Parameters outString:@"Xamarin" length:&l upper:&s];
	XCTAssert (l == 7, "out int 2");
	XCTAssertEqualObjects (@"XAMARIN", s, "ref string 2");

	id item = [Methods_Factory createItemId:1];
	XCTAssert ([item integer] == 1, "indirect creation 1");

	Methods_Collection *collection = [[Methods_Collection alloc] init];
	XCTAssert ([collection count] == 0, "count 0");
	[collection addItem:item];
	XCTAssert ([collection count] == 1, "count 1");
	XCTAssert ([[collection getItem:0] integer] == [item integer], "get 1");

	id item2 = [Methods_Factory createItemId:2];
	[collection setItem:0 value:item2];
	XCTAssert ([collection count] == 1, "count 2");
	XCTAssert ([[collection getItem:0] integer] == [item2 integer], "get 2");

	Methods_Item *nilitem = [Methods_Factory returnNull];
	XCTAssertNil(nilitem);

	[collection removeItem:item]; // not there
	XCTAssert ([collection count] == 1, "count 3");

	[collection removeItem:item2];
	XCTAssert ([collection count] == 0, "count 4");

	id default_item = [Methods_Factory createItem];
	XCTAssert ([default_item integer] == 0, "default creation 0");

	XCTAssertEqualObjects (@"", [Methods_SomeExtensions notAnExtensionMethod], "empty string");

	XCTAssert ([Methods_SomeExtensions incrementValue:1] == 2, "no category on primitive types");
}

- (void)testCategories {
	Methods_Collection *collection = [[Methods_Collection alloc] init];
	[collection addItem:nil];
	id item = [Methods_Factory createItemId:1];
	[collection addItem:item];
	XCTAssert ([collection count] == 2, "count 0");

	XCTAssert ([collection countNull] == 1, "count null");
	XCTAssert ([collection countNonNull] == 1, "count non null");

	NSString *s1 = @"";
	XCTAssertTrue ([s1 isEmptyButNotNull], "isEmptyButNotNullString 1");
	NSString *s2 = @"Category";
	XCTAssertFalse ([s2 isEmptyButNotNull], "isEmptyButNotNullString 2");
}

- (void) testStructs {
	Structs_Point* def = [[Structs_Point alloc] init];
	XCTAssert ([def x] == .0f, "x 0");
	XCTAssert ([def y] == .0f, "y 0");

	Structs_Point* p1 = [[Structs_Point alloc] initWithX:1.0f y:-1.0f];
	XCTAssert ([p1 x] == 1.0f, "x 1");
	XCTAssert ([p1 y] == -1.0f, "y 1");

	Structs_Point* p2 = [[Structs_Point alloc] initWithX:2.0f y:-2.0f];
	XCTAssert ([p2 x] == 2.0f, "x 2");
	XCTAssert ([p2 y] == -2.0f, "y 2");

	XCTAssert ([Structs_Point areEqual:p1 right:p1], "p1 == p1");
	XCTAssert ([Structs_Point areEqual:p1 right:p1], "p2 == p2");
	XCTAssert ([Structs_Point areEqual:p1 right:p2] == NO, "p1 != p2");

	Structs_Point* p3 = [Structs_Point add:p1 right:p2];
	XCTAssert ([p3 x] == 3.0f, "x 3");
	XCTAssert ([p3 y] == -3.0f, "y 3");

	Structs_Point* p4 = [Structs_Point subtract:p3 right:p2];
	XCTAssert ([Structs_Point areEqual:p4 right:p1], "p4 == p1");

	Structs_Point* z = [Structs_Point zero];
	XCTAssert ([z x] == 0.0f, "x 4");
	XCTAssert ([z y] == 0.0f, "y 4");
}

- (void) testEnums {
	Enums_IntEnum i = Enums_IntEnumMin;
	Enums_ShortEnum s;
	Enums_ByteFlags f = [Enums_Enumer testB:Enums_ByteEnumMax i:&i s:&s];
	XCTAssert (f == 0x22, "return flag 1");
	XCTAssert (i == Enums_IntEnumMax, "ref enum 1");
	XCTAssert (s == Enums_ShortEnumMax, "out enum 1");

	f = [Enums_Enumer testB:Enums_ByteEnumZero i:&i s:&s];
	XCTAssert (i == Enums_IntEnumMin, "ref enum 2");
	XCTAssert (s == Enums_ShortEnumMin, "out enum 2");
}

- (void) testFieldsInReference {
	XCTAssert ([Fields_Class maxLong] == LONG_MAX, "class const");

	XCTAssert (Fields_Class.integer == 0, "static field unset");
	Fields_Class.integer = 1;
	XCTAssert (Fields_Class.integer == 1, "static field set");

	XCTAssertTrue (Fields_Class.scratch.boolean, "scratch default");

	Fields_Class.scratch = [[Fields_Class alloc] initWithEnabled:false];
	XCTAssertFalse (Fields_Class.scratch.boolean, "scratch re-assign");

	Fields_Class *ref1 = [[Fields_Class alloc] initWithEnabled:true];
	XCTAssertTrue (ref1.boolean, "init / boolean / true");
	ref1.boolean = false;
	XCTAssertFalse (ref1.boolean, "init / boolean / set 1");

	XCTAssertNotNil (ref1.structure, "init / class initialized 1");
	XCTAssertFalse (ref1.structure.boolean, "init / class / boolean / default");
	ref1.structure = [[Fields_Struct alloc] initWithEnabled:true];
	XCTAssertTrue (ref1.structure.boolean, "init / class / boolean / true");

	Fields_Class *ref2 = [[Fields_Class alloc] initWithEnabled:false];
	XCTAssertNotNil ([ref2 structure], "init / class initialized 2");
	XCTAssertFalse ([ref2 boolean], "init / boolean / false");
}

- (void) testFieldsInValueType {
	XCTAssert (Fields_Struct.integer == 0, "static valuetype field unset");
	Fields_Struct.integer = 1;
	XCTAssert (Fields_Struct.integer == 1, "static valuetype field set");

	XCTAssertFalse (Fields_Struct.scratch.boolean, "scratch default");

	Fields_Struct.scratch = [[Fields_Struct alloc] initWithEnabled:true];
	XCTAssertTrue (Fields_Struct.scratch.boolean, "scratch re-assign");

	Fields_Struct *empty = [Fields_Struct empty];
	XCTAssertNotNil (empty, "empty / struct static readonly");
	XCTAssertNil ([empty managedClass], "empty / class uninitialized");

	Fields_Struct *struct1 = [[Fields_Struct alloc] initWithEnabled:true];
	XCTAssertTrue (struct1.boolean, "init / boolean / true");
	struct1.boolean = false;
	XCTAssertFalse (struct1.boolean, "init / boolean / set 1");

	XCTAssertNotNil (struct1.managedClass, "init / class initialized 1");
	XCTAssertFalse (struct1.managedClass.boolean, "init / class / boolean / default");
	struct1.managedClass = nil;
	XCTAssertNil (struct1.managedClass, "init / class set 1");
	struct1.managedClass = [[Fields_Class alloc] initWithEnabled:true];
	XCTAssertTrue (struct1.managedClass.boolean, "init / class / boolean / true");

	Fields_Struct *struct2 = [[Fields_Struct alloc] initWithEnabled:false];
	XCTAssertNotNil ([struct2 class], "init / class initialized 2");
	XCTAssertFalse ([struct2 boolean], "init / boolean / false");
}

- (void) testComparable {
	// nil behaviour check
	NSDate *d1 = [[NSDate alloc] initWithTimeIntervalSinceNow:0];
	NSDate *d2 = nil;
	XCTAssert ([d1 compare:d2] == NSOrderedSame, "foundation / compare w/nil");

	Comparable_Class *c1 = [[Comparable_Class alloc] initWithI:1];
	XCTAssert ([c1 compare:nil] == NSOrderedSame, "compare w/nil");
	XCTAssert ([c1 compare:c1] == NSOrderedSame, "compare self");

	Comparable_Class *c2 = [[Comparable_Class alloc] initWithI:2];
	XCTAssert ([c1 compare:c2] == NSOrderedAscending, "compare <");
	XCTAssert ([c2 compare:c1] == NSOrderedDescending, "compare >");

	Comparable_Generic *g1 = [[Comparable_Generic alloc] initWithI:-3];
	XCTAssert ([g1 compare:nil] == NSOrderedSame, "generic / compare w/nil");
	XCTAssert ([g1 compare:g1] == NSOrderedSame, "generic / compare self");

	Comparable_Generic *g2 = [[Comparable_Generic alloc] initWithI:-1];
	XCTAssert ([g1 compare:g2] == NSOrderedAscending, "generic / compare <");
	XCTAssert ([g2 compare:g1] == NSOrderedDescending, "generic / compare >");

	Comparable_Both *b1 = [[Comparable_Both alloc] initWithI:10];
	XCTAssert ([b1 compare:nil] == NSOrderedSame, "both / compare w/nil");
	XCTAssert ([b1 compare:b1] == NSOrderedSame, "both / compare self");

	Comparable_Both *b2 = [[Comparable_Both alloc] initWithI:20];
	XCTAssert ([b1 compare:b2] == NSOrderedAscending, "both / compare <");
	XCTAssert ([b2 compare:b1] == NSOrderedDescending, "both / compare >");

	// normally bound methods for IComparable<T> where T is not the current type
	Comparable_Different *d = [[Comparable_Different alloc] initWithI:-2];
	XCTAssert ([d compareToGeneric:g2] == NSOrderedAscending, "different generic / compare <");
	XCTAssert ([d compareToGeneric:g1] == NSOrderedDescending, "different generic / compare >");

	XCTAssert ([d compareToInteger:10] == NSOrderedAscending, "different int / compare <");
	XCTAssert ([d compareToInteger:-10] == NSOrderedDescending, "different int / compare >");
}

- (void)testStaticCallPerformance {
	const int iterations = 1000000;
	[self measureBlock:^{
		int n = 0;
		for (int i = 0; i < iterations; i++) {
			if ([Platform isWindows])
				++n;
		}
		XCTAssert (n == 0);
	}];
}

- (void)testTypes {
	XCTAssertEqual (0, [Type_Char min], "char min");
	XCTAssertEqual (USHRT_MAX, [Type_Char max], "char max");
	XCTAssertEqual (0, [Type_Char zero], "char zero");

	XCTAssertEqual (SCHAR_MIN, [Type_SByte min], "sbyte min");
	XCTAssertEqual (SCHAR_MAX, [Type_SByte max], "sbyte max");

	XCTAssertEqual (0, [Type_Byte min], "byte min");
	XCTAssertEqual (UCHAR_MAX, [Type_Byte max], "byte max");

	XCTAssertEqual (SHRT_MIN, [Type_Int16 min], "short min");
	XCTAssertEqual (SHRT_MAX, [Type_Int16 max], "short max");

	XCTAssertEqual (INT_MIN, [Type_Int32 min], "int min");
	XCTAssertEqual (INT_MAX, [Type_Int32 max], "int max");

	XCTAssertEqual (LONG_MIN, [Type_Int64 min], "long min");
	XCTAssertEqual (LONG_MAX, [Type_Int64 max], "long max");

	XCTAssertEqual (0, [Type_UInt16 min], "ushort min");
	XCTAssertEqual (USHRT_MAX, [Type_UInt16 max], "ushort max");

	XCTAssertEqual (0, [Type_UInt32 min], "uint min");
	XCTAssertEqual (UINT_MAX, [Type_UInt32 max], "uint max");

	XCTAssertEqual (0, [Type_UInt64 min], "ulong min");
	XCTAssertEqual (ULONG_MAX, [Type_UInt64 max], "ulong max");

	XCTAssertEqual (-FLT_MAX, [Type_Single min], "single min");
	XCTAssertEqual (FLT_MAX, [Type_Single max], "single max");

	XCTAssertEqual (-DBL_MAX, [Type_Double min], "double min");
	XCTAssertEqual (DBL_MAX, [Type_Double max], "double max");

	XCTAssertEqualObjects ((__bridge NSString *) nil, [Type_String nullString], "null string");
	XCTAssertEqualObjects (@"", [Type_String emptyString], "empty string");
	XCTAssertEqualObjects (@"Hello World", [Type_String nonEmptyString], "non-empty string");

	NSDecimalNumber *decimalmax = [Type_Decimal max];
	NSDecimalNumber *nsdecimalmax = [NSDecimalNumber decimalNumberWithString:@"79228162514264337593543950335"];
	XCTAssertEqualObjects (nsdecimalmax, decimalmax, "NSDecimalNumber max");

	NSDecimalNumber *decimalmin = [Type_Decimal min];
	NSDecimalNumber *nsdecimalmin = [NSDecimalNumber decimalNumberWithString:@"-79228162514264337593543950335"];
	XCTAssertEqualObjects (nsdecimalmin, decimalmin, "NSDecimalNumber min");

	NSDecimalNumber *decimalzero = [Type_Decimal zero];
	NSDecimalNumber *nsdecimalzero = [NSDecimalNumber zero];
	XCTAssertEqualObjects (nsdecimalzero, decimalzero, "NSDecimalNumber zero");

	NSDecimalNumber *decimalone = [Type_Decimal one];
	NSDecimalNumber *nsdecimalone = [NSDecimalNumber one];
	XCTAssertEqualObjects (nsdecimalone, decimalone, "NSDecimalNumber one");

	NSDecimalNumber *decimalminusone = [Type_Decimal minusOne];
	NSDecimalNumber *nsdecimalminusone = [NSDecimalNumber decimalNumberWithString:@"-1"];
	XCTAssertEqualObjects (nsdecimalminusone, decimalminusone, "NSDecimalNumber minusOne");

	NSDecimalNumber *decimalpi = [Type_Decimal pi];
	NSDecimalNumber *nsdecimalpi = [NSDecimalNumber decimalNumberWithString:@"3.14159265358979323846264"];
	XCTAssertEqualObjects (nsdecimalpi, decimalpi, "NSDecimalNumber pi");

	NSDecimalNumber *decimalminustau = [Type_Decimal minusTau];
	NSDecimalNumber *nsdecimalminustau = [NSDecimalNumber decimalNumberWithString:@"-6.28318530717958647692"];
	XCTAssertEqualObjects (nsdecimalminustau, decimalminustau, "NSDecimalNumber tau");

	NSDecimalNumber *decimalfortytwo = [Type_Decimal fortyTwo];
	NSDecimalNumber *nsdecimalfortytwo = [NSDecimalNumber decimalNumberWithString:@"42"];
	XCTAssertEqualObjects (nsdecimalfortytwo, decimalfortytwo, "NSDecimalNumber fortytwo");

	NSArray<NSDecimalNumber *> *decimalarr = [Type_Decimal decArr];
	XCTAssertEqual([decimalarr count], 8, "decimalarr count");
	XCTAssertEqualObjects (nsdecimalmax, decimalarr[0], "decimalarr[0] max");
	XCTAssertEqualObjects (nsdecimalmin, decimalarr[1], "decimalarr[1] min");
	XCTAssertEqualObjects (nsdecimalzero, decimalarr[2], "decimalarr[2] zero");
	XCTAssertEqualObjects (nsdecimalone, decimalarr[3], "decimalarr[3] one");
	XCTAssertEqualObjects (nsdecimalminusone, decimalarr[4], "decimalarr[4] minusOne");
	XCTAssertEqualObjects (nsdecimalpi, decimalarr[5], "decimalarr[5] pi");
	XCTAssertEqualObjects (nsdecimalminustau, decimalarr[6], "decimalarr[6] tau");
	XCTAssertEqualObjects (nsdecimalfortytwo, decimalarr[7], "decimalarr[7] fortytwo");

	for (NSDecimalNumber *dec in decimalarr) {
		NSDecimalNumber *decimalmeth = [Type_Decimal getDecimalDec:dec];
		XCTAssertEqualObjects (dec, decimalmeth, "decimal array");
	}

	NSArray<NSDecimalNumber *> *decimalarr2 = [Type_Decimal getDecimalArrDec:decimalarr];
	XCTAssertEqual([decimalarr count], 8, "decimalarr count");
	XCTAssertEqualObjects (decimalarr[0], decimalarr2[0], "decimalarr2[0] max");
	XCTAssertEqualObjects (decimalarr[1], decimalarr2[1], "decimalarr2[1] min");
	XCTAssertEqualObjects (decimalarr[2], decimalarr2[2], "decimalarr2[2] zero");
	XCTAssertEqualObjects (decimalarr[3], decimalarr2[3], "decimalarr2[3] one");
	XCTAssertEqualObjects (decimalarr[4], decimalarr2[4], "decimalarr2[4] minusOne");
	XCTAssertEqualObjects (decimalarr[5], decimalarr2[5], "decimalarr2[5] pi");
	XCTAssertEqualObjects (decimalarr[6], decimalarr2[6], "decimalarr2[6] tau");
	XCTAssertEqualObjects (decimalarr[7], decimalarr2[7], "decimalarr2[7] fortytwo");

	NSDecimalNumber *refDec = [Type_Decimal minusTau];
	XCTAssertEqualObjects (nsdecimalminustau, refDec, "NSDecimalNumber refDec tau");
	[Type_Decimal getRefPiDec:&refDec];
	XCTAssertEqualObjects (nsdecimalpi, refDec, "NSDecimalNumber refDec pi");

	NSArray<NSDecimalNumber *> *decimalrefarr = [Type_Decimal decArr];
	[Type_Decimal reverseDecimalArrRefDecArr:&decimalrefarr];
	XCTAssertEqual([decimalrefarr count], 8, "decimalarr count");
	XCTAssertEqualObjects (nsdecimalmax, decimalrefarr[7], "decimalrefarr[7] max");
	XCTAssertEqualObjects (nsdecimalmin, decimalrefarr[6], "decimalrefarr[6] min");
	XCTAssertEqualObjects (nsdecimalzero, decimalrefarr[5], "decimalrefarr[5] zero");
	XCTAssertEqualObjects (nsdecimalone, decimalrefarr[4], "decimalrefarr[4] one");
	XCTAssertEqualObjects (nsdecimalminusone, decimalrefarr[3], "decimalrefarr[3] minusOne");
	XCTAssertEqualObjects (nsdecimalpi, decimalrefarr[2], "decimalrefarr[2] pi");
	XCTAssertEqualObjects (nsdecimalminustau, decimalrefarr[1], "decimalrefarr[1] tau");
	XCTAssertEqualObjects (nsdecimalfortytwo, decimalrefarr[0], "decimalrefarr[0] fortytwo");

	NSArray<NSDecimalNumber *> *decimalrefNilarr = nil;
	[Type_Decimal reverseDecimalArrRefDecArr:&decimalrefNilarr];
	XCTAssertNil (decimalrefNilarr, "decimalrefNilarr");

	NSDate *distantFuture = [Type_DateTime returnDateDatetime:[NSDate distantFuture]];
	XCTAssertNotNil (distantFuture, "distantFuture");

	NSDate *verydistantFuture = [NSDate dateWithTimeIntervalSinceReferenceDate:[distantFuture timeIntervalSinceReferenceDate] * 8];
	NSDate *netmaxverydistantFuture = [Type_DateTime returnDateDatetime:verydistantFuture];
	NSDate *nsnetmax = [NSDate dateWithTimeIntervalSinceReferenceDate:(NSTimeInterval) 252423993600];
	XCTAssertEqualWithAccuracy ([nsnetmax timeIntervalSinceReferenceDate], [netmaxverydistantFuture timeIntervalSinceReferenceDate], 0.001, "netmaxverydistantFuture");

	NSDate *distantPast = [Type_DateTime returnDateDatetime:[NSDate distantPast]];
	NSDate *nsnetmin = [NSDate dateWithTimeIntervalSinceReferenceDate:(NSTimeInterval) -63113904000];
	XCTAssertEqualWithAccuracy ([nsnetmin timeIntervalSinceReferenceDate], [distantPast timeIntervalSinceReferenceDate], 0.001, "DateTime distantPast");

	NSDate *netmax = Type_DateTime.max;
	XCTAssertEqualWithAccuracy ([nsnetmax timeIntervalSinceReferenceDate], [netmax timeIntervalSinceReferenceDate], 0.001, "DateTime Max");

	NSDate *netmin = Type_DateTime.min;
	XCTAssertEqualWithAccuracy ([nsnetmin timeIntervalSinceReferenceDate], [netmin timeIntervalSinceReferenceDate], 0.001, "DateTime Min");

	NSDate *refdate = nil;
	[Type_DateTime refDateDatetime:&refdate];
	XCTAssertEqualWithAccuracy ([nsnetmin timeIntervalSinceReferenceDate], [refdate timeIntervalSinceReferenceDate], 0.001, "DateTime refdate");

	NSDate *nilTest = [Type_DateTime returnDateDatetime:nil];
	XCTAssertEqualWithAccuracy ([nsnetmin timeIntervalSinceReferenceDate], [nilTest timeIntervalSinceReferenceDate], 0.001, "DateTime nilTest");

	NSArray<NSDate *> *datesArr = @[netmax, netmin];
	[Type_DateTime reverseRefDatesDates:&datesArr];
	XCTAssertEqualWithAccuracy ([nsnetmin timeIntervalSinceReferenceDate], [datesArr[0] timeIntervalSinceReferenceDate], 0.001, "DateTime reverseRefDatesDates Min");
	XCTAssertEqualWithAccuracy ([nsnetmax timeIntervalSinceReferenceDate], [datesArr[1] timeIntervalSinceReferenceDate], 0.001, "DateTime reverseRefDatesDates Max");

	NSArray<NSDate *> *revdatesArr = [Type_DateTime reverseDatesDates:@[netmax, netmin]];
	XCTAssertEqualWithAccuracy ([nsnetmin timeIntervalSinceReferenceDate], [revdatesArr[0] timeIntervalSinceReferenceDate], 0.001, "DateTime reverseDatesDates Min");
	XCTAssertEqualWithAccuracy ([nsnetmax timeIntervalSinceReferenceDate], [revdatesArr[1] timeIntervalSinceReferenceDate], 0.001, "DateTime reverseDatesDates Max");

	NSArray<NSDate *> *refnilarrdate = nil;
	[Type_DateTime reverseRefDatesDates:&refnilarrdate];
	XCTAssertNil (refnilarrdate, "DateTime refnilarrdate");

	NSArray<NSDate *> *revdatesnillArr = [Type_DateTime reverseDatesDates:nil];
	XCTAssertNil (revdatesnillArr, "DateTime revdatesnillArr");
}

- (void) testObjectIndexedSubscripting {
	Subscripts_BoolCollection *boolCollection = [[Subscripts_BoolCollection alloc] init];
	XCTAssert ([boolCollection count] == 0, "count 0");
	[boolCollection addItem:YES];
	XCTAssert ([boolCollection count] == 1, "count 1");
	XCTAssert ([boolCollection [0] isEqual:@YES], "get 0");
	boolCollection[0] = @NO;
	XCTAssert ([boolCollection [0] isEqual:@NO], "get 1");

	Subscripts_SbyteCollection *sbyteCollection = [[Subscripts_SbyteCollection alloc] init];
	XCTAssert ([sbyteCollection count] == 0, "count 2");
	[sbyteCollection addItem:42];
	XCTAssert ([sbyteCollection count] == 1, "count 3");
	XCTAssert ([sbyteCollection [0] isEqual:@42], "get 2");
	sbyteCollection[0] = @13;
	XCTAssert ([sbyteCollection [0] isEqual:@13], "get 3");

	Subscripts_ByteCollection *byteCollection = [[Subscripts_ByteCollection alloc] init];
	XCTAssert ([byteCollection count] == 0, "count 4");
	[byteCollection addItem:42];
	XCTAssert ([byteCollection count] == 1, "count 5");
	XCTAssert ([byteCollection [0] isEqual:@42], "get 4");
	byteCollection[0] = @13;
	XCTAssert ([byteCollection [0] isEqual:@13], "get 5");

	Subscripts_ShortCollection *shortCollection = [[Subscripts_ShortCollection alloc] init];
	XCTAssert ([shortCollection count] == 0, "count 6");
	[shortCollection addItem:42];
	XCTAssert ([shortCollection count] == 1, "count 7");
	XCTAssert ([shortCollection [0] isEqual:@42], "get 6");
	shortCollection[0] = @13;
	XCTAssert ([shortCollection [0] isEqual:@13], "get 7");

	Subscripts_UshortCollection *ushortCollection = [[Subscripts_UshortCollection alloc] init];
	XCTAssert ([ushortCollection count] == 0, "count 8");
	[ushortCollection addItem:42];
	XCTAssert ([ushortCollection count] == 1, "count 9");
	XCTAssert ([ushortCollection [0] isEqual:@42], "get 8");
	ushortCollection[0] = @13;
	XCTAssert ([ushortCollection [0] isEqual:@13], "get 9");

	Subscripts_IntCollection *intCollection = [[Subscripts_IntCollection alloc] init];
	XCTAssert ([intCollection count] == 0, "count 10");
	[intCollection addItem:42];
	XCTAssert ([intCollection count] == 1, "count 11");
	XCTAssert ([intCollection [0] isEqual:@42], "get 10");
	intCollection[0] = @13;
	XCTAssert ([intCollection [0] isEqual:@13], "get 11");

	Subscripts_UintCollection *uintCollection = [[Subscripts_UintCollection alloc] init];
	XCTAssert ([uintCollection count] == 0, "count 12");
	[uintCollection addItem:42];
	XCTAssert ([uintCollection count] == 1, "count 13");
	XCTAssert ([uintCollection [0] isEqual:@42], "get 12");
	uintCollection[0] = @13;
	XCTAssert ([uintCollection [0] isEqual:@13], "get 13");

	Subscripts_LongCollection *longCollection = [[Subscripts_LongCollection alloc] init];
	XCTAssert ([longCollection count] == 0, "count 14");
	[longCollection addItem:42];
	XCTAssert ([longCollection count] == 1, "count 15");
	XCTAssert ([longCollection [0] isEqual:@42], "get 14");
	longCollection[0] = @13;
	XCTAssert ([longCollection [0] isEqual:@13], "get 15");

	Subscripts_UlongCollection *ulongCollection = [[Subscripts_UlongCollection alloc] init];
	XCTAssert ([ulongCollection count] == 0, "count 16");
	[ulongCollection addItem:42];
	XCTAssert ([ulongCollection count] == 1, "count 17");
	XCTAssert ([ulongCollection [0] isEqual:@42], "get 16");
	ulongCollection[0] = @13;
	XCTAssert ([ulongCollection [0] isEqual:@13], "get 17");

	Subscripts_FloatCollection *floatCollection = [[Subscripts_FloatCollection alloc] init];
	XCTAssert ([floatCollection count] == 0, "count 18");
	[floatCollection addItem:42];
	XCTAssert ([floatCollection count] == 1, "count 19");
	XCTAssert ([floatCollection [0] isEqual:@42], "get 18");
	floatCollection[0] = @13;
	XCTAssert ([floatCollection [0] isEqual:@13], "get 19");

	Subscripts_DoubleCollection *doubleCollection = [[Subscripts_DoubleCollection alloc] init];
	XCTAssert ([doubleCollection count] == 0, "count 20");
	[doubleCollection addItem:42];
	XCTAssert ([doubleCollection count] == 1, "count 21");
	XCTAssert ([doubleCollection [0] isEqual:@42], "get 20");
	doubleCollection[0] = @13;
	XCTAssert ([doubleCollection [0] isEqual:@13], "get 21");

	Subscripts_CharCollection *charCollection = [[Subscripts_CharCollection alloc] init];
	XCTAssert ([charCollection count] == 0, "count 22");
	[charCollection addItem:42];
	XCTAssert ([charCollection count] == 1, "count 23");
	XCTAssert ([charCollection [0] isEqual:@42], "get 22");
	charCollection[0] = @13;
	XCTAssert ([charCollection [0] isEqual:@13], "get 23");

	Subscripts_StringCollection *stringCollection = [[Subscripts_StringCollection alloc] init];
	XCTAssert ([stringCollection count] == 0, "count 24");
	[stringCollection addItem:@"asdf"];
	XCTAssert ([stringCollection count] == 1, "count 25");
	XCTAssert ([stringCollection [0] isEqual:@"asdf"], "get 24");
	stringCollection[0] = @"fdsa";
	XCTAssert ([stringCollection [0] isEqual:@"fdsa"], "get 25");
}

- (void) testObjectKeyedSubscripting {
	Subscripts_BoolDictionaryCollection *BoolCollection = [[Subscripts_BoolDictionaryCollection alloc] init];
	XCTAssert ([BoolCollection count] == 0, "count 0");
	BoolCollection[@"asdf"] = @YES;
	XCTAssert ([BoolCollection count] == 1, "count 1");
	XCTAssert ([BoolCollection [@"asdf"] isEqual:@YES], "get 0");
	BoolCollection[@"asdf"] = @NO;
	XCTAssert ([BoolCollection [@"asdf"] isEqual:@NO], "get 1");

	Subscripts_SbyteDictionaryCollection *SbyteCollection = [[Subscripts_SbyteDictionaryCollection alloc] init];
	XCTAssert ([SbyteCollection count] == 0, "count 2");
	SbyteCollection[@"asdf"] = @42;
	XCTAssert ([SbyteCollection count] == 1, "count 3");
	XCTAssert ([SbyteCollection [@"asdf"] isEqual:@42], "get 2");
	SbyteCollection[@"asdf"] = @13;
	XCTAssert ([SbyteCollection [@"asdf"] isEqual:@13], "get 3");

	Subscripts_ByteDictionaryCollection *ByteCollection = [[Subscripts_ByteDictionaryCollection alloc] init];
	XCTAssert ([ByteCollection count] == 0, "count 4");
	ByteCollection[@"asdf"] = @42;
	XCTAssert ([ByteCollection count] == 1, "count 5");
	XCTAssert ([ByteCollection [@"asdf"] isEqual:@42], "get 4");
	ByteCollection[@"asdf"] = @13;
	XCTAssert ([ByteCollection [@"asdf"] isEqual:@13], "get 5");

	Subscripts_ShortDictionaryCollection *ShortCollection = [[Subscripts_ShortDictionaryCollection alloc] init];
	XCTAssert ([ShortCollection count] == 0, "count 6");
	ShortCollection[@"asdf"] = @42;
	XCTAssert ([ShortCollection count] == 1, "count 7");
	XCTAssert ([ShortCollection [@"asdf"] isEqual:@42], "get 6");
	ShortCollection[@"asdf"] = @13;
	XCTAssert ([ShortCollection [@"asdf"] isEqual:@13], "get 7");

	Subscripts_UshortDictionaryCollection *UshortCollection = [[Subscripts_UshortDictionaryCollection alloc] init];
	XCTAssert ([UshortCollection count] == 0, "count 8");
	UshortCollection[@"asdf"] = @42;
	XCTAssert ([UshortCollection count] == 1, "count 9");
	XCTAssert ([UshortCollection [@"asdf"] isEqual:@42], "get 8");
	UshortCollection[@"asdf"] = @13;
	XCTAssert ([UshortCollection [@"asdf"] isEqual:@13], "get 9");

	Subscripts_IntDictionaryCollection *IntCollection = [[Subscripts_IntDictionaryCollection alloc] init];
	XCTAssert ([IntCollection count] == 0, "count 10");
	IntCollection[@"asdf"] = @42;
	XCTAssert ([IntCollection count] == 1, "count 11");
	XCTAssert ([IntCollection [@"asdf"] isEqual:@42], "get 10");
	IntCollection[@"asdf"] = @13;
	XCTAssert ([IntCollection [@"asdf"] isEqual:@13], "get 11");

	Subscripts_UintDictionaryCollection *UintCollection = [[Subscripts_UintDictionaryCollection alloc] init];
	XCTAssert ([UintCollection count] == 0, "count 12");
	UintCollection[@"asdf"] = @42;
	XCTAssert ([UintCollection count] == 1, "count 13");
	XCTAssert ([UintCollection [@"asdf"] isEqual:@42], "get 12");
	UintCollection[@"asdf"] = @13;
	XCTAssert ([UintCollection [@"asdf"] isEqual:@13], "get 13");

	Subscripts_LongDictionaryCollection *LongCollection = [[Subscripts_LongDictionaryCollection alloc] init];
	XCTAssert ([LongCollection count] == 0, "count 14");
	LongCollection[@"asdf"] = @42;
	XCTAssert ([LongCollection count] == 1, "count 15");
	XCTAssert ([LongCollection [@"asdf"] isEqual:@42], "get 14");
	LongCollection[@"asdf"] = @13;
	XCTAssert ([LongCollection [@"asdf"] isEqual:@13], "get 15");

	Subscripts_UlongDictionaryCollection *UlongCollection = [[Subscripts_UlongDictionaryCollection alloc] init];
	XCTAssert ([UlongCollection count] == 0, "count 16");
	UlongCollection[@"asdf"] = @42;
	XCTAssert ([UlongCollection count] == 1, "count 17");
	XCTAssert ([UlongCollection [@"asdf"] isEqual:@42], "get 16");
	UlongCollection[@"asdf"] = @13;
	XCTAssert ([UlongCollection [@"asdf"] isEqual:@13], "get 17");

	Subscripts_FloatDictionaryCollection *FloatCollection = [[Subscripts_FloatDictionaryCollection alloc] init];
	XCTAssert ([FloatCollection count] == 0, "count 18");
	FloatCollection[@"asdf"] = @42;
	XCTAssert ([FloatCollection count] == 1, "count 19");
	XCTAssert ([FloatCollection [@"asdf"] isEqual:@42], "get 18");
	FloatCollection[@"asdf"] = @13;
	XCTAssert ([FloatCollection [@"asdf"] isEqual:@13], "get 19");

	Subscripts_DoubleDictionaryCollection *DoubleCollection = [[Subscripts_DoubleDictionaryCollection alloc] init];
	XCTAssert ([DoubleCollection count] == 0, "count 20");
	DoubleCollection[@"asdf"] = @42;
	XCTAssert ([DoubleCollection count] == 1, "count 21");
	XCTAssert ([DoubleCollection [@"asdf"] isEqual:@42], "get 20");
	DoubleCollection[@"asdf"] = @13;
	XCTAssert ([DoubleCollection [@"asdf"] isEqual:@13], "get 21");

	Subscripts_CharDictionaryCollection *CharCollection = [[Subscripts_CharDictionaryCollection alloc] init];
	XCTAssert ([CharCollection count] == 0, "count 22");
	CharCollection[@"asdf"] = @42;
	XCTAssert ([CharCollection count] == 1, "count 23");
	XCTAssert ([CharCollection [@"asdf"] isEqual:@42], "get 22");
	CharCollection[@"asdf"] = @13;
	XCTAssert ([CharCollection [@"asdf"] isEqual:@13], "get 23");

	Subscripts_StringDictionaryCollection *StringCollection = [[Subscripts_StringDictionaryCollection alloc] init];
	XCTAssert ([StringCollection count] == 0, "count 24");
	StringCollection[@"asdf"] = @"one";
	XCTAssert ([StringCollection count] == 1, "count 25");
	XCTAssert ([StringCollection [@"asdf"] isEqual:@"one"], "get 24");
	StringCollection[@"asdf"] = @"two";
	XCTAssert ([StringCollection [@"asdf"] isEqual:@"two"], "get 25");
}

- (void) testDuplicateNaming {
	// The DuplicateMethods class has a number of duplicate methods with different arguments
	// This test verifies we output the best converted names, using argument names instead of types
	// where possible
	Methods_DuplicateMethods *m = [[Methods_DuplicateMethods alloc] init];

	XCTAssert ([m doIt] == 42, "doIt 1");
	XCTAssert ([m doItIntValue:0] == 42, "doIt 2");
	XCTAssert ([m doItStringValue:@""] == 42, "doIt 3");
	XCTAssert ([m doItI:0 j:1] == 84, "doIt 4");
	XCTAssert ([m findName:@"name"] == YES, "doIt 5");
	XCTAssert ([m findFirstName:@"name" lastName:@"last"] == YES, "doIt 6");

	Properties_DuplicateIndexedProperties * p = [[Properties_DuplicateIndexedProperties alloc] init];
	XCTAssert ([p getItemIntValue:0] == 42, "getItemInt32");
	XCTAssert ([p getItemStringValue:@""] == 42, "getItemString");

	Constructors_Duplicates * c = [[Constructors_Duplicates alloc] initWithUCharValue:1 uCharValue:2 uCharValue:3 uCharValue:4];
	XCTAssertNotNil (c, "c");
	Constructors_Duplicates * c2 = [[Constructors_Duplicates alloc] initWithUCharValue:1 shortValue:2 intValue:3 longValue:4];
	XCTAssertNotNil (c2, "c2");
	Constructors_Duplicates * c3 = [[Constructors_Duplicates alloc] initWithIntValue:1 intValue:2 intValue:3 intValue:4];
	XCTAssertNotNil (c3, "c3");
}

   - (void) testIsEqual {
	EqualsHashOverrides_Class *c1 = [[EqualsHashOverrides_Class alloc] initWithX:1];
	XCTAssertFalse ([c1 isEqual:nil], "equals nil");
	XCTAssertFalse ([c1 isEqual:@"String"], "equals non-mono NSObject");
	XCTAssertTrue ([c1 isEqual:c1], "equals self");

	EqualsHashOverrides_Class *c2 = [[EqualsHashOverrides_Class alloc] initWithX:1];
	XCTAssertTrue ([c1 isEqual:c2], "compare equal objects");
	XCTAssertTrue ([c2 isEqual:c1], "compare equal objects");

	EqualsHashOverrides_Class *c3 = [[EqualsHashOverrides_Class alloc] initWithX:2];
	XCTAssertFalse ([c1 isEqual:c3], "compare unequal objects");
	XCTAssertFalse ([c3 isEqual:c1], "compare unequal objects");
}

- (void) testHash {
	EqualsHashOverrides_Class *c1 = [[EqualsHashOverrides_Class alloc] initWithX:1];
	EqualsHashOverrides_Class *c2 = [[EqualsHashOverrides_Class alloc] initWithX:1];
	EqualsHashOverrides_Class *c3 = [[EqualsHashOverrides_Class alloc] initWithX:2];

	XCTAssertTrue ([c1 hash] == [c2 hash], "Equal objects have matching hash");
	XCTAssertFalse ([c1 hash] == [c3 hash], "Non-equal objects have different hashes");
}

- (void) testEquatable {
	EqualsHashOverrides_EquatableClass *c1 = [[EqualsHashOverrides_EquatableClass alloc] initWithY:3];
	EqualsHashOverrides_Class *c2 = [[EqualsHashOverrides_Class alloc] initWithX:3];
	EqualsHashOverrides_Class *c3 = [[EqualsHashOverrides_Class alloc] initWithX:1];

	XCTAssertTrue ([c1 isEqualToEqualsHashOverrides_Class:c2], "Equatable matches another class");
	XCTAssertFalse ([c1 isEqualToEqualsHashOverrides_Class:c3], "Equatable does not match another class");

	EqualsHashOverrides_EquatableInt *c4 = [[EqualsHashOverrides_EquatableInt alloc] initWithY:5];

	XCTAssertTrue ([c4 isEqualToInt:5], "Equatable int matches");
	XCTAssertFalse ([c4 isEqualToInt:7], "Equatable int does not match");
}

- (void)testProtocols {
	id<Interfaces_IMakeItUp> m = [Interfaces_Supplier create];
	XCTAssertTrue ([m conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "conformsToProtocol 1");
	XCTAssertTrue ([m boolean], "true");
	XCTAssertFalse ([m boolean], "false");

	XCTAssertEqualObjects (@"0", [m convertIntValue:0], "0");
	XCTAssertEqualObjects (@"1", [m convertLongValue:1ll], "1");

	Interfaces_ManagedAdder *adder = [[Interfaces_ManagedAdder alloc] init];
	XCTAssertTrue ([adder conformsToProtocol:@protocol(Interfaces_IOperations)], "conformsToProtocol 2");
	XCTAssertTrue ([Interfaces_OpConsumer doAdditionOps:adder a:40 b:2] == 42, "doAdditionOps");
	XCTAssertTrue ([Interfaces_OpConsumer testManagedAdderA:1 b:-1], "testManagedAdder");

	// FIXME: today it is not possible to define a native type that conforms
	// to `Interfaces_IOperations` and pass it on the managed side (abort)
}

- (void) testOperatorOverloading {
	// Overloads_OperatorCollision defines:
	// - One operator with both a "friendly" and operator version (Addition)
	// - One with just the "friendly" version
	// - One with just the operator versions
	Overloads_OperatorCollision * one = [[Overloads_OperatorCollision alloc] initWithVal:1];
	Overloads_OperatorCollision * two = [[Overloads_OperatorCollision alloc] initWithVal:2];
	XCTAssertTrue ([[Overloads_OperatorCollision add:one c2:two] value] == 3, "1 + 2");
	XCTAssertTrue ([[Overloads_OperatorCollision subtract:two c2:one] value] == 1, "2 - 1");
	XCTAssertTrue ([[Overloads_OperatorCollision multiply:two c2:two] value] == 4, "2 * 2");

	// Overloads_AllOperators defines just operator versions of all
	Overloads_AllOperators * oneAll = [[Overloads_AllOperators alloc] initWithVal:1];
	Overloads_AllOperators * twoAll = [[Overloads_AllOperators alloc] initWithVal:2];
	XCTAssertTrue ([[Overloads_AllOperators add:oneAll c2:twoAll] value] == (1 + 2), "1 + 2 All");
	XCTAssertTrue ([[Overloads_AllOperators subtract:oneAll c2:twoAll] value] == (1 - 2), "1 - 2 All");
	XCTAssertTrue ([[Overloads_AllOperators multiply:oneAll c2:twoAll] value] == (1 * 2), "1 * 2 All");
	XCTAssertTrue ([[Overloads_AllOperators divideAllOperators:twoAll allOperators:oneAll] value] == (2 / 1), "2 / 1 All");
	XCTAssertTrue ([[Overloads_AllOperators divideAllOperators:twoAll intValue:1] value] == (2 / 1), "2 / 1 All int");

	XCTAssertTrue ([[Overloads_AllOperators bitwiseAndAllOperators:oneAll allOperators:twoAll] value] == (1 & 2), "1 & 2 All");
	XCTAssertTrue ([[Overloads_AllOperators bitwiseAndAllOperators:oneAll intValue:2] value] == (1 & 2), "1 & 2 All int");

	XCTAssertTrue ([[Overloads_AllOperators bitwiseOr:oneAll c2:twoAll] value] == (1 | 2), "1 | 2 All");
	XCTAssertTrue ([[Overloads_AllOperators xor:oneAll c2:twoAll] value] == (1 ^ 2), "1 ^ 2 All");

	XCTAssertTrue ([[Overloads_AllOperators leftShift:oneAll a:2] value] == (1 << 2), "1 << 2 All");
	XCTAssertTrue ([[Overloads_AllOperators rightShift:oneAll a:2] value] == (1 >> 2), "1 >> 2 All");

	XCTAssertTrue ([[Overloads_AllOperators onesComplement:oneAll] value] == (~1), "!1 All");
	XCTAssertTrue ([[Overloads_AllOperators negate:oneAll] value] == (-1), "- 1 All");
	XCTAssertTrue ([[Overloads_AllOperators plus:oneAll] value] == (+1), "+ 1 All");

	XCTAssertTrue ([[Overloads_AllOperators decrement:oneAll] value] == (1 - 1), "1 - 1 All");
	XCTAssertTrue ([[Overloads_AllOperators increment:oneAll] value] == (1 + 1), "1 + 1 All");

	// Overloads_AllOperatorsWithFriendly defines both the operator and "friendly" version
	Overloads_AllOperatorsWithFriendly * oneFriend = [[Overloads_AllOperatorsWithFriendly alloc] initWithVal:1];
	Overloads_AllOperatorsWithFriendly * twoFriend = [[Overloads_AllOperatorsWithFriendly alloc] initWithVal:2];
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly add:oneFriend c2:twoFriend] value] == (1 + 2), "1 + 2 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly subtract:oneFriend c2:twoFriend] value] == (1 - 2), "1 - 2 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly multiply:oneFriend c2:twoFriend] value] == (1 * 2), "1 * 2 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly divide:twoFriend c2:oneFriend] value] == (2 / 1), "2 / 1 All");

	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly bitwiseAnd:oneFriend c2:twoFriend] value] == (1 & 2), "1 & 2 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly bitwiseOr:oneFriend c2:twoFriend] value] == (1 | 2), "1 | 2 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly xor:oneFriend c2:twoFriend] value] == (1 ^ 2), "1 ^ 2 All");

	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly leftShift:oneFriend a:2] value] == (1 << 2), "1 << 2 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly rightShift:oneFriend a:2] value] == (1 >> 2), "1 >> 2 All");

	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly onesComplement:oneFriend] value] == (~1), "!1 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly negate:oneFriend] value] == (-1), "- 1 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly plus:oneFriend] value] == (+1), "+ 1 All");

	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly decrement:oneFriend] value] == (1 - 1), "1 - 1 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly increment:oneFriend] value] == (1 + 1), "1 + 1 All");

	Overloads_EqualOverrides * equalOverrideTwo = [[Overloads_EqualOverrides alloc] initWithVal:2];
	Overloads_EqualOverrides * equalOverrideFour = [[Overloads_EqualOverrides alloc] initWithVal:4];

	XCTAssertTrue ([Overloads_EqualOverrides areEqual:equalOverrideFour b:equalOverrideTwo] == NO, "2 != 4 EqualOverride");
}

- (void)testShortParamters {
	ShortParameters_Class *testClass = [[ShortParameters_Class alloc] init];
	XCTAssertTrue([testClass noDuplicateTypesA:@"Foo" c:1.0 e:0.1f g:1 i:2 k:3 m:4 o:5 q:6 s:YES], "Short arguments, no duplicates");
	XCTAssert ([testClass twoIntX:1 y:2] == 3, "Two ints");
	XCTAssert ([testClass twoBoolS:YES t:YES] == YES, "Two bools");
	XCTAssert ([testClass twoCharQ:'c' r:'d'] == 199, "Two characters");
	XCTAssert ([testClass twoLongM:-3 n:15] == 12, "Two longs");
	XCTAssert ([testClass twoUintG:3 h:4] == 7, "Two uints");
	XCTAssert ([testClass twoFloatE:1.2f f:3.4f] - 4.6f < 0.000001, "Two floats");
	XCTAssert ([testClass twoShortI:1 j:4] == 5, "Two shorts");
	XCTAssert ([testClass twoUlongO:2 p:3] == 5, "Two ulong");
	XCTAssert ([testClass twoDoubleC:3.2 d:5.7] == 8.9, "Two doubles");
	XCTAssert ([[testClass twoStringA:@"Foo" b:@"Bar"] isEqualToString:@"FooBar"], "Two strings");
	XCTAssert ([testClass twoUshortK:5 l:8] == 13, "Two ushorts");
}

- (void)testNestedClasses {
	NestedClasses_ParentClass *parent = [[NestedClasses_ParentClass alloc] init];
	XCTAssert ([parent addNumbersX:3 y:5] == 8, "Parent class calling nested class method");
	XCTAssert ([parent sum] == 8, "Parent class retrieving nested class property");

	NestedClasses_ParentClass_NestedClass *nested = [[NestedClasses_ParentClass_NestedClass alloc] init];
	XCTAssert ([nested additionX:2 y:4] == 6, "Nested class method call");
}

- (void)testArraySupport {
	Arrays_Arr *testClass = [[Arrays_Arr alloc] init];

	NSArray<NSString *> *strArr = [testClass stringArrMethod];
	XCTAssertEqual ([strArr count], 3, @"strArr count");
	XCTAssertEqualObjects (@"Hola", strArr[0], @"strArr[0] Hola");
	XCTAssertEqualObjects (@"Hello", strArr[1], @"strArr[1] Hello");
	XCTAssertEqualObjects (@"Bonjour", strArr[2], @"strArr[2] Bonjour");

	NSArray<NSString *> *strArr2 = [testClass stringArrMethodStrArr:strArr];
	XCTAssertEqual ([strArr2 count], 3, @"strArr2 count");
	XCTAssertEqualObjects (@"Hola", strArr2[0], @"strArr2[0] Hola");
	XCTAssertEqualObjects (@"Hello", strArr2[1], @"strArr2[1] Hello");
	XCTAssertEqualObjects (@"Bonjour", strArr2[2], @"strArr2[2] Bonjour");

	NSArray<NSString *> *strRefArr = [testClass stringArrMethod];
	[testClass stringArrRefStrArr:&strRefArr];
	XCTAssertEqual ([strRefArr count], 3, @"strArr count");
	XCTAssertEqualObjects (@"Hola", strRefArr[2], @"strRefArr[2] Hola");
	XCTAssertEqualObjects (@"Hello", strRefArr[1], @"strRefArr[1] Hello");
	XCTAssertEqualObjects (@"Bonjour", strRefArr[0], @"strRefArr[0] Bonjour");

	NSArray<NSString *> *strRefNilArr = nil;
	[testClass stringArrRefStrArr:&strRefNilArr];
	XCTAssertNil(strRefNilArr, "strRefNilArr");

	NSArray<Arrays_ValueHolder *> *vhArr = [testClass valueHolderArrMethod];
	XCTAssertEqual ([vhArr count], 3, @"vhArr count");
	XCTAssertEqual (vhArr[0].intValue, 1, @"vhArr[0] 1");
	XCTAssertEqual (vhArr[1].intValue, 2, @"vhArr[1] 2");
	XCTAssertEqual (vhArr[2].intValue, 3, @"vhArr[2] 3");

	NSArray<Arrays_ValueHolder *> *vhArr2 = [testClass valueHolderArrMethodValhArr:vhArr];
	XCTAssertEqual ([vhArr2 count], 3, @"vhArr2 count");
	XCTAssertEqual (vhArr2[0].intValue, 1, @"vhArr2[0] 1");
	XCTAssertEqual (vhArr2[1].intValue, 2, @"vhArr2[1] 2");
	XCTAssertEqual (vhArr2[2].intValue, 3, @"vhArr2[2] 3");

	NSArray<Arrays_ValueHolder *> *vhRefArr = [testClass valueHolderArrMethod];
	[testClass valueHolderArrRefValueArr:&vhRefArr];
	XCTAssertEqual ([vhRefArr count], 3, @"vhArr count");
	XCTAssertEqual (vhRefArr[2].intValue, 1, @"vhRefArr[2] 1");
	XCTAssertEqual (vhRefArr[1].intValue, 2, @"vhRefArr[1] 2");
	XCTAssertEqual (vhRefArr[0].intValue, 3, @"vhRefArr[0] 3");

	NSArray<Arrays_ValueHolder *> *vhRefNilArr = nil;
	[testClass valueHolderArrRefValueArr:&vhRefNilArr];
	XCTAssertNil(vhRefNilArr, "vhRefNilArr");

	NSArray<NSNumber *> *boolArr = [testClass boolArrMethod];
	XCTAssertEqual ([boolArr count], 3, @"boolArr count");
	XCTAssertEqual (boolArr[0].boolValue, YES, @"boolArr[0] YES");
	XCTAssertEqual (boolArr[1].boolValue, NO, @"boolArr[1] NO");
	XCTAssertEqual (boolArr[2].boolValue, YES, @"boolArr[2] YES");

	NSArray<NSNumber *> *boolArr2 = [testClass boolArrMethodBoolArr:boolArr];
	XCTAssertEqual ([boolArr2 count], 3, @"boolArr2 count");
	XCTAssertEqual (boolArr2[0].boolValue, YES, @"boolArr2[0] YES");
	XCTAssertEqual (boolArr2[1].boolValue, NO, @"boolArr2[1] NO");
	XCTAssertEqual (boolArr2[2].boolValue, YES, @"boolArr2[2] YES");

	NSArray<NSNumber *> *charArr = [testClass charArrMethod];
	XCTAssertEqual ([charArr count], 3, @"charArr count");
	XCTAssertEqual (charArr[0].unsignedShortValue, 'a', @"charArr[0] a");
	XCTAssertEqual (charArr[1].unsignedShortValue, 'b', @"charArr[1] b");
	XCTAssertEqual (charArr[2].unsignedShortValue, '@', @"charArr[2] @");

	NSArray<NSNumber *> *charArr2 = [testClass charArrMethodCharArr:charArr];
	XCTAssertEqual ([charArr2 count], 3, @"charArr2 count");
	XCTAssertEqual (charArr2[0].unsignedShortValue, 'a', @"charArr2[0] a");
	XCTAssertEqual (charArr2[1].unsignedShortValue, 'b', @"charArr2[1] b");
	XCTAssertEqual (charArr2[2].unsignedShortValue, '@', @"charArr2[2] @");

	NSArray<NSNumber *> *doubleArr = [testClass doubleArrMethod];
	XCTAssertEqual ([doubleArr count], 3, @"doubleArr count");
	XCTAssertEqual (doubleArr[0].doubleValue, 1.5, @"doubleArr[0] 1.5");
	XCTAssertEqual (doubleArr[1].doubleValue, 5.1, @"doubleArr[1] 5.1");
	XCTAssertEqual (doubleArr[2].doubleValue, 3.1416, @"doubleArr[2] 3.1416");

	NSArray<NSNumber *> *doubleArr2 = [testClass doubleArrMethodDoubleArr:doubleArr];
	XCTAssertEqual ([doubleArr2 count], 3, @"doubleArr2 count");
	XCTAssertEqual (doubleArr2[0].doubleValue, 1.5, @"doubleArr2[0] 1.5");
	XCTAssertEqual (doubleArr2[1].doubleValue, 5.1, @"doubleArr2[1] 5.1");
	XCTAssertEqual (doubleArr2[2].doubleValue, 3.1416, @"doubleArr2[2] 3.1416");

	NSArray<NSNumber *> *floatArr = [testClass floatArrMethod];
	XCTAssertEqual ([floatArr count], 3, @"floatArr count");
	XCTAssertEqual (floatArr[0].floatValue, 1.5f, @"floatArr[0] 1.5");
	XCTAssertEqual (floatArr[1].floatValue, 5.1f, @"floatArr[1] 5.1");
	XCTAssertEqual (floatArr[2].floatValue, 3.1416f, @"floatArr[2] 3.1416");

	NSArray<NSNumber *> *floatArr2 = [testClass floatArrMethodFloatArr:floatArr];
	XCTAssertEqual ([floatArr2 count], 3, @"floatArr2 count");
	XCTAssertEqual (floatArr2[0].floatValue, 1.5f, @"floatArr2[0] 1.5");
	XCTAssertEqual (floatArr2[1].floatValue, 5.1f, @"floatArr2[1] 5.1");
	XCTAssertEqual (floatArr2[2].floatValue, 3.1416f, @"floatArr2[2] 3.1416");

	NSArray<NSNumber *> *sbyteArr = [testClass sbyteArrMethod];
	XCTAssertEqual ([sbyteArr count], 3, @"sbyteArr count");
	XCTAssertEqual (sbyteArr[0].charValue, 127, @"sbyteArr[0] 127");
	XCTAssertEqual (sbyteArr[1].charValue, -128, @"sbyteArr[1] -128");
	XCTAssertEqual (sbyteArr[2].charValue, 0, @"sbyteArr[2] 0");

	NSArray<NSNumber *> *sbyteArr2 = [testClass sbyteArrMethodSbyteArr:sbyteArr];
	XCTAssertEqual ([sbyteArr2 count], 3, @"sbyteArr2 count");
	XCTAssertEqual (sbyteArr2[0].charValue, 127, @"sbyteArr2[0] 127");
	XCTAssertEqual (sbyteArr2[1].charValue, -128, @"sbyteArr2[1] -128");
	XCTAssertEqual (sbyteArr2[2].charValue, 0, @"sbyteArr2[2] 0");

	NSArray<NSNumber *> *shortArr = [testClass shortArrMethod];
	XCTAssertEqual ([shortArr count], 3, @"shortArr count");
	XCTAssertEqual (shortArr[0].shortValue, SHRT_MAX, @"shortArr[0] SHRT_MAX");
	XCTAssertEqual (shortArr[1].shortValue, SHRT_MIN, @"shortArr[1] SHRT_MIN");
	XCTAssertEqual (shortArr[2].shortValue, 0, @"shortArr[2] 0");

	NSArray<NSNumber *> *shortArr2 = [testClass shortArrMethodShortArr:shortArr];
	XCTAssertEqual ([shortArr2 count], 3, @"shortArr2 count");
	XCTAssertEqual (shortArr2[0].shortValue, SHRT_MAX, @"shortArr2[0] SHRT_MAX");
	XCTAssertEqual (shortArr2[1].shortValue, SHRT_MIN, @"shortArr2[1] SHRT_MIN");
	XCTAssertEqual (shortArr2[2].shortValue, 0, @"shortArr2[2] 0");

	NSArray<NSNumber *> *intArr = [testClass intArrMethod];
	XCTAssertEqual ([intArr count], 3, @"intArr count");
	XCTAssertEqual (intArr[0].intValue, INT_MAX, @"intArr[0] INT_MAX");
	XCTAssertEqual (intArr[1].intValue, INT_MIN, @"intArr[1] INT_MIN");
	XCTAssertEqual (intArr[2].intValue, 0, @"intArr[2] 0");

	NSArray<NSNumber *> *intArr2 = [testClass intArrMethodIntArr:intArr];
	XCTAssertEqual ([intArr2 count], 3, @"intArr2 count");
	XCTAssertEqual (intArr2[0].intValue, INT_MAX, @"intArr2[0] INT_MAX");
	XCTAssertEqual (intArr2[1].intValue, INT_MIN, @"intArr2[1] INT_MIN");
	XCTAssertEqual (intArr2[2].intValue, 0, @"intArr2[2] 0");

	NSArray<NSNumber *> *longArr = [testClass longArrMethod];
	XCTAssertEqual ([longArr count], 3, @"longArr count");
	XCTAssertEqual (longArr[0].longLongValue, LONG_MAX, @"longArr[0] LONG_MAX");
	XCTAssertEqual (longArr[1].longLongValue, LONG_MIN, @"longArr[1] LONG_MIN");
	XCTAssertEqual (longArr[2].longLongValue, 0, @"longArr[2] 0");

	NSArray<NSNumber *> *longArr2 = [testClass longArrMethodLongArr:longArr];
	XCTAssertEqual ([longArr2 count], 3, @"longArr2 count");
	XCTAssertEqual (longArr2[0].longLongValue, LONG_MAX, @"longArr2[0] LONG_MAX");
	XCTAssertEqual (longArr2[1].longLongValue, LONG_MIN, @"longArr2[1] LONG_MIN");
	XCTAssertEqual (longArr2[2].longLongValue, 0, @"longArr2[2] 0");

	NSArray<NSNumber *> *longRefArr = [testClass longArrMethod];
	[testClass longArrRefLongArr:&longRefArr];
	XCTAssertEqual ([longRefArr count], 3, @"longRefArr count");
	XCTAssertEqual (longRefArr[2].longLongValue, LONG_MAX, @"longRefArr[2] LONG_MAX");
	XCTAssertEqual (longRefArr[1].longLongValue, LONG_MIN, @"longRefArr[1] LONG_MIN");
	XCTAssertEqual (longRefArr[0].longLongValue, 0, @"longRefArr[0] 0");

	NSArray<NSNumber *> *longRefNilArr = nil;
	[testClass longArrRefLongArr:&longRefNilArr];
	XCTAssertNil(longRefNilArr, "longRefNilArr");

	NSArray<NSNumber *> *ushortArr = [testClass ushortArrMethod];
	XCTAssertEqual ([ushortArr count], 3, @"ushortArr count");
	XCTAssertEqual (ushortArr[0].unsignedShortValue, USHRT_MAX, @"ushortArr[0] USHRT_MAX");
	XCTAssertEqual (ushortArr[1].unsignedShortValue, 0, @"ushortArr[1] 0");
	XCTAssertEqual (ushortArr[2].unsignedShortValue, 10, @"ushortArr[2] 10");

	NSArray<NSNumber *> *ushortArr2 = [testClass ushortArrMethodUshortArr:ushortArr];
	XCTAssertEqual ([ushortArr2 count], 3, @"ushortArr2 count");
	XCTAssertEqual (ushortArr2[0].unsignedShortValue, USHRT_MAX, @"ushortArr2[0] USHRT_MAX");
	XCTAssertEqual (ushortArr2[1].unsignedShortValue, 0, @"ushortArr2[1] 0");
	XCTAssertEqual (ushortArr2[2].unsignedShortValue, 10, @"ushortArr2[2] 10");

	NSArray<NSNumber *> *uintArr = [testClass uintArrMethod];
	XCTAssertEqual ([uintArr count], 3, @"uintArr count");
	XCTAssertEqual (uintArr[0].unsignedIntValue, UINT_MAX, @"uintArr[0] INT_MAX");
	XCTAssertEqual (uintArr[1].unsignedIntValue, 0, @"uintArr[1] 0");
	XCTAssertEqual (uintArr[2].unsignedIntValue, 15, @"uintArr[2] 15");

	NSArray<NSNumber *> *uintArr2 = [testClass uintArrMethodUintArr:uintArr];
	XCTAssertEqual ([uintArr2 count], 3, @"uintArr2 count");
	XCTAssertEqual (uintArr2[0].unsignedIntValue, UINT_MAX, @"uintArr2[0] INT_MAX");
	XCTAssertEqual (uintArr2[1].unsignedIntValue, 0, @"uintArr2[1] 0");
	XCTAssertEqual (uintArr2[2].unsignedIntValue, 15, @"uintArr2[2] 15");

	NSArray<NSNumber *> *ulongArr = [testClass ulongArrMethod];
	XCTAssertEqual ([ulongArr count], 3, @"longArr count");
	XCTAssertEqual (ulongArr[0].unsignedLongLongValue, ULONG_MAX, @"ulongArr[0] ULONG_MAX");
	XCTAssertEqual (ulongArr[1].unsignedLongLongValue, 0, @"ulongArr[1] 0");
	XCTAssertEqual (ulongArr[2].unsignedLongLongValue, 117, @"ulongArr[2] 117");

	NSArray<NSNumber *> *ulongArr2 = [testClass ulongArrMethodUlongArr:ulongArr];
	XCTAssertEqual ([ulongArr2 count], 3, @"ulongArr2 count");
	XCTAssertEqual (ulongArr2[0].unsignedLongLongValue, ULONG_MAX, @"ulongArr2[0] ULONG_MAX");
	XCTAssertEqual (ulongArr2[1].unsignedLongLongValue, 0, @"ulongArr2[1] 0");
	XCTAssertEqual (ulongArr2[2].unsignedLongLongValue, 117, @"ulongArr2[2] 117");

	NSData *data = [testClass byteArrMethod];
	char bytes[5] = {0x0, 0x01, 0x02, 0x04, 0x08};
	NSData *cData = [NSData dataWithBytes:bytes length:sizeof(bytes)];
	XCTAssertEqualObjects (cData, data, @"data");

	NSData *data2 = [testClass byteArrMethodByteArr:data];
	XCTAssertEqualObjects (cData, data2, @"data");

	NSData *dataRefArr = [testClass byteArrMethod];
	[testClass byteArrRefByteArr:&dataRefArr];
	char rbytes[5] = {0x08, 0x04, 0x02, 0x01, 0x0};
	NSData *crData = [NSData dataWithBytes:rbytes length:sizeof(rbytes)];
	XCTAssertEqualObjects (crData, dataRefArr, @"data");

	NSData *dataRefNilArr = nil;
	[testClass byteArrRefByteArr:&dataRefNilArr];
	XCTAssertNil(longRefNilArr, "longRefNilArr");

	NSArray<id<Interfaces_IMakeItUp>> *interArr = [testClass interfaceArrMethod];
	XCTAssertEqual ([interArr count], 3, @"interArr count");
	XCTAssertTrue ([interArr[0] conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "interArr[0] conformsToProtocol 1");
	XCTAssertTrue ([interArr[0] boolean], "interArr[0] true");
	XCTAssertFalse ([interArr[0] boolean], "interArr[0] false");
	XCTAssertEqualObjects (@"0", [interArr[0] convertIntValue:0], "interArr[0] 0");
	XCTAssertEqualObjects (@"1", [interArr[0] convertLongValue:1ll], "interArr[0] 1");
	XCTAssertTrue ([interArr[1] conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "interArr[1] conformsToProtocol 1");
	XCTAssertTrue ([interArr[1] boolean], "interArr[1] true");
	XCTAssertFalse ([interArr[1] boolean], "interArr[1] false");
	XCTAssertEqualObjects (@"0", [interArr[1] convertIntValue:0], "interArr[1] 0");
	XCTAssertEqualObjects (@"1", [interArr[1] convertLongValue:1ll], "interArr[1] 1");
	XCTAssertTrue ([interArr[2] conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "interArr[2] conformsToProtocol 1");
	XCTAssertTrue ([interArr[2] boolean], "interArr[2] true");
	XCTAssertFalse ([interArr[2] boolean], "interArr[2] false");
	XCTAssertEqualObjects (@"0", [interArr[2] convertIntValue:0], "interArr[2] 0");
	XCTAssertEqualObjects (@"1", [interArr[2] convertLongValue:1ll], "interArr[2] 1");

	NSArray<id<Interfaces_IMakeItUp>> *interArr2 = [testClass interfaceArrMethodInterArr:interArr];
	XCTAssertEqual ([interArr2 count], 3, @"interArr2 count");
	XCTAssertTrue ([interArr2[0] conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "interArr2[0] conformsToProtocol 1");
	XCTAssertTrue ([interArr2[0] boolean], "interArr2[0] true");
	XCTAssertFalse ([interArr2[0] boolean], "interArr2[0] false");
	XCTAssertEqualObjects (@"0", [interArr2[0] convertIntValue:0], "interArr2[0] 0");
	XCTAssertEqualObjects (@"1", [interArr2[0] convertLongValue:1ll], "interArr2[0] 1");
	XCTAssertTrue ([interArr2[1] conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "interArr2[1] conformsToProtocol 1");
	XCTAssertTrue ([interArr2[1] boolean], "interArr2[1] true");
	XCTAssertFalse ([interArr2[1] boolean], "interArr2[1] false");
	XCTAssertEqualObjects (@"0", [interArr2[1] convertIntValue:0], "interArr2[1] 0");
	XCTAssertEqualObjects (@"1", [interArr2[1] convertLongValue:1ll], "interArr2[1] 1");
	XCTAssertTrue ([interArr2[2] conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "interArr2[2] conformsToProtocol 1");
	XCTAssertTrue ([interArr2[2] boolean], "interArr2[2] true");
	XCTAssertFalse ([interArr2[2] boolean], "interArr2[2] false");
	XCTAssertEqualObjects (@"0", [interArr2[2] convertIntValue:0], "interArr2[2] 0");
	XCTAssertEqualObjects (@"1", [interArr2[2] convertLongValue:1ll], "interArr2[2] 1");

	NSArray<id<Interfaces_IMakeItUp>> *interRefArr = [testClass interfaceArrMethod];
	[testClass iMakeItUpArrRefInterArr:&interRefArr];
	XCTAssertEqual ([interRefArr count], 1, @"interRefArr count");
	XCTAssertTrue ([interRefArr[0] conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "interRefArr[0] conformsToProtocol 1");
	XCTAssertTrue ([interRefArr[0] boolean], "interRefArr[0] true");
	XCTAssertFalse ([interRefArr[0] boolean], "interRefArr[0] false");
	XCTAssertEqualObjects (@"0", [interRefArr[0] convertIntValue:0], "interRefArr[0] 0");
	XCTAssertEqualObjects (@"1", [interRefArr[0] convertLongValue:1ll], "interRefArr[0] 1");

	NSArray<id<Interfaces_IMakeItUp>> *interRefNilArr = nil;
	[testClass iMakeItUpArrRefInterArr:&interRefNilArr];
	XCTAssertNil(interRefNilArr, "interRefNilArr");

	NSArray<NSString *> *nullArr = [testClass getNullMethod];
	XCTAssertNil (nullArr, @"nullArr");

	NSArray<NSString *> *nullArrProp = testClass.getNull;
	XCTAssertNil (nullArrProp, @"nullArrProp");

	NSArray<NSString *> *strArrProp = testClass.stringArr;
	XCTAssertEqual ([strArrProp count], 3, @"strArrProp count");
	XCTAssertEqualObjects (@"Hola", strArrProp[0], @"strArrProp[0] Hola");
	XCTAssertEqualObjects (@"Hello", strArrProp[1], @"strArrProp[1] Hello");
	XCTAssertEqualObjects (@"Bonjour", strArrProp[2], @"strArrProp[2] Bonjour");

	NSArray<NSNumber *> *intArrProp = testClass.intArr;
	XCTAssertEqual ([intArrProp count], 3, @"intArrProp count");
	XCTAssertEqual (intArrProp[0].intValue, INT_MAX, @"intArrProp[0] INT_MAX");
	XCTAssertEqual (intArrProp[1].intValue, INT_MIN, @"intArrProp[1] INT_MIN");
	XCTAssertEqual (intArrProp[2].intValue, 0, @"intArrProp[2] 0");

	NSArray<Arrays_ValueHolder *> *vhArrProp = testClass.valueHolderArr;
	XCTAssertEqual ([vhArrProp count], 3, @"vhArrProp count");
	XCTAssertEqual (vhArrProp[0].intValue, 1, @"vhArrProp[0] 1");
	XCTAssertEqual (vhArrProp[1].intValue, 2, @"vhArrProp[1] 2");
	XCTAssertEqual (vhArrProp[2].intValue, 3, @"vhArrProp[2] 3");

	NSData *dataProp = testClass.byteArr;
	XCTAssertEqualObjects (cData, dataProp, @"dataProp");

	NSArray<id<Interfaces_IMakeItUp>> *interProp = testClass.interfaceArr;
	XCTAssertEqual ([interProp count], 3, @"interProp count");
	XCTAssertTrue ([interProp[0] conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "interProp[0] conformsToProtocol 1");
	XCTAssertTrue ([interProp[0] boolean], "interProp[0] true");
	XCTAssertFalse ([interProp[0] boolean], "interProp[0] false");
	XCTAssertEqualObjects (@"0", [interProp[0] convertIntValue:0], "interProp[0] 0");
	XCTAssertEqualObjects (@"1", [interProp[0] convertLongValue:1ll], "interProp[0] 1");
	XCTAssertTrue ([interProp[1] conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "interProp[1] conformsToProtocol 1");
	XCTAssertTrue ([interProp[1] boolean], "interProp[1] true");
	XCTAssertFalse ([interProp[1] boolean], "interProp[1] false");
	XCTAssertEqualObjects (@"0", [interProp[1] convertIntValue:0], "interProp[1] 0");
	XCTAssertEqualObjects (@"1", [interProp[1] convertLongValue:1ll], "interProp[1] 1");
	XCTAssertTrue ([interProp[2] conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "interProp[2] conformsToProtocol 1");
	XCTAssertTrue ([interProp[2] boolean], "interProp[2] true");
	XCTAssertFalse ([interProp[2] boolean], "interProp[2] false");
	XCTAssertEqualObjects (@"0", [interProp[2] convertIntValue:0], "interProp[2] 0");
	XCTAssertEqualObjects (@"1", [interProp[2] convertLongValue:1ll], "interProp[2] 1");

	NSArray<NSString *> *strNullArr = [testClass stringNullArrMethod];
	XCTAssertEqual ([strNullArr count], 3, @"strNullArr count");
	XCTAssertEqualObjects (@"Hola", strNullArr[0], @"strNullArr[0] Hola");
	XCTAssertEqualObjects ([NSNull null], strNullArr[1], @"strNullArr[1] nil");
	XCTAssertEqualObjects (@"Bonjour", strNullArr[2], @"strNullArr[2] Bonjour");

	NSArray<NSString *> *strNullArr2 = [testClass stringArrMethodStrArr:strNullArr];
	XCTAssertEqual ([strNullArr2 count], 3, @"strNullArr2 count");
	XCTAssertEqualObjects (@"Hola", strNullArr2[0], @"strNullArr2[0] Hola");
	XCTAssertEqualObjects (strNullArr2[1], [NSNull null], @"strNullArr2[1] NSNull");
	XCTAssertEqualObjects (@"Bonjour", strNullArr2[2], @"strNullArr2[2] Bonjour");

	NSArray<Arrays_ValueHolder *> *vhNullArr = [testClass valueHolderNullArrMethod];
	XCTAssertEqual ([vhNullArr count], 3, @"vhNullArr count");
	XCTAssertEqual (vhNullArr[0].intValue, 1, @"vhNullArr[0] 1");
	XCTAssertEqualObjects (vhNullArr[1], [NSNull null], @"vhNullArr[1] NSNull");
	XCTAssertEqual (vhNullArr[2].intValue, 3, @"vhNullArr[2] 3");

	NSArray<Arrays_ValueHolder *> *vhNullArr2 = [testClass valueHolderArrMethodValhArr:vhNullArr];
	XCTAssertEqual ([vhNullArr2 count], 3, @"vhNullArr2 count");
	XCTAssertEqual (vhNullArr2[0].intValue, 1, @"vhNullArr2[0] 1");
	XCTAssertEqualObjects (vhNullArr2[1], [NSNull null], @"vhNullArr2[1] NSNull");
	XCTAssertEqual (vhNullArr2[2].intValue, 3, @"vhNullArr2[2] 3");

	NSArray<id<Interfaces_IMakeItUp>> *interNullArr = [testClass interfaceNullArrMethod];
	XCTAssertEqual ([interNullArr count], 3, @"interNullArr count");
	XCTAssertTrue ([interNullArr[0] conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "interNullArr[0] conformsToProtocol 1");
	XCTAssertTrue ([interNullArr[0] boolean], "interNullArr[0] true");
	XCTAssertFalse ([interNullArr[0] boolean], "interNullArr[0] false");
	XCTAssertEqualObjects (@"0", [interNullArr[0] convertIntValue:0], "interNullArr[0] 0");
	XCTAssertEqualObjects (@"1", [interNullArr[0] convertLongValue:1ll], "interNullArr[0] 1");
	XCTAssertEqualObjects (interNullArr[1], [NSNull null], @"interNullArr[1] NSNull");
	XCTAssertTrue ([interNullArr[2] conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "interNullArr[2] conformsToProtocol 1");
	XCTAssertTrue ([interNullArr[2] boolean], "interNullArr[2] true");
	XCTAssertFalse ([interNullArr[2] boolean], "interNullArr[2] false");
	XCTAssertEqualObjects (@"0", [interNullArr[2] convertIntValue:0], "interNullArr[2] 0");
	XCTAssertEqualObjects (@"1", [interNullArr[2] convertLongValue:1ll], "interNullArr[2] 1");

	NSArray<id<Interfaces_IMakeItUp>> *interNullArr2 = [testClass interfaceArrMethodInterArr:interNullArr];
	XCTAssertEqual ([interNullArr2 count], 3, @"interNullArr2 count");
	XCTAssertTrue ([interNullArr2[0] conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "interNullArr2[0] conformsToProtocol 1");
	XCTAssertTrue ([interNullArr2[0] boolean], "interNullArr2[0] true");
	XCTAssertFalse ([interNullArr2[0] boolean], "interNullArr2[0] false");
	XCTAssertEqualObjects (@"0", [interNullArr2[0] convertIntValue:0], "interNullArr2[0] 0");
	XCTAssertEqualObjects (@"1", [interNullArr2[0] convertLongValue:1ll], "interNullArr2[0] 1");
	XCTAssertEqualObjects (interNullArr2[1], [NSNull null], @"interNullArr2[1] NSNull");
	XCTAssertTrue ([interNullArr2[2] conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "interNullArr2[2] conformsToProtocol 1");
	XCTAssertTrue ([interNullArr2[2] boolean], "interNullArr2[2] true");
	XCTAssertFalse ([interNullArr2[2] boolean], "interNullArr2[2] false");
	XCTAssertEqualObjects (@"0", [interNullArr2[2] convertIntValue:0], "interNullArr2[2] 0");
	XCTAssertEqualObjects (@"1", [interNullArr2[2] convertLongValue:1ll], "interNullArr2[2] 1");
}

- (void)testTimeSpan {
	// because we have an API that expose TimeSpan we generate the type from mscorlib.dll (because NSTimeInterval is not a very good alternative)
	System_TimeSpan *ts = [[System_TimeSpan alloc] initWithDays:1 hours:2 minutes:3 seconds:4 milliseconds:5];
	XCTAssertTrue ([ts days] == 1, "days");
	XCTAssertTrue ([ts hours] == 2, "hours");
	XCTAssertTrue ([ts minutes] == 3, "minutes");
	XCTAssertTrue ([ts seconds] == 4, "seconds");
	XCTAssertTrue ([ts milliseconds] == 5, "milliseconds");

	XCTAssertTrue ([ts ticks] == 937840050000ll, "ticks");
}

- (void)testIFormatProvider {
	id<System_IFormatProvider> enUS = [Interfaces_ExposeIFormatProvider getCultureName:@"en-US"];
	XCTAssertNotNil (enUS, "en-US");
	NSString *s1 = [Interfaces_ExposeIFormatProvider formatValue:1.2 provider:enUS];
	XCTAssertEqualObjects (@"1.2", s1, "1.2");

	id<System_IFormatProvider> frCA = [Interfaces_ExposeIFormatProvider getCultureName:@"fr-CA"];
	XCTAssertNotNil (frCA, "fr-CA");
	NSString *s2 = [Interfaces_ExposeIFormatProvider formatValue:1.2 provider:frCA];
	XCTAssertEqualObjects (@"1,2", s2, "1,2");

	System_TimeSpan *ts = [[System_TimeSpan alloc] initWithTicks:937840050000ll];
	NSString *s3 = [ts toStringFormat:@"c" formatProvider:nil];
	NSString *s4 = [ts toStringFormat:@"c"];
	XCTAssertEqualObjects (s3, @"1.02:03:04.0050000", "toStringFormat");
	XCTAssertEqualObjects (s3, s4, "toStringFormat:formatprovider");
}

-(void)testOtherThreads {
	// Test a variety of things, constructors, static methods, instance methods
	// We first run using GCD, then manually creating new threads.

	dispatch_sync (dispatch_get_global_queue (QOS_CLASS_USER_INITIATED, 0), ^{
		[self testExceptions];
	});
	dispatch_sync (dispatch_get_global_queue (QOS_CLASS_USER_INITIATED, 0), ^{
		[self testMethods];
	});
	dispatch_sync (dispatch_get_global_queue (QOS_CLASS_USER_INITIATED, 0), ^{
		[self testProperties];
	});

	NSThread *thread;
	thread = [[NSThread alloc] initWithTarget: self selector:@selector(testExceptions) object:nil];
	[thread start];
	while ([thread isFinished] == NO) {
		usleep (1000); // there's no [NSThread join]...
	}

	thread = [[NSThread alloc] initWithTarget: self selector:@selector(testMethods) object:nil];
	[thread start];
	while ([thread isFinished] == NO) {
		usleep (1000); // there's no [NSThread join]...
	}

	thread = [[NSThread alloc] initWithTarget: self selector:@selector(testProperties) object:nil];
	[thread start];
	while ([thread isFinished] == NO) {
		usleep (1000); // there's no [NSThread join]...
	}
}

- (void)testRestrictedNames {
	Duplicates_WithRestrictedNamed * d = [[Duplicates_WithRestrictedNamed alloc] init];
	Class c = [d class];
	XCTAssertNotEqual(42, [d hash], "Must not call instance Hash ()");
}

#pragma clang diagnostic pop

@end
