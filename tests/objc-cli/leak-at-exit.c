//
// leak-at-exit.c
//
// Author:
//   Rolf Bjarne Kvinge
//
// Copyright 2017 Microsoft
//

#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>
#include <errno.h>
#include <time.h>

static void my__exit (int status);

typedef void (*ExitFunc) (int status);
ExitFunc system__exit = exit;

static void
my__exit (int status)
{
	const char *ready_file = getenv ("LEAK_READY_FILE");
	const char *done_file = getenv ("LEAK_DONE_FILE");
	if (!ready_file || !done_file)
		return;

	fprintf (stderr, "Checking for leaks\n");

	int rv = unlink (ready_file);
	if (rv != 0) {
		fprintf (stderr, "Could not delete ready file %s: %s\n", ready_file, strerror (errno));;
		return;
	}

	rv = access (done_file, F_OK);
	while (rv == 0) {
		// fprintf (stdout, "Waiting for done file to be deleted...\n");
		struct timespec ts;
		ts.tv_sec = 0;
		ts.tv_nsec = 100000000; /* 100 ms */
		nanosleep (&ts, NULL);
		rv = access (done_file, F_OK);
	}
	if (errno != ENOENT)
		fprintf (stdout, "Failed to access done file %s: %s\n", done_file, strerror (errno));

	fprintf (stdout, "Leak check performed\n");

	system__exit (status);
}

typedef struct interpose_s {
	void *new_func;
	void *orig_func;
} interpose_t;

static const interpose_t interposers[] __attribute__ ((used)) \
	__attribute__ ((section("__DATA,__interpose"))) = { 
		{ (void *) &my__exit,  (void *) &exit  },
    };
