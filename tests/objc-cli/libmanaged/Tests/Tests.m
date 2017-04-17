#import <XCTest/XCTest.h>
#include "bindings.h"
#include "mono_embeddinator.h"

@interface Tests : XCTestCase

@end

@implementation Tests

+ (void)setUp {
	[super setUp];
	NSBundle *bundle = [NSBundle bundleForClass:[self class]];
	NSString *path = [bundle pathForResource:@"managed" ofType:@"dll"];
	mono_embeddinator_set_assembly_path ([path UTF8String]);
}

- (void)setUp {
	[super setUp];
}

- (void)tearDown {
	[super tearDown];
}

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
	
	[collection removeItem:item]; // not there
	XCTAssert ([collection count] == 1, "count 3");

	[collection removeItem:item2];
	XCTAssert ([collection count] == 0, "count 4");
}

- (void) testStructs {
	id p1 = [[Structs_Point alloc] initWithX:1.0f y:-1.0f];
	XCTAssert ([p1 x] == 1.0f, "x 1");
	XCTAssert ([p1 y] == -1.0f, "y 1");

	id p2 = [[Structs_Point alloc] initWithX:2.0f y:-2.0f];
	XCTAssert ([p2 x] == 2.0f, "x 2");
	XCTAssert ([p2 y] == -2.0f, "y 2");

	XCTAssert ([Structs_Point equality:p1 right:p1], "p1 == p1");
	XCTAssert ([Structs_Point equality:p2 right:p2], "p2 == p2");
	XCTAssert ([Structs_Point inequality:p1 right:p2], "p1 != p2");

	id p3 = [Structs_Point addition:p1 right:p2];
	XCTAssert ([p3 x] == 3.0f, "x 3");
	XCTAssert ([p3 y] == -3.0f, "y 3");

	id p4 = [Structs_Point subtraction:p3 right:p2];
	XCTAssert ([Structs_Point equality:p4 right:p1], "p4 == p1");

	id z = [Structs_Point zero];
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

@end
