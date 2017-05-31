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

#if !defined (XAMARIN_IOS) && !defined (XAMARIN_MAC)
@interface MonoEmbeddinatorFindBundleObject : NSObject
@end
@implementation MonoEmbeddinatorFindBundleObject
@end
#endif

MonoAssembly *
mono_embeddinator_find_assembly_in_bundle (const char *assembly)
{
#if defined (XAMARIN_IOS) || defined (XAMARIN_MAC)
	return xamarin_open_assembly (assembly);
#else
	// Locating the bundle in which this code is located is surprisingly
	// difficult. There's an NSBundle API that can find the bundle in which a
	// particular Objective-C class is in, so we create a dummy Objective-C
	// class for only this purpose, and use it to get the bundle.
	NSBundle *bundle = [NSBundle bundleForClass: [MonoEmbeddinatorFindBundleObject class]];
	NSString *path = [bundle resourcePath];
	path = [path stringByAppendingPathComponent: @"MonoBundle"];
	path = [path stringByAppendingPathComponent: [NSString stringWithUTF8String:assembly]];
	if ([[NSFileManager defaultManager] fileExistsAtPath: path]) {
		return mono_assembly_open ([path UTF8String], NULL);
	} else {
		return NULL;
	}
#endif
}

static NSDictionary* forcedotseparator = nil;

NSDecimalNumber* mono_embeddinator_get_nsdecimalnumber (void* unboxedresult)
{
	static MonoMethod* tostringmethod = nil;

	MonoObject* invariantculture = mono_embeddinator_get_cultureinfo_invariantculture_object ();
	void* tostringargs [1];
	tostringargs [0] = invariantculture;

	if (!tostringmethod)
		tostringmethod = mono_embeddinator_lookup_method (":ToString(System.IFormatProvider)", mono_embeddinator_get_decimal_class ());

	MonoObject* ex = nil;
	MonoString* decimalmonostr = (MonoString *) mono_runtime_invoke (tostringmethod, unboxedresult, tostringargs, &ex);
	if (ex)
		mono_embeddinator_throw_exception (ex);
	NSString* decimalnsstr = mono_embeddinator_get_nsstring (decimalmonostr);

	// Force NSDecimalNumber to parse the number using dot as decimal separator http://stackoverflow.com/a/7905775/572076
	if (!forcedotseparator)
		forcedotseparator = @{ NSLocaleDecimalSeparator : @"." };
	NSDecimalNumber* nsdecresult = [NSDecimalNumber decimalNumberWithString:decimalnsstr locale:forcedotseparator];

	return nsdecresult;
}

MonoDecimal mono_embeddinator_get_system_decimal (NSDecimalNumber* nsdecimalnumber, mono_embeddinator_context_t* context)
{
	static MonoMethod* decimalparsemethod = nil;

	// Force NSDecimalNumber to parse the number using dot as decimal separator http://stackoverflow.com/a/7905775/572076
	if (!forcedotseparator)
		forcedotseparator = @{ NSLocaleDecimalSeparator : @"." };

	NSString* nsdecimalstr = [nsdecimalnumber descriptionWithLocale:forcedotseparator];
	MonoString* decimalstr = mono_string_new (context->domain, [nsdecimalstr UTF8String]);

	MonoObject* invariantculture = mono_embeddinator_get_cultureinfo_invariantculture_object ();
	void* parseargs [2];
	parseargs [0] = decimalstr;
	parseargs [1] = invariantculture;

	if (!decimalparsemethod)
		decimalparsemethod = mono_embeddinator_lookup_method ("System.Decimal:Parse(string,System.IFormatProvider)", mono_embeddinator_get_decimal_class ());

	MonoObject* ex = nil;
	MonoObject* boxeddecimal = mono_runtime_invoke (decimalparsemethod, NULL, parseargs, &ex);
	if (ex)
		mono_embeddinator_throw_exception (ex);

	MonoDecimal mdecimal = *(MonoDecimal*) mono_object_unbox (boxeddecimal);

	return mdecimal;
}

