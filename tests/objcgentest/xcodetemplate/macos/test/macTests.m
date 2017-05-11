#if defined (XAMARIN_MAC)
#import <XCTest/XCTest.h>

#if defined (XAMARIN_MAC_MODERN)
#include "managed-macos-modern/managed-macos-modern.h"
#elif defined (XAMARIN_MAC_FULL)
#include "managed-macos-full/managed-macos-full.h"
#elif defined (XAMARIN_MAC_SYSTEM)
#include "managed-macos-system/managed-macos-system.h"
#else
#include "managed/managed.h"
#endif

@interface macTests : XCTestCase

@end

@implementation macTests

- (void)testCustomNSObject {
    XCTAssertEqual (7 + 9, [CustomUI_MyObject addStatic: 7 to: 9], "7+9 static");
    CustomUI_MyObject *myobj = [[CustomUI_MyObject alloc] init];
    XCTAssertEqual (3 + 5, [myobj addInstance: 3 to: 5], "3+5 instance");
}


@end
#endif // defined (XAMARIN_MAC)
