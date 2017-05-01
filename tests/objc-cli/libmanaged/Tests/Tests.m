#import <XCTest/XCTest.h>
#if defined (TEST_FRAMEWORK)
#include "managed-ios/managed-ios.h"
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
	
	XCTAssert ([Properties_Query universalAnswer] == 42, "static property getter only");
	
	Properties_Query* query = [[Properties_Query alloc] init];
	XCTAssertTrue ([query isGood], "instance property getter only 1");
	XCTAssertFalse ([query isBad], "instance property getter only 2");
	XCTAssert ([query answer] == 42, "instance property getter");
	query.answer = 911;
	XCTAssert ([query answer] == 911, "instance property setter check");

	XCTAssertFalse ([query isSecret], "instance property getter only 3");
	// setter only property turned into method, so different syntax
	[query setSecret: 1];
	XCTAssertTrue ([query isSecret], "instance property getter only 4");
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

	XCTAssert ([Structs_Point equality:p1 right:p1], "p1 == p1");
	XCTAssert ([Structs_Point equality:p2 right:p2], "p2 == p2");
	XCTAssert ([Structs_Point inequality:p1 right:p2], "p1 != p2");

	Structs_Point* p3 = [Structs_Point addition:p1 right:p2];
	XCTAssert ([p3 x] == 3.0f, "x 3");
	XCTAssert ([p3 y] == -3.0f, "y 3");

	Structs_Point* p4 = [Structs_Point subtraction:p3 right:p2];
	XCTAssert ([Structs_Point equality:p4 right:p1], "p4 == p1");

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
	XCTAssertNil ([empty class], "empty / class uninitialized");

	Fields_Struct *struct1 = [[Fields_Struct alloc] initWithEnabled:true];
	XCTAssertTrue (struct1.boolean, "init / boolean / true");
	struct1.boolean = false;
	XCTAssertFalse (struct1.boolean, "init / boolean / set 1");

	XCTAssertNotNil (struct1.class, "init / class initialized 1");
	XCTAssertFalse (struct1.class.boolean, "init / class / boolean / default");
	struct1.class = nil;
	XCTAssertNil (struct1.class, "init / class set 1");
	struct1.class = [[Fields_Class alloc] initWithEnabled:true];
	XCTAssertTrue (struct1.class.boolean, "init / class / boolean / true");

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
}

