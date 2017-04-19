//
//  ViewController.h
//  theweather
//
//  Created by Alex Soto on 4/13/17.
//  Copyright © 2017 Xamarin Inc. All rights reserved.
//

#import <Cocoa/Cocoa.h>
#import "bindings.h"

@interface ViewController : NSViewController

@property (weak) IBOutlet NSTextField *cityField;
@property (weak) IBOutlet NSTextField *stateField;
@property (weak) IBOutlet NSTextField *tempLabel;
@property (weak) IBOutlet NSTextField *descriptionLabel;
- (IBAction)getWeather:(NSButton *)sender;

@end

