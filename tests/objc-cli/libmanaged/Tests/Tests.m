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
	
/*	XCTAssert ([Properties_Query universalAnswer] == 42, "static property getter only");
	
	Properties_Query* query = [[Properties_Query alloc] init];
	XCTAssertTrue ([query isGood], "instance property getter only 1");
	XCTAssertFalse ([query isBad], "instance property getter only 2");
	XCTAssert ([query answer] == 42, "instance property getter");
	query.answer = 911;
	XCTAssert ([query answer] == 911, "instance property setter check");*/
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

@end
