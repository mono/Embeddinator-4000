#include "mono_managed_to_native.h"
#include "glib.h"

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#ifdef __APPLE__
#include <errno.h>
#include <libproc.h>
#include <unistd.h>
#endif

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