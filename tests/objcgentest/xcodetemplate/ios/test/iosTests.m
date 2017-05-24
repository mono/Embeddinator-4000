#import <XCTest/XCTest.h>
#include "managed-ios/managed-ios.h"

@interface iosTests : XCTestCase

@end

@implementation iosTests

- (void)testCustomNSObject {
    XCTAssertEqual (7 + 9, [CustomUI_MyObject addStatic: 7 to: 9], "7+9 static");
    CustomUI_MyObject *myobj = [[CustomUI_MyObject alloc] init];
    XCTAssertEqual (3 + 5, [myobj addInstance: 3 to: 5], "3+5 instance");
}


@end
