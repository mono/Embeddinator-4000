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

#pragma once

#ifdef  __cplusplus
#include <cstdbool>
#include <cstdint> 
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

typedef uint16_t gunichar2;

typedef struct _GArray GArray;
typedef struct _GString GString;

typedef struct _MonoDomain MonoDomain;
typedef struct _MonoException MonoException;
typedef struct _MonoClass MonoClass;
typedef struct _MonoObject MonoObject;
typedef struct _MonoImage MonoImage;
typedef struct _MonoMethod MonoMethod;

MONO_EMBEDDINATOR_BEGIN_DECLS

/** 
 * Represents a managed-to-native binding context.
 */
typedef struct
{
  MonoDomain* domain;
} mono_embeddinator_context_t;

/** 
 * Initializes a managed-to-native binding context.
 * Returns a boolean indicating success or failure.
 */
MONO_EMBEDDINATOR_API
int mono_embeddinator_init(mono_embeddinator_context_t* ctx, const char* domain);

/** 
 * Destroys the managed-to-native binding context.
 * Returns a boolean indicating success or failure.
 */
MONO_EMBEDDINATOR_API
int mono_embeddinator_destroy(mono_embeddinator_context_t* ctx);

/** 
 * Returns the current context.
 */
MONO_EMBEDDINATOR_API
mono_embeddinator_context_t* mono_embeddinator_get_context();

/**
 * Override the default path (current executable) where assemblies will be loaded.
 */
MONO_EMBEDDINATOR_API
void mono_embeddinator_set_assembly_path (const char *path);

/** 
 * Sets the current context.
 */
MONO_EMBEDDINATOR_API
void mono_embeddinator_set_context(mono_embeddinator_context_t* ctx);

/** 
 * Loads an assembly into the context.
 */
MONO_EMBEDDINATOR_API
MonoImage* mono_embeddinator_load_assembly(mono_embeddinator_context_t* ctx,
    const char* assembly);

/** 
 * Searches and returns the path to the given managed assembly.
 */
MONO_EMBEDDINATOR_API
char* mono_embeddinator_search_assembly(const char* assembly);

/** Represents the assembly search hook function type. */
typedef const char* (*mono_embeddinator_assembly_search_hook_t)(const char*);

/**
 * Installs an hook that returns the path to the given managed assembly.
 * Returns the previous installed hook.
 */
MONO_EMBEDDINATOR_API
void* mono_embeddinator_install_assembly_search_hook(mono_embeddinator_assembly_search_hook_t hook);

/** 
 * Searches and returns for the Mono class in the given assembly.
 */
MONO_EMBEDDINATOR_API
MonoClass* mono_embeddinator_search_class(const char* assembly, const char* _namespace,
    const char* name);

/** 
 * Looks up and returns a MonoMethod* for a given Mono class and method name.
 */
MONO_EMBEDDINATOR_API
MonoMethod* mono_embeddinator_lookup_method(const char* method_name, MonoClass* klass);

/** 
 * Throws an exception based on a given Mono exception object.
 */
MONO_EMBEDDINATOR_API
void mono_embeddinator_throw_exception(MonoObject* exception);

/**
 * Represents the different types of errors to be reported.
 */
typedef enum
{
    MONO_EMBEDDINATOR_OK = 0,
    // Mono managed exception
    MONO_EMBEDDINATOR_EXCEPTION_THROWN,
    // Mono failed to load assembly
    MONO_EMBEDDINATOR_ASSEMBLY_OPEN_FAILED,
    // Mono failed to lookup method
    MONO_EMBEDDINATOR_METHOD_LOOKUP_FAILED
} mono_embeddinator_error_type_t;

/**
 * Represents the error type and associated data.
 */
typedef struct
{
    mono_embeddinator_error_type_t type;
    // Contains exception object if type is MONO_EMBEDDINATOR_EXCEPTION_THROWN
    MonoException* exception;
    const char* string;
} mono_embeddinator_error_t;

/**
 * Fires an error and calls the installed error hook for handling.
 */
MONO_EMBEDDINATOR_API
void mono_embeddinator_error(mono_embeddinator_error_t error);

/** Represents the error report hook function type. */
typedef void (*mono_embeddinator_error_report_hook_t)(mono_embeddinator_error_t);

/**
 * Installs an hook that is called for each error reported.
 */
MONO_EMBEDDINATOR_API
void* mono_embeddinator_install_error_report_hook(mono_embeddinator_error_report_hook_t hook);

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
 * Creates a MonoEmbedObject support object from a Mono object instance.
 */
MONO_EMBEDDINATOR_API
void* mono_embeddinator_create_object(MonoObject* instance);

/**
 * Initializes a MonoEmbedObject object from a Mono object instance.
 */
MONO_EMBEDDINATOR_API
void mono_embeddinator_init_object(MonoEmbedObject* object, MonoObject* instance);

/**
 * Destroys a MonoEmbedObject object for a Mono object instance.
 */
MONO_EMBEDDINATOR_API
void mono_embeddinator_destroy_object(MonoEmbedObject *object);

MONO_EMBEDDINATOR_END_DECLS
