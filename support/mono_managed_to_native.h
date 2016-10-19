/*
 * Mono managed-to-native support code.
 *
 * Author:
 *   Joao Matos (joao.matos@xamarin.com)
 *
 * (C) 2016 Microsoft, Inc.
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

#ifdef  __cplusplus
#include <cstdbool>
#include <cstdint> 
#else
#include <stdbool.h>
#include <stdint.h>
#endif

#ifdef  __cplusplus
    #define MONO_M2N_BEGIN_DECLS  extern "C" {
    #define MONO_M2N_END_DECLS    }
#else
    #define MONO_M2N_BEGIN_DECLS
    #define MONO_M2N_END_DECLS
#endif

#if defined(_MSC_VER)
    #define MONO_M2N_API_EXPORT __declspec(dllexport)
    #define MONO_M2N_API_IMPORT __declspec(dllimport)
#else
    #define MONO_M2N_API_EXPORT __attribute__ ((visibility ("default")))
    #define MONO_M2N_API_IMPORT
#endif

#if defined(MONO_M2N_DLL_EXPORT)
    #define MONO_M2N_API MONO_M2N_API_EXPORT
#else
    #define MONO_M2N_API MONO_M2N_API_IMPORT
#endif

typedef uint16_t gunichar2;

typedef struct _GArray GArray;
typedef struct _GString GString;

typedef struct _MonoDomain MonoDomain;
typedef struct _MonoException MonoException;
typedef struct _MonoClass MonoClass;
typedef struct _MonoObject MonoObject;

MONO_M2N_BEGIN_DECLS

/** 
 * Represents a managed-to-native binding context.
 */
typedef struct
{
  MonoDomain* domain;
} mono_m2n_context_t;

/** 
 * Initializes a managed-to-native binding context.
 * Returns a boolean indicating success or failure.
 */
int mono_m2n_init(mono_m2n_context_t* ctx, const char* domain);

/** 
 * Destroys the managed-to-native binding context.
 * Returns a boolean indicating success or failure.
 */
int mono_m2n_destroy(mono_m2n_context_t* ctx);

/** 
 * Returns the current context.
 */
mono_m2n_context_t* mono_m2n_get_context();

/** 
 * Sets the current context.
 */
void mono_m2n_set_context(mono_m2n_context_t* ctx);

/** 
 * Searches and returns the path to the given managed assembly.
 */
char* mono_m2n_search_assembly(const char* assembly);

/** Represents the assembly search hook function type. */
typedef const char* (*mono_m2n_assembly_search_hook_t)(const char*);

/**
 * Installs an hook that returns the path to the given managed assembly.
 * Returns the previous installed hook.
 */
void* mono_m2n_install_assembly_search_hook(mono_m2n_assembly_search_hook_t hook);

/** 
 * Searches and returns for the Mono class in the given assembly.
 */
MonoClass* mono_m2n_search_class(const char* assembly, const char* _namespace,
    const char* name);

/**
 * Represents the different types of errors to be reported.
 */
typedef enum
{
    MONO_M2N_OK = 0,
    // Mono managed exception
    MONO_M2N_EXCEPTION_THROWN,
    // Mono failed to load assembly
    MONO_M2N_ASSEMBLY_OPEN_FAILED,
    // Mono failed to lookup method
    MONO_M2N_METHOD_LOOKUP_FAILED
} mono_m2n_error_type_t;

/**
 * Represents the error type and associated data.
 */
typedef struct
{
    mono_m2n_error_type_t type;
    // Contains exception object if type is MONO_M2N_EXCEPTION_THROWN
    MonoException* exception;
    const char* string;
} mono_m2n_error_t;

/**
 * Fires an error and calls the installed error hook for handling.
 */
void mono_m2n_error(mono_m2n_error_t error);

/** Represents the error report hook function type. */
typedef void (*mono_m2n_error_report_hook_t)(mono_m2n_error_t);

/**
 * Installs an hook that is called for each error reported.
 */
void* mono_m2n_install_error_report_hook(mono_m2n_error_report_hook_t hook);

/** 
 * Arrays
 */

typedef struct
{
    GArray* array;
} MonoEmbedArray;

/**
 * Objects
 */

typedef struct
{
    MonoClass* _class;
    uint32_t _handle;
} MonoEmbedObject;

/**
 * Creates an support object from a Mono object instance.
 */
void* mono_m2n_create_object(MonoObject* instance);

MONO_M2N_END_DECLS
