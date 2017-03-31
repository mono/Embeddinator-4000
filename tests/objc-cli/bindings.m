#include "bindings.h"
#include "glib.h"
#include <mono/jit/jit.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/object.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/debug-helpers.h>

mono_embeddinator_context_t __mono_context;

MonoImage* __managed_image;

static MonoClass* Platform_class = nil;

static void __initialize_mono ()
{
	if (__mono_context.domain)
		return;
	mono_embeddinator_init (&__mono_context, "mono_embeddinator_binding");
}

static void __lookup_assembly_managed ()
{
	if (__managed_image)
		return;
	__managed_image = mono_embeddinator_load_assembly (&__mono_context, "managed.dll");
}

static void __lookup_class_Platform ()
{
	if (!Platform_class) {
		__initialize_mono ();
		__lookup_assembly_managed ();
		Platform_class = mono_class_from_name (__managed_image, "", "Platform");
	}
}
// Platform, managed, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
@implementation Platform

+ (bool) isWindows
{
	const char __method_name [] = "Platform:get_IsWindows()";
	static MonoMethod* __method = nil;
	if (!__method) {
		__lookup_class_Platform ();
		__method = mono_embeddinator_lookup_method (__method_name, Platform_class);
	}
	MonoObject* __exception = nil;
	MonoObject* __result = mono_runtime_invoke (__method, nil, nil, &__exception);
	if (__exception)
		mono_embeddinator_throw_exception (__exception);
	void* __unbox = mono_object_unbox (__result);
	return *((bool*)__unbox);
}
+ (int) exitCode
{
	const char __method_name [] = "Platform:get_ExitCode()";
	static MonoMethod* __method = nil;
	if (!__method) {
		__lookup_class_Platform ();
		__method = mono_embeddinator_lookup_method (__method_name, Platform_class);
	}
	MonoObject* __exception = nil;
	MonoObject* __result = mono_runtime_invoke (__method, nil, nil, &__exception);
	if (__exception)
		mono_embeddinator_throw_exception (__exception);
	void* __unbox = mono_object_unbox (__result);
	return *((int*)__unbox);
}
+ (void) setExitCode:(int)value
{
	const char __method_name [] = "Platform:set_ExitCode(int)";
	static MonoMethod* __method = nil;
	if (!__method) {
		__lookup_class_Platform ();
		__method = mono_embeddinator_lookup_method (__method_name, Platform_class);
	}
	void* __args [1];
	__args [0] = &value;
	MonoObject* __exception = nil;
	MonoObject* __result = mono_runtime_invoke (__method, nil, __args, &__exception);
	if (__exception)
		mono_embeddinator_throw_exception (__exception);
}

@end
