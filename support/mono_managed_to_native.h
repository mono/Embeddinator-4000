/*
 * Mono managed-to-native support API.
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
extern "C" {
#endif

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
 * Represents the different types of errors to be reported.
 */
typedef enum
{
    MONO_M2N_OK = 0,
    // Mono failed to load assembly
    MONO_M2N_ASSEMBLY_OPEN_FAILED
} mono_m2n_error_type_t;

/**
 * Represents the error type and associated data.
 */
typedef struct
{
    mono_m2n_error_type_t type;
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

#ifdef  __cplusplus
}
#endif