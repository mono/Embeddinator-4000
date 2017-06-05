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

#if !defined(MONO_API_DEF)
#define MONO_API_DEF(name)
#endif

MONO_API_DEF(mono_method_desc_new)
MONO_API_DEF(mono_method_desc_free)
MONO_API_DEF(mono_method_desc_search_in_class)
MONO_API_DEF(mono_jit_cleanup)
MONO_API_DEF(mono_jit_init_version)
MONO_API_DEF(mono_domain_assembly_open)
MONO_API_DEF(mono_domain_get)
MONO_API_DEF(mono_string_length)
MONO_API_DEF(mono_string_chars)
MONO_API_DEF(mono_field_get_value_object)
MONO_API_DEF(mono_field_set_value)
MONO_API_DEF(mono_class_vtable)
MONO_API_DEF(mono_field_static_set_value)
MONO_API_DEF(mono_object_to_string)
MONO_API_DEF(mono_class_get)
MONO_API_DEF(mono_class_get_field)
MONO_API_DEF(mono_get_string_class)
MONO_API_DEF(mono_get_boolean_class)
MONO_API_DEF(mono_get_char_class)
MONO_API_DEF(mono_get_sbyte_class)
MONO_API_DEF(mono_get_int16_class)
MONO_API_DEF(mono_get_int32_class)
MONO_API_DEF(mono_get_int64_class)
MONO_API_DEF(mono_get_byte_class)
MONO_API_DEF(mono_get_uint16_class)
MONO_API_DEF(mono_get_uint32_class)
MONO_API_DEF(mono_get_uint64_class)
MONO_API_DEF(mono_get_single_class)
MONO_API_DEF(mono_get_double_class)
MONO_API_DEF(mono_array_element_size)

#undef MONO_API_DEF