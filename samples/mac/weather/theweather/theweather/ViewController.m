//
//  ViewController.m
//  theweather
//
//  Created by Alex Soto on 4/13/17.
//  Copyright © 2017 Xamarin Inc. All rights reserved.
//

#import "ViewController.h"

@implementation ViewController

- (void)viewDidLoad {
    [super viewDidLoad];

    // Do any additional setup after loading the view.
}


- (void)setRepresentedObject:(id)representedObject {
    [super setRepresentedObject:representedObject];

    // Update the view, if already loaded.
}


- (IBAction)getWeather:(NSButton *)sender {
    XAM_WeatherFetcher * fetcher = [[XAM_WeatherFetcher alloc] initWithCity:_cityField.stringValue state:_stateField.stringValue];

    XAM_WeatherResult * result = [fetcher getWeather];

    if (result) {
        _descriptionLabel.stringValue = [result text];
        _tempLabel.stringValue = [[result temp] stringByAppendingString:@" °F"];
    }
    // This means the managed API returned null an exception occured.
    else {
        _descriptionLabel.stringValue = @"An error ocurred";
        _tempLabel.stringValue = @" :( ";
    }
    
    
}
@end
