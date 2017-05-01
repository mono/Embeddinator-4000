/*
 * ObjC support code
 *
 * Copyright (C) 2017 Microsoft Inc.
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#include "objc-support.h"

NSString* mono_embeddinator_get_nsstring (MonoString* string)
{
	if (string == NULL)
		return NULL;
	int length = mono_string_length (string);
	gunichar2 *str = mono_string_chars (string);
	return [[NSString alloc] initWithBytes: str length: length * 2 encoding: NSUTF16LittleEndianStringEncoding];
}

// helper for System.IComparable
NSComparisonResult mono_embeddinator_compare_to (MonoEmbedObject *object, MonoMethod *method, MonoEmbedObject *other)
{
	if (!other)
		return NSOrderedSame;
	void* __args [1];
	__args [0] = mono_gchandle_get_target (other->_handle);
	MonoObject* __exception = nil;
	MonoObject* __instance = mono_gchandle_get_target (object->_handle);
	MonoObject* __result = mono_runtime_invoke (method, __instance, __args, &__exception);
	if (__exception)
		mono_embeddinator_throw_exception (__exception);
	void* __unbox = mono_object_unbox (__result);
	return (NSComparisonResult) *((int*)__unbox);
}

MonoObject* mono_embeddinator_get_object (id native, bool assertOnFailure)
{
	if (![native respondsToSelector:@selector (xamarinGetGCHandle)]) {
		if (!assertOnFailure)
			return nil;
		NSLog (@"`%@` is not a managed instance and cannot be used like one", [native description]);
		abort ();
	}
	int gchandle = (int) [native performSelector:@selector (xamarinGetGCHandle)];
	return mono_gchandle_get_target (gchandle);
}
