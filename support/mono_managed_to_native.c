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

char* mono_managed_to_native_search_assembly(const char* assembly)
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