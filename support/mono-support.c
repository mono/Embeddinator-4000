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

#include "mono-support.h"
#include "mono_embeddinator.h"

#if !defined(_WIN32)

#include <string.h>
#include <dlfcn.h>

#define log_info(cat, msg, ...)
#define log_error(cat, msg, ...)
#define log_fatal(cat, msg, ...)

DylibMono* mono_embeddinator_dylib_mono_new (const char *libmono_path)
{
	struct DylibMono *imports = calloc (1, sizeof (struct DylibMono));
	if (!imports)
		return NULL;
	if (!mono_embeddinator_dylib_mono_init (imports, libmono_path)) {
		free (imports);
		return NULL;
	}
	return imports;
}

void mono_embeddinator_dylib_mono_free (struct DylibMono *mono_imports)
{
	if (!mono_imports)
		return;
	dlclose (mono_imports->dl_handle);
	free (mono_imports);
}

int mono_embeddinator_dylib_mono_init (struct DylibMono *mono_imports, const char *libmono_path)
{
	bool symbols_missing = false;

	if (mono_imports == NULL)
		return false;

	memset (mono_imports, 0, sizeof (*mono_imports));

	/*
	 * We need to use RTLD_GLOBAL so that libmono-profiler-log.so can resolve
	 * symbols against the Mono library we're loading.
	 */
	mono_imports->dl_handle = dlopen (libmono_path, RTLD_LAZY | RTLD_GLOBAL);

	if (!mono_imports->dl_handle) {
		return false;
	}

	mono_imports->version   = sizeof (*mono_imports);

	log_info (LOG_DEFAULT, "Loading Mono symbols...");

#define LOAD_SYMBOL(symbol) \
	mono_imports->symbol = dlsym (mono_imports->dl_handle, #symbol); \
	if (!mono_imports->symbol) { \
		log_error (LOG_DEFAULT, "Failed to load Mono symbol: %s", #symbol); \
		symbols_missing = true; \
	}

	LOAD_SYMBOL(mono_method_desc_new)
	LOAD_SYMBOL(mono_method_desc_free)
	LOAD_SYMBOL(mono_method_desc_search_in_class)
	LOAD_SYMBOL(mono_jit_cleanup)
	LOAD_SYMBOL(mono_domain_assembly_open)
	LOAD_SYMBOL(mono_string_length)
	LOAD_SYMBOL(mono_string_chars)
	LOAD_SYMBOL(mono_field_get_value_object)
	LOAD_SYMBOL(mono_field_set_value)
	LOAD_SYMBOL(mono_class_vtable)
	LOAD_SYMBOL(mono_field_static_set_value)
	LOAD_SYMBOL(mono_object_to_string)
	LOAD_SYMBOL(mono_class_get)
	LOAD_SYMBOL(mono_class_get_field)
	LOAD_SYMBOL(mono_get_string_class)
	LOAD_SYMBOL(mono_get_boolean_class)
	LOAD_SYMBOL(mono_get_char_class)
	LOAD_SYMBOL(mono_get_sbyte_class)
	LOAD_SYMBOL(mono_get_int16_class)
	LOAD_SYMBOL(mono_get_int32_class)
	LOAD_SYMBOL(mono_get_int64_class)
	LOAD_SYMBOL(mono_get_byte_class)
	LOAD_SYMBOL(mono_get_uint16_class)
	LOAD_SYMBOL(mono_get_uint32_class)
	LOAD_SYMBOL(mono_get_uint64_class)
	LOAD_SYMBOL(mono_get_single_class)
	LOAD_SYMBOL(mono_get_double_class)
	LOAD_SYMBOL(mono_array_element_size)

	if (symbols_missing) {
		log_fatal (LOG_DEFAULT, "Failed to load some Mono symbols, aborting...");

		mono_embeddinator_error_t error;
		error.type = MONO_EMBEDDINATOR_MONO_RUNTIME_MISSING_SYMBOLS;
		mono_embeddinator_error(error);

		return false;
	}

	return true;
}

#endif
