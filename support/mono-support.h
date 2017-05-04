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

#if defined (XAMARIN_IOS) || defined (XAMARIN_MAC)
#include <xamarin/xamarin.h>
typedef uint16_t    mono_unichar2;

typedef struct _MonoMethodDesc MonoMethodDesc;
typedef uint16_t    mono_unichar2;
typedef struct _GArray GArray;

MONO_EMBEDDINATOR_BEGIN_DECLS
MonoMethodDesc* mono_method_desc_new (const char *name, mono_bool include_namespace);
void            mono_method_desc_free (MonoMethodDesc *desc);
MonoMethod*     mono_method_desc_search_in_class (MonoMethodDesc *desc, MonoClass *klass);
void            mono_jit_cleanup           (MonoDomain *domain);
MonoAssembly *  mono_domain_assembly_open  (MonoDomain *domain, const char *name);
int             mono_string_length (MonoString *s);
mono_unichar2 * mono_string_chars  (MonoString *s);
MonoObject *    mono_field_get_value_object (MonoDomain *domain, MonoClassField *field, MonoObject *obj);
void            mono_field_set_value (MonoObject *obj, MonoClassField *field, void *value);
MonoVTable *    mono_class_vtable          (MonoDomain *domain, MonoClass *klass);
void            mono_field_static_set_value (MonoVTable *vt, MonoClassField *field, void *value);
MonoString *    mono_object_to_string (MonoObject *obj, MonoObject **exc);
MonoClass *     mono_class_get (MonoImage *image, uint32_t type_token);
MonoMethod *    mono_get_method (MonoImage *image, uint32_t token, MonoClass *klass);
MonoClassField* mono_class_get_field (MonoClass *klass, uint32_t field_token);

MONO_EMBEDDINATOR_END_DECLS
#else
#include <mono/jit/jit.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/assembly.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/metadata/object.h>

typedef uint16_t gunichar2;

typedef struct _GArray GArray;
typedef struct _GString GString;

typedef struct _MonoDomain MonoDomain;
typedef struct _MonoException MonoException;
typedef struct _MonoClass MonoClass;
typedef struct _MonoObject MonoObject;
typedef struct _MonoImage MonoImage;
typedef struct _MonoMethod MonoMethod;
#endif
