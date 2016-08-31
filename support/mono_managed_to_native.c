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

#include "mono_managed_to_native.h"
#include "glib.h"

#include <stdbool.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef __APPLE__
#include <errno.h>
#include <libproc.h>
#include <unistd.h>
#endif

#include <mono/jit/jit.h>
#include <mono/metadata/mono-config.h>

int mono_m2n_init(mono_m2n_context_t* ctx, const char* domain)
{
    if (ctx == 0 || ctx->domain != 0)
        return false;

    mono_config_parse(NULL);
    ctx->domain = mono_jit_init_version(domain, "v4.0.30319");

    return true;
}

int mono_m2n_destroy(mono_m2n_context_t* ctx)
{
    if (ctx == 0 || ctx->domain != 0)
        return false;

    mono_jit_cleanup (ctx->domain);
    return true;
}

static GString* get_current_executable_path()
{
#ifdef __APPLE__
    int ret;
    pid_t pid; 
    char pathbuf[PROC_PIDPATHINFO_MAXSIZE];

    pid = getpid();
    ret = proc_pidpath (pid, pathbuf, sizeof(pathbuf));

   return (ret > 0) ? g_string_new(pathbuf) : 0;
#else
   return 0;
#endif
}

static gchar*
strrchr_seperator (const gchar* filename)
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

char* mono_m2n_search_assembly(const char* assembly)
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

static mono_m2n_assembly_search_hook_t g_assembly_search_hook = 0;

void* mono_m2n_install_assembly_search_hook(mono_m2n_assembly_search_hook_t hook)
{
    mono_m2n_assembly_search_hook_t prev = g_assembly_search_hook;
    g_assembly_search_hook = hook;
    return prev;
}

static mono_m2n_error_report_hook_t g_error_report_hook = 0;

void* mono_m2n_install_error_report_hook(mono_m2n_error_report_hook_t hook)
{
    mono_m2n_error_report_hook_t prev = g_error_report_hook;
    g_error_report_hook = hook;
    return prev;
}

void mono_m2n_error(mono_m2n_error_t error)
{
    if (g_error_report_hook == 0)
        return;

    g_error_report_hook(error);
}