﻿/*
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

#include "mono_embeddinator.h"
#include "glib.h"
#include "mono-support.h"

#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#if defined(__APPLE__)
#include <mach-o/dyld.h>
#endif

#if defined(__OBJC__)
#include <objc/runtime.h>
#endif

#ifdef _WIN32
#include <Windows.h>
#define PATH_MAX MAX_PATH
#else
#include <unistd.h>
#endif

/* To support configurations where Mono symbols need to be looked up dinamically
 * we define a macro for each Mono API that we rely on and either call the exported
 * definition from the shared library linked in or the function address that we
 * look up at runtime (see mono-support.h) */

#if defined(__ANDROID__)
#define MONO_DYLIB_DYNAMIC_CALLS
#endif

#if defined(MONO_DYLIB_DYNAMIC_CALLS)
#define MONO_API_DEF(name, ...) static name() _;
#else
#define MONO_API_DEF(name, ...) _##name##_fptr name;
#endif
#include "mono-api.h"

mono_embeddinator_context_t* _current_context;

mono_embeddinator_context_t* mono_embeddinator_get_context()
{
    return _current_context;
}

void mono_embeddinator_set_context(mono_embeddinator_context_t* ctx)
{
    _current_context = ctx;
}

static GString* mono_dylib_path_override = NULL;

void mono_embeddinator_set_mono_dylib_path (const char *path)
{
    mono_dylib_path_override = g_string_new (path);
}

int mono_embeddinator_init(mono_embeddinator_context_t* ctx, const char* domain)
{
    if (ctx == 0 || ctx->domain != 0)
        return false;

    if (mono_dylib_path_override != 0)
    {
        ctx->dylib = mono_embeddinator_dylib_mono_new (mono_dylib_path_override->str);
        g_string_free (mono_dylib_path_override, true);
    }

#if defined (XAMARIN_IOS) || defined (XAMARIN_MAC)
    xamarin_initialize_embedded ();
    ctx->domain = mono_domain_get ();
#else
    mono_config_parse(NULL);
    ctx->domain = mono_jit_init_version(domain, "v4.0.30319");
#endif

    mono_embeddinator_set_context(ctx);

    char cwd[PATH_MAX];
    getcwd(cwd, PATH_MAX);
    mono_domain_set_config(ctx->domain, cwd, "app.config");

    return true;
}

int mono_embeddinator_destroy(mono_embeddinator_context_t* ctx)
{
    if (ctx == 0 || ctx->domain != 0)
        return false;

    mono_jit_cleanup (ctx->domain);

    if (ctx->dylib != 0)
        mono_embeddinator_dylib_mono_free (ctx->dylib);

    return true;
}

static GString* path_override = NULL;

void mono_embeddinator_set_assembly_path (const char *path)
{
	path_override = g_string_new (path);
}

static GString* get_current_executable_path()
{
    if (path_override)
        return path_override;
#if defined(__APPLE__)
    char pathbuf [1024];
    uint32_t bufsize = sizeof (pathbuf);
    int ret = _NSGetExecutablePath (pathbuf, &bufsize);
    return ret == 0 ? g_string_new (pathbuf) : 0;
#elif defined(_WIN32)
    HMODULE hModule = GetModuleHandleW(0);
    CHAR pathbuf[MAX_PATH];
    DWORD ret = GetModuleFileNameA(hModule, pathbuf, MAX_PATH);

    return (ret > 0) ? g_string_new(pathbuf) : 0;
#else
    g_assert_not_reached();
#endif
}

static gchar* strrchr_seperator (const gchar* filename)
{
#ifdef G_OS_WIN32
    char *p2;
#endif
    char *p;

    p = strrchr (filename, G_DIR_SEPARATOR);
#ifdef G_OS_WIN32
    p2 = strrchr (filename, '/');
    if (p2 > p)
        p = p2;
#endif

    return p;
}

char* mono_embeddinator_search_assembly(const char* assembly)
{
    GString* path = get_current_executable_path();

    gchar* sep = strrchr_seperator(path->str);
    g_string_truncate(path, sep - path->str);

    g_string_append(path, G_DIR_SEPARATOR_S);
    g_string_append(path, assembly);

    char* data = path->str;
    g_string_free(path, /*free_segment=*/ FALSE);

    return data;
}

static mono_embeddinator_assembly_load_hook_t g_assembly_load_hook = NULL;

MonoImage* mono_embeddinator_load_assembly(mono_embeddinator_context_t* ctx, const char* assembly)
{
    char *path = NULL;
    MonoAssembly *mono_assembly = NULL;

    if (g_assembly_load_hook) {
        mono_assembly = g_assembly_load_hook (assembly);

        if (mono_assembly)
            return mono_assembly_get_image (mono_assembly);
    }

#if defined (XAMARIN_IOS) || defined (XAMARIN_MAC)
    mono_assembly = xamarin_open_assembly (assembly);
#else

    path = mono_embeddinator_search_assembly(assembly);

    mono_assembly = mono_domain_assembly_open (ctx->domain, path);
#endif

    if (!mono_assembly)
    {
        mono_embeddinator_error_t error;
        error.type = MONO_EMBEDDINATOR_ASSEMBLY_OPEN_FAILED;
        error.string = path;
        mono_embeddinator_error(error);

        if (path)
            g_free (path);

        return 0;
    }

    if (path)
        g_free (path);

    return mono_assembly_get_image(mono_assembly);
}

