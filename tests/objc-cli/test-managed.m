#import <Foundation/Foundation.h>
#include "mono_embeddinator.h"
#include "bindings.h"

int main ()
{
	@autoreleasepool {
		NSLog (@"%s static property getter only", [Platform isWindows] ? "[FAIL]" : "[PASS]");
		
		NSLog (@"%s static property getter", [Platform exitCode] == 0 ? "[PASS]" : "[FAIL]");
		Platform.exitCode = 255;
		NSLog (@"%s static property setter check", [Platform exitCode] == 255 ? "[PASS]" : "[FAIL]");
	}
	return 0;
}
