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

#pragma once

#include "mono_embeddinator.h"
#include "glib.h"
#include "mono-support.h"

#import <Foundation/Foundation.h>

MONO_EMBEDDINATOR_BEGIN_DECLS

MONO_EMBEDDINATOR_API
NSString* mono_embeddinator_get_nsstring (MonoString* string);

MONO_EMBEDDINATOR_API
NSComparisonResult mono_embeddinator_compare_to (MonoEmbedObject *object, MonoMethod *method, MonoEmbedObject *other);

MONO_EMBEDDINATOR_API
MonoObject* mono_embeddinator_get_object (id native, bool assertOnFailure);

MONO_EMBEDDINATOR_API
MonoAssembly *mono_embeddinator_find_assembly_in_bundle (const char *assembly);

MONO_EMBEDDINATOR_API
NSDecimalNumber* mono_embeddinator_get_nsdecimalnumber (void* __unboxedresult);

MONO_EMBEDDINATOR_API
MonoDecimal mono_embeddinator_get_system_decimal (NSDecimalNumber* nsdecimalnumber, mono_embeddinator_context_t* context);

MONO_EMBEDDINATOR_API
E4KDateTime mono_embeddinator_get_system_datetime (NSDate* nsdate, mono_embeddinator_context_t* context);

MONO_EMBEDDINATOR_API
NSDate* mono_embeddinator_get_nsdate (E4KDateTime* datetime);

MONO_EMBEDDINATOR_END_DECLS
	
