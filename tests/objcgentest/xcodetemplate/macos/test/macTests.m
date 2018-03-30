#if defined (XAMARIN_MAC)
#import <XCTest/XCTest.h>

#if defined (XAMARIN_MAC_MODERN)
#include "managed-macos-modern/managed-macos-modern.h"
#elif defined (XAMARIN_MAC_FULL)
#include "managed-macos-full/managed-macos-full.h"
#elif defined (XAMARIN_MAC_SYSTEM)
#include "managed-macos-system/managed-macos-system.h"
#elif defined (XAMARIN_FSHARP)
#include "fsharp-macos/fsharp-macos.h"
#else
#include "managed/managed.h"
#endif

@interface macTests : XCTestCase

@end

@implementation macTests

#if !defined XAMARIN_FSHARP

- (void)testCustomNSObject {
    XCTAssertEqual (7 + 9, [CustomUI_MyObject addStatic: 7 to: 9], "7+9 static");
    CustomUI_MyObject *myobj = [[CustomUI_MyObject alloc] init];
    XCTAssertEqual (3 + 5, [myobj addInstance: 3 to: 5], "3+5 instance");
}

#else

- (void)testfsharpTypes {
    managed_FSharp_UserRecord * userRecord = [[managed_FSharp_UserRecord alloc] initWithUserDescription:@"Test"];
    XCTAssertEqualObjects(@"Test", [userRecord userDescription]);
    managed_FSharp_UserRecord * defaultUserRecord = [managed_FSharp getDefaultUserRecord];
    XCTAssertEqualObjects(@"Cherry", [defaultUserRecord userDescription]);

    NSArray<managed_FSharp_UserRecord *> * userRecordArray = [managed_FSharp_ArrayTest getDefaultUserRecordArrayCount:10];
    XCTAssertEqual (10, [userRecordArray count]);
    for (managed_FSharp_UserRecord * entry in userRecordArray)
        XCTAssertEqualObjects(@"Cherry", [entry userDescription]);
    
    // https://github.com/mono/Embeddinator-4000/issues/630
    //managed_FSharp_UserRecord * resultUserRecord = [managed_FSharp useUserRecordUserRecord:userRecord];
    //XCTAssertEqualObjects(@"Test", [resultUserRecord userDescription]);
    
    NSArray<managed_FSharp_UserStruct *> * userStructArray = [managed_FSharp_ArrayTest getDefaultUserStructArrayCount:10];
    XCTAssertEqual (10, [userRecordArray count]);
    for (managed_FSharp_UserStruct * entry in userStructArray)
        XCTAssertEqualObjects(@"Fun!", [entry userDefinition]);

    managed_FSharp_UserStruct * userStruct = [managed_FSharp getDefaultUserStruct];
    XCTAssertEqualObjects(@"Fun!", [userStruct userDefinition]);

    // https://github.com/mono/Embeddinator-4000/issues/630
    //managed_FSharp_UserStruct * resultUserStruct = [managed_FSharp useUserStructUserStruct:userStruct];
    //XCTAssertEqualObjects(@"Fun!", [resultUserStruct userDefinition]);
}

- (void) testfsharpModules {
    XCTAssertEqualObjects (@"Hello from a nested F# module", [managed_FSharp_NestedModuleTest nestedConstant]);
    XCTAssertEqualObjects (@"Hello from a nested F# module", [managed_FSharp_NestedModuleTest nestedFunction]);
}

#endif

@end
#endif // defined (XAMARIN_MAC)
