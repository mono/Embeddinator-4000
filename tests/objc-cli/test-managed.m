#import <Foundation/Foundation.h>
#include "bindings.h"

int main ()
{
	@autoreleasepool {
		
		// properties
		
		NSLog (@"%s static property getter only", [Platform isWindows] ? "[FAIL]" : "[PASS]");
		
		NSLog (@"%s static property getter", [Platform exitCode] == 0 ? "[PASS]" : "[FAIL]");
		Platform.exitCode = 255;
		NSLog (@"%s static property setter check", [Platform exitCode] == 255 ? "[PASS]" : "[FAIL]");
		
		NSLog (@"%s static property getter only", [Properties_Query universalAnswer] == 42 ? "[PASS]" : "[FAIL]");

		Properties_Query* query = [[Properties_Query alloc] init];
		NSLog (@"%s instance property getter only 1", [query isGood] ? "[PASS]" : "[FAIL]");
		NSLog (@"%s instance property getter only 2", ![query isBad] ? "[PASS]" : "[FAIL]");
		NSLog (@"%s instance property getter", [query answer] == 42 ? "[PASS]" : "[FAIL]");
		query.answer = 911;
		NSLog (@"%s instance property setter", [query answer] == 911 ? "[PASS]" : "[FAIL]");
			
		// namespaces
			
		id nonamespace = [[ClassWithoutNamespace alloc] init];
		NSLog (@"%s no namespace", [[nonamespace description] containsString:@"<ClassWithoutNamespace:"] ? "[PASS]" : "[FAIL]");

		id singlenamespace = [[First_ClassWithSingleNamespace alloc] init];
		NSLog (@"%s single namespace", [[singlenamespace description] containsString:@"<First_ClassWithSingleNamespace:"] ? "[PASS]" : "[FAIL]");
		
		id nestednamespaces = [[First_Second_ClassWithNestedNamespace alloc] init];
		NSLog (@"%s nested namespaces", [[nestednamespaces description] containsString:@"<First_Second_ClassWithNestedNamespace:"] ? "[PASS]" : "[FAIL]");
	}
	return 0;
}
