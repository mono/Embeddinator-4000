#import <Foundation/Foundation.h>
#include "bindings.h"

int main (int argc, const char * argv[])
{
	@autoreleasepool {
		int counter = argc == 1 ? 1000000 : atoi (argv [0]);

		for (int i = 0; i < counter; i++) {
			assert (![Platform isWindows]);
		}
	}
	return 0;
}