- (void) testObjectIndexedSubscripting {
    Subscripts_BoolCollection *boolCollection = [[Subscripts_BoolCollection alloc] init];
    XCTAssert ([boolCollection count] == 0, "count 0");
    [boolCollection addItem:YES];
    XCTAssert ([boolCollection count] == 1, "count 1");
    XCTAssert ([boolCollection [0] isEqual:@YES], "get 0");
    boolCollection[0] = NO;
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
    XCTAssert ([m doItInt32:0] == 42, "doIt 2");
    XCTAssert ([m doItString:@""] == 42, "doIt 3");
    XCTAssert ([m doItI:0 j:1] == 84, "doIt 4");
    XCTAssert ([m findName:@"name"] == YES, "doIt 5");
    XCTAssert ([m findFirstName:@"name" lastname:@"last"] == YES, "doIt 6");
    
    Properties_DuplicateIndexedProperties * p = [[Properties_DuplicateIndexedProperties alloc] init];
    XCTAssert ([p getItemInt32:0] == 42, "getItemInt32");
    XCTAssert ([p getItemString:@""] == 42, "getItemString");
    
    Constructors_Duplicates * c = [[Constructors_Duplicates alloc] initWithByte:1 byte:2 byte:3 byte:4];
    XCTAssertNotNil (c, "c");
    Constructors_Duplicates * c2 = [[Constructors_Duplicates alloc] initWithByte:1 int16:2 int32:3 int64:4];
    XCTAssertNotNil (c2, "c2");
    Constructors_Duplicates * c3 = [[Constructors_Duplicates alloc] initWithInt32:1 int32:2 int32:3 int32:4];
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

- (void)testProtocols {
	id<Interfaces_IMakeItUp> m = [Interfaces_Supplier create];
	XCTAssertTrue ([m conformsToProtocol:@protocol(Interfaces_IMakeItUp)], "conformsToProtocol 1");
	XCTAssertTrue ([m boolean], "true");
	XCTAssertFalse ([m boolean], "false");

	XCTAssertEqualObjects (@"0", [m convertInt32:0], "0");
	XCTAssertEqualObjects (@"1", [m convertInt64:1ll], "1");

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
	XCTAssertTrue ([[Overloads_OperatorCollision addC1:one c2:two] value] == 3, "1 + 2");
	XCTAssertTrue ([[Overloads_OperatorCollision subtraction:two c2:one] value] == 1, "2 - 1");
	XCTAssertTrue ([[Overloads_OperatorCollision multiplyC1:two c2:two] value] == 4, "2 * 2");

	// Overloads_AllOperators defines just operator versions of all
	Overloads_AllOperators * oneAll = [[Overloads_AllOperators alloc] initWithVal:1];
	Overloads_AllOperators * twoAll = [[Overloads_AllOperators alloc] initWithVal:2];
	XCTAssertTrue ([[Overloads_AllOperators addition:oneAll c2:twoAll] value] == (1 + 2), "1 + 2 All");
	XCTAssertTrue ([[Overloads_AllOperators subtraction:oneAll c2:twoAll] value] == (1 - 2), "1 - 2 All");
	XCTAssertTrue ([[Overloads_AllOperators multiply:oneAll c2:twoAll] value] == (1 * 2), "1 * 2 All");
	XCTAssertTrue ([[Overloads_AllOperators division:twoAll alloperators:oneAll] value] == (2 / 1), "2 / 1 All");
	XCTAssertTrue ([[Overloads_AllOperators division:twoAll int32:1] value] == (2 / 1), "2 / 1 All int");

	XCTAssertTrue ([[Overloads_AllOperators bitwiseAnd:oneAll alloperators:twoAll] value] == (1 & 2), "1 & 2 All");
	XCTAssertTrue ([[Overloads_AllOperators bitwiseAnd:oneAll int32:2] value] == (1 & 2), "1 & 2 All int");

	XCTAssertTrue ([[Overloads_AllOperators bitwiseOr:oneAll c2:twoAll] value] == (1 | 2), "1 | 2 All");
	XCTAssertTrue ([[Overloads_AllOperators exclusiveOr:oneAll c2:twoAll] value] == (1 ^ 2), "1 ^ 2 All");

	XCTAssertTrue ([[Overloads_AllOperators leftShift:oneAll a:2] value] == (1 << 2), "1 << 2 All");
	XCTAssertTrue ([[Overloads_AllOperators rightShift:oneAll a:2] value] == (1 >> 2), "1 >> 2 All");

	XCTAssertTrue ([[Overloads_AllOperators onesComplement:oneAll] value] == (~1), "!1 All");
	XCTAssertTrue ([[Overloads_AllOperators unaryNegation:oneAll] value] == (-1), "- 1 All");
	XCTAssertTrue ([[Overloads_AllOperators unaryPlus:oneAll] value] == (+1), "+ 1 All");

	XCTAssertTrue ([[Overloads_AllOperators decrement:oneAll] value] == (1 - 1), "1 - 1 All");
	XCTAssertTrue ([[Overloads_AllOperators increment:oneAll] value] == (1 + 1), "1 + 1 All");

	// Overloads_AllOperatorsWithFriendly defines both the operator and "friendly" version
	Overloads_AllOperatorsWithFriendly * oneFriend = [[Overloads_AllOperatorsWithFriendly alloc] initWithVal:1];
	Overloads_AllOperatorsWithFriendly * twoFriend = [[Overloads_AllOperatorsWithFriendly alloc] initWithVal:2];
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly addC1:oneFriend c2:twoFriend] value] == (1 + 2), "1 + 2 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly subtractC1:oneFriend c2:twoFriend] value] == (1 - 2), "1 - 2 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly multiplyC1:oneFriend c2:twoFriend] value] == (1 * 2), "1 * 2 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly divideC1:twoFriend c2:oneFriend] value] == (2 / 1), "2 / 1 All");

	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly bitwiseAndC1:oneFriend c2:twoFriend] value] == (1 & 2), "1 & 2 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly bitwiseOrC1:oneFriend c2:twoFriend] value] == (1 | 2), "1 | 2 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly xorC1:oneFriend c2:twoFriend] value] == (1 ^ 2), "1 ^ 2 All");

	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly leftShiftC1:oneFriend a:2] value] == (1 << 2), "1 << 2 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly rightShiftC1:oneFriend a:2] value] == (1 >> 2), "1 >> 2 All");

	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly onesComplementC1:oneFriend] value] == (~1), "!1 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly negateC1:oneFriend] value] == (-1), "- 1 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly plusC1:oneFriend] value] == (+1), "+ 1 All");

	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly decrementC1:oneFriend] value] == (1 - 1), "1 - 1 All");
	XCTAssertTrue ([[Overloads_AllOperatorsWithFriendly incrementC1:oneFriend] value] == (1 + 1), "1 + 1 All");
}

#pragma clang diagnostic pop

@end
