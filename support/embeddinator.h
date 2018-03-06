/*
 * Definitions needed for the generated header files.
 *
 * (C) 2017 Microsoft, Inc.
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

#ifdef  __cplusplus
#if !defined (__OBJC__)
#include <cstdbool>
#include <cstdint>
#endif
#else
#include <stdbool.h>
#include <stdint.h>
#endif

#ifdef  __cplusplus
    #define MONO_EMBEDDINATOR_BEGIN_DECLS  extern "C" {
    #define MONO_EMBEDDINATOR_END_DECLS    }
#else
    #define MONO_EMBEDDINATOR_BEGIN_DECLS
    #define MONO_EMBEDDINATOR_END_DECLS
#endif

#if defined(_MSC_VER)
    #define MONO_EMBEDDINATOR_API_EXPORT __declspec(dllexport)
    #define MONO_EMBEDDINATOR_API_IMPORT __declspec(dllimport)
#else
    #define MONO_EMBEDDINATOR_API_EXPORT __attribute__ ((visibility ("default")))
    #define MONO_EMBEDDINATOR_API_IMPORT
#endif

#if defined(MONO_EMBEDDINATOR_DLL_EXPORT)
    #define MONO_EMBEDDINATOR_API MONO_EMBEDDINATOR_API_EXPORT
#else
    #define MONO_EMBEDDINATOR_API MONO_EMBEDDINATOR_API_IMPORT
#endif

/**
 * Objects
 */

typedef struct MonoEmbedObject MonoEmbedObject;