// NSDate reference date 00:00:00 UTC on 1 January 2001
// https://developer.apple.com/reference/foundation/nsdate
#define NSDateRefDateTicks 631139040000000000LL
#define NetTicksPerSecond 10000000LL
#define DateTimeMaxValueTicks 3155378975999999999LL
#define DateTimeMinValueTicks 0LL

NSDate* mono_embeddinator_get_nsdate (E4KDateTime* datetime)
{
	static MonoMethod* dtticksmethod = nil;
	static MonoMethod* dtkindmethod = nil;
	static MonoMethod* dttoutcmethod = nil;

	MonoClass* datetimeclass = mono_embeddinator_get_datetime_class ();

	if (!dtticksmethod)
		dtticksmethod = mono_embeddinator_lookup_method ("System.DateTime:get_Ticks()", datetimeclass);
	if (!dtkindmethod)
		dtkindmethod = mono_embeddinator_lookup_method ("System.DateTimeKind:get_Kind()", datetimeclass);

	MonoObject* kindex = nil;
	MonoObject* kindboxed = mono_runtime_invoke (dtkindmethod, datetime, NULL, &kindex);
	if (kindex)
		mono_embeddinator_throw_exception (kindex);
	E4KDateTimeKind kind = *(E4KDateTimeKind *) mono_object_unbox (kindboxed);

	if (kind == E4KDateTimeKind_Local) {
		if (!dttoutcmethod)
			dttoutcmethod = mono_embeddinator_lookup_method ("System.DateTime:ToUniversalTime()", datetimeclass);

		MonoObject* toutcex = nil;
		MonoObject* utcdtboxed = mono_runtime_invoke (dttoutcmethod, datetime, NULL, &toutcex);
		if (toutcex)
			mono_embeddinator_throw_exception (toutcex);
		datetime = mono_object_unbox (utcdtboxed);
	}

	MonoObject* ticks_ex = nil;
	MonoObject* ticksboxed = mono_runtime_invoke (dtticksmethod, datetime, NULL, &ticks_ex);
	if (ticks_ex)
		mono_embeddinator_throw_exception (ticks_ex);
	long long ticks = *(long long *) mono_object_unbox (ticksboxed);

	NSTimeInterval seconds = (NSTimeInterval) (ticks - NSDateRefDateTicks) / NetTicksPerSecond;
	NSDate* nsdate = [NSDate dateWithTimeIntervalSinceReferenceDate:seconds];

	return nsdate;
}

E4KDateTime mono_embeddinator_get_system_datetime (NSDate* nsdate, mono_embeddinator_context_t* context)
{
	static MonoMethod* datetimector = nil;

	MonoClass* datetimeclass = mono_embeddinator_get_datetime_class ();
	int utc = E4KDateTimeKind_Utc;
	long long minvalueticks = DateTimeMinValueTicks;

	void* datetimeargs [2];
	long long nsdateinterval;
	long long dateticks;

	if (!nsdate)
		datetimeargs [0] = &minvalueticks;
	else {
		nsdateinterval = [nsdate timeIntervalSinceReferenceDate];
		dateticks = nsdateinterval * NetTicksPerSecond + NSDateRefDateTicks;

		if (dateticks > DateTimeMaxValueTicks)
			dateticks = DateTimeMaxValueTicks;
		else if (dateticks < DateTimeMinValueTicks)
			dateticks = DateTimeMinValueTicks;

		datetimeargs [0] = &dateticks;
	}

	datetimeargs [1] = &utc;

	if (!datetimector)
		datetimector = mono_embeddinator_lookup_method ("System.DateTime:.ctor(long,System.DateTimeKind)", datetimeclass);

	E4KDateTime datetime = { 0 };

	MonoObject* ex = nil;
	mono_runtime_invoke (datetimector, &datetime, datetimeargs, &ex);
	if (ex)
		mono_embeddinator_throw_exception (ex);

	return datetime;
}
