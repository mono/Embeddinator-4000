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

#include "c-support.h"

GString* mono_embeddinator_decimal_to_gstring (MonoDecimal decimal)
{
    static MonoMethod* tostringmethod = 0;

    MonoObject* invariantculture = mono_embeddinator_get_cultureinfo_invariantculture_object ();
    void* tostringargs [1];
    tostringargs [0] = invariantculture;

    if (!tostringmethod)
        tostringmethod = mono_embeddinator_lookup_method (":ToString(System.IFormatProvider)", mono_embeddinator_get_decimal_class ());

    MonoObject* ex = 0;
    MonoString* decimalmonostr = (MonoString *) mono_runtime_invoke (tostringmethod, &decimal, tostringargs, &ex);
    if (ex)
        mono_embeddinator_throw_exception (ex);

    GString* gstring = g_string_new("");
    mono_embeddinator_marshal_string_to_gstring (gstring, decimalmonostr);
    
    return gstring;
}

MonoDecimal mono_embeddinator_string_to_decimal (const char * number)
{
    static MonoMethod* decimalparsemethod = 0;

    MonoString* decimalstr = mono_string_new (mono_embeddinator_get_context()->domain, number);

    MonoObject* invariantculture = mono_embeddinator_get_cultureinfo_invariantculture_object ();
    void* parseargs [2];
    parseargs [0] = decimalstr;
    parseargs [1] = invariantculture;

    if (!decimalparsemethod)
        decimalparsemethod = mono_embeddinator_lookup_method ("System.Decimal:Parse(string,System.IFormatProvider)",
            mono_embeddinator_get_decimal_class ());

    MonoObject* ex = 0;
    MonoObject* boxeddecimal = mono_runtime_invoke (decimalparsemethod, NULL, parseargs, &ex);
    if (ex)
        mono_embeddinator_throw_exception (ex);

    MonoDecimal mdecimal = *(MonoDecimal*) mono_object_unbox (boxeddecimal);

    return mdecimal;
}

void mono_embeddinator_marshal_string_to_gstring(GString* g_string, MonoString* mono_string)
{
    if (!mono_string)
    {
        g_string_null(g_string);
        return;
    }
    
    g_string_truncate(g_string, 0);
    g_string_append(g_string, mono_string_to_utf8((MonoString*) mono_string));
}