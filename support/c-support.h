/*
 * Mono support code
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

#include "embeddinator.h"
#include "glib.h"
#include "mono-support.h"
#include "mono_embeddinator.h"

MONO_EMBEDDINATOR_BEGIN_DECLS

/**
 * Arrays
 */
typedef struct MonoEmbedArray
{
    GArray* array;
} MonoEmbedArray;

typedef MonoEmbedArray _BoolArray;
typedef MonoEmbedArray _CharArray;
typedef MonoEmbedArray _SByteArray;
typedef MonoEmbedArray _ByteArray;
typedef MonoEmbedArray _Int16Array;
typedef MonoEmbedArray _UInt16Array;
typedef MonoEmbedArray _Int32Array;
typedef MonoEmbedArray _UInt32Array;
typedef MonoEmbedArray _Int64Array;
typedef MonoEmbedArray _UInt64Array;
typedef MonoEmbedArray _SingleArray;
typedef MonoEmbedArray _DoubleArray;
typedef MonoEmbedArray _StringArray;
typedef MonoEmbedArray _DecimalArray;

/**
 * Performs marshaling of a given MonoDecimal to a GLib string.
 */
MONO_EMBEDDINATOR_API
GString* mono_embeddinator_decimal_to_gstring (MonoDecimal decimal);

/**
 * Performs marshaling of a given MonoDecimal to a GLib string.
 */
MONO_EMBEDDINATOR_API
MonoDecimal mono_embeddinator_string_to_decimal (const char * number);

/**
 * Performs marshaling of a given MonoString to a GLib string.
 */
MONO_EMBEDDINATOR_API
void mono_embeddinator_marshal_string_to_gstring(GString* g_string, MonoString* mono_string);

MONO_EMBEDDINATOR_END_DECLS