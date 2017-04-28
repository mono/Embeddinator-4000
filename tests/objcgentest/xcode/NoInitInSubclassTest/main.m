//
//  main.m
//  NoInitInSubclassTest
//
//  Created by Alex Soto on 4/21/17.
//  Copyright © 2017 Xamarin. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "bindings.h"

int main(int argc, const char * argv[]) {
    @autoreleasepool {
        // initWithId must be unavailable
        ConstructorsLib_Child * child = [[ConstructorsLib_Child alloc] initWithId:1];
    }
    return 0;
}