mono_embeddinator_assembly_load_hook_t
mono_embeddinator_install_assembly_load_hook(mono_embeddinator_assembly_load_hook_t hook)
{
    mono_embeddinator_assembly_load_hook_t prev = g_assembly_load_hook;
    g_assembly_load_hook = hook;
    return prev;
}

MonoClass* mono_embeddinator_search_class(const char* assembly, const char* _namespace,
    const char* name)
{
    mono_embeddinator_context_t* ctx = mono_embeddinator_get_context();

    char* path = mono_embeddinator_search_assembly(assembly);
    MonoAssembly* mono_assembly = mono_domain_assembly_open(ctx->domain, path);

    if (mono_assembly == 0)
    {
        mono_embeddinator_error_t error;
        error.type = MONO_EMBEDDINATOR_ASSEMBLY_OPEN_FAILED;
        error.string = path;
        mono_embeddinator_error(error);
    }

    g_free (path);

    MonoImage* image = mono_assembly_get_image(mono_assembly);
    MonoClass* klass = mono_class_from_name(image, _namespace, name);

    if (klass == 0)
    {
        mono_embeddinator_error_t error;
        error.type = MONO_EMBEDDINATOR_CLASS_LOOKUP_FAILED;
        error.string = path;
        mono_embeddinator_error(error);
    }

    return klass;
}

MonoMethod* mono_embeddinator_lookup_method(const char* method_name, MonoClass *klass)
{
    MonoMethodDesc* desc = mono_method_desc_new(method_name, /*include_namespace=*/true);
    MonoMethod* method = mono_method_desc_search_in_class(desc, klass);
    mono_method_desc_free(desc);

    if (!method)
    {
        mono_embeddinator_error_t error;
        error.type = MONO_EMBEDDINATOR_METHOD_LOOKUP_FAILED;
        error.string = method_name;
        mono_embeddinator_error(error);
    }

    return method;
}

void mono_embeddinator_throw_exception(MonoObject *exception)
{
    mono_embeddinator_error_t error;
    error.type = MONO_EMBEDDINATOR_EXCEPTION_THROWN;
    error.exception = (MonoException*) exception;
    error.string = 0;

    mono_embeddinator_error(error);
}

char* mono_embeddinator_error_to_string(mono_embeddinator_error_t error)
{
    switch(error.type)
    {
    case MONO_EMBEDDINATOR_OK:
        return "No error";
    case MONO_EMBEDDINATOR_EXCEPTION_THROWN:
        return "Mono threw a managed exception";
    case MONO_EMBEDDINATOR_ASSEMBLY_OPEN_FAILED:
        return "Mono failed to load assembly";
    case MONO_EMBEDDINATOR_CLASS_LOOKUP_FAILED:
        return "Mono failed to lookup class";
    case MONO_EMBEDDINATOR_METHOD_LOOKUP_FAILED:
        return "Mono failed to lookup method";
    case MONO_EMBEDDINATOR_MONO_RUNTIME_LOAD_FAILED:
        return "Failed to load Mono runtime shared library";
    case MONO_EMBEDDINATOR_MONO_RUNTIME_MISSING_SYMBOLS:
        return "Failed to look up Mono runtime shared library symbols";
    }

    g_assert_not_reached();
}

static void mono_embeddinator_report_error_and_abort(mono_embeddinator_error_t error)
{
    fprintf(stderr, "Embeddinator error: %s.\n", mono_embeddinator_error_to_string(error));
    abort();
}

static mono_embeddinator_error_report_hook_t g_error_report_hook = mono_embeddinator_report_error_and_abort;

void* mono_embeddinator_install_error_report_hook(mono_embeddinator_error_report_hook_t hook)
{
    mono_embeddinator_error_report_hook_t prev = g_error_report_hook;
    g_error_report_hook = hook;

    return prev;
}

void mono_embeddinator_error(mono_embeddinator_error_t error)
{
    if (g_error_report_hook == 0)
        return;

    g_error_report_hook(error);
}

void* mono_embeddinator_create_object(MonoObject* instance)
{
    MonoEmbedObject* object = g_new(MonoEmbedObject, 1);
    mono_embeddinator_init_object(object, instance);

    return object;
}

void mono_embeddinator_init_object(MonoEmbedObject* object, MonoObject* instance)
{
    object->_class = mono_object_get_class(instance);
    object->_handle = mono_gchandle_new(instance, /*pinned=*/false);
}

void mono_embeddinator_destroy_object(MonoEmbedObject* object)
{
    mono_gchandle_free (object->_handle);
    g_free (object);
}

MonoObject* mono_embeddinator_get_cultureinfo_invariantculture_object ()
{
    static MonoObject* invariantculture = NULL;
    if (!invariantculture) {
        MonoClass* klass = mono_class_from_name (mono_get_corlib (), "System.Globalization", "CultureInfo");
        const char mname [] = "System.Globalization:get_InvariantCulture()";
        MonoMethod* method = mono_embeddinator_lookup_method (mname, klass);
        MonoObject* ex = NULL;
        invariantculture = mono_runtime_invoke (method, NULL, NULL, &ex);
        if (ex) {
            mono_embeddinator_throw_exception (ex);
        }
    }
    return invariantculture;
}

MonoClass* mono_embeddinator_get_decimal_class ()
{
    static MonoClass* decimalclass = NULL;
    if (!decimalclass) {
        decimalclass = mono_class_from_name (mono_get_corlib (), "System", "Decimal");
    }
    return decimalclass;
}
