#include "mono_embeddinator.h"
#import <Foundation/Foundation.h>

MONO_EMBEDDINATOR_BEGIN_DECLS

// forward declarations
@class Platform;

// Platform, managed, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
@interface Platform : NSObject {
	MonoEmbedObject* _object;
}

// a .net static type cannot be initialized
- (instancetype)init NS_UNAVAILABLE;

@property (nonatomic, class, readonly) bool isWindows;
@property (nonatomic, class, readwrite) int exitCode;

@end

MONO_EMBEDDINATOR_END_DECLS
