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
}

- (void)testNamespaces {
	id nonamespace = [[ClassWithoutNamespace alloc] init];
	XCTAssertTrue ([[nonamespace description] containsString:@"<ClassWithoutNamespace:"], "nonamespace");
	
	id singlenamespace = [[First_ClassWithSingleNamespace alloc] init];
	XCTAssertTrue ([[singlenamespace description] containsString:@"<First_ClassWithSingleNamespace:"], "singlenamespace");
	
	id nestednamespaces = [[First_Second_ClassWithNestedNamespace alloc] init];
	XCTAssertTrue ([[nestednamespaces description] containsString:@"<First_Second_ClassWithNestedNamespace:"], "nestednamespaces");
}

- (void)testExceptions {
	// .ctor that throws
	id throwers = [[Exceptions_Throwers alloc] init];
	XCTAssertNil (throwers, "Exceptions_Throwers init");
	
	// .cctor that throw - can't be called directly but it makes the type unusable
	id static_thrower = [[Exceptions_ThrowInStaticCtor alloc] init];
	XCTAssertNil (static_thrower, "Exceptions_ThrowInStaticCtor init");
}

- (void)testConstructors {
	id unique_default_init = [[Constructors_Unique alloc] init];
	XCTAssert ([unique_default_init id] == 1, "default id");

	id unique_init_id = [[Constructors_Unique alloc] initWithId:911];
	XCTAssert ([unique_init_id id] == 911, "id");
	
	Constructors_Implicit* implicit = [[Constructors_Implicit alloc] init];
	XCTAssertEqualObjects (@"OK", [implicit testResult], "implicit");
	
	Constructors_AllTypeCode* all1 = [[Constructors_AllTypeCode alloc] initWithB1:true C2:USHRT_MAX S:@"Mono"];
	XCTAssertTrue ([all1 testResult], "all1");

	Constructors_AllTypeCode* all2 = [[Constructors_AllTypeCode alloc] initWithI8:SCHAR_MAX I16:SHRT_MAX I32:INT_MAX I64:LONG_MAX];
	XCTAssertTrue ([all2 testResult], "all2");

	Constructors_AllTypeCode* all3 = [[Constructors_AllTypeCode alloc] initWithU8:UCHAR_MAX U16:USHRT_MAX U32:UINT_MAX U64:ULONG_MAX];
	XCTAssertTrue ([all3 testResult], "all3");

	Constructors_AllTypeCode* all4 = [[Constructors_AllTypeCode alloc] initWithF32:FLT_MAX F64:DBL_MAX];
	XCTAssertTrue ([all4 testResult], "all4");
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
