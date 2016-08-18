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

const char* mono_managed_to_native_search_assembly(const char* assembly)
{
	GString* current_exe_path = get_current_executable_path();
	puts(current_exe_path->str);

	return assembly;
}