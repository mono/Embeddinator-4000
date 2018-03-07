/*
 * String functions
 *
 * Author:
 *   Miguel de Icaza (miguel@novell.com)
 *   Aaron Bockover (abockover@novell.com)
 *
 * (C) 2006 Novell, Inc.
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
#include <stdio.h>
#include "glib.h"

void g_free (void *ptr)
{
  free(ptr);
}

gpointer g_realloc (gpointer obj, gsize size)
{
  return realloc(obj, size);
}

gpointer g_malloc (gsize x)
{
  return malloc(x);
}

gpointer g_malloc0 (gsize x)
{
  return calloc(1, x);
}

gpointer
g_memdup (gconstpointer mem, guint byte_size)
{
  gpointer ptr;

  if (mem == NULL)
    return NULL;

  ptr = g_malloc (byte_size);
  if (ptr != NULL)
    memcpy (ptr, mem, byte_size);

  return ptr;
}

gchar   *
g_strdup (const gchar *str)
{
  if (str) { return (gchar*) g_memdup (str, (guint)strlen (str) + 1); }
  return NULL;
}

void
g_assertion_message (const gchar *format, ...)
{
  va_list args;

  va_start (args, format);
  fprintf (stderr, format, args);
  va_end (args);
  exit (0);
}

#define INITIAL_CAPACITY 16

#define element_offset(p,i) ((p)->array.data + (i) * (p)->element_size)
#define element_length(p,i) ((i) * (p)->element_size)

typedef struct {
  GArray array;
  gboolean clear_;
  guint element_size;
  gboolean zero_terminated;
  guint capacity;
} GArrayPriv;

static void
ensure_capacity (GArrayPriv *priv, guint capacity)
{
  guint new_capacity;
  
  if (capacity <= priv->capacity)
    return;
  
  new_capacity = (capacity + 63) & ~63;
  
  priv->array.data = (gchar*) g_realloc (priv->array.data, element_length (priv, new_capacity));
  
  if (priv->clear_) {
    memset (element_offset (priv, priv->capacity),
      0,
      element_length (priv, new_capacity - priv->capacity));
  }
  
  priv->capacity = new_capacity;
}

GArray *
g_array_new (gboolean zero_terminated,
       gboolean clear_,
       guint element_size)
{
  GArrayPriv *rv = g_new0 (GArrayPriv, 1);
  rv->zero_terminated = zero_terminated;
  rv->clear_ = clear_;
  rv->element_size = element_size;

  ensure_capacity (rv, INITIAL_CAPACITY);

  return (GArray*)rv;
}

GArray *
g_array_sized_new (gboolean zero_terminated,
       gboolean clear_,
       guint element_size,
     guint reserved_size)
{
  GArrayPriv *rv = g_new0 (GArrayPriv, 1);
  rv->zero_terminated = zero_terminated;
  rv->clear_ = clear_;
  rv->element_size = element_size;

  ensure_capacity (rv, reserved_size);

  return (GArray*)rv;
}

gchar*
g_array_free (GArray *array,
        gboolean free_segment)
{
  gchar* rv = NULL;

  g_return_val_if_fail (array != NULL, NULL);

  if (free_segment)
    g_free (array->data);
  else
    rv = array->data;

  g_free (array);

  return rv;
}

GArray *
g_array_append_vals (GArray *array,
         gconstpointer data,
         guint len)
{
  GArrayPriv *priv = (GArrayPriv*)array;

  g_return_val_if_fail (array != NULL, NULL);

  ensure_capacity (priv, priv->array.len + len + (priv->zero_terminated ? 1 : 0));
  
  memmove (element_offset (priv, priv->array.len),
     data,
     element_length (priv, len));

  priv->array.len += len;

  if (priv->zero_terminated) {
    memset (element_offset (priv, priv->array.len),
      0,
      priv->element_size);
  }

  return array;
}

GArray*
g_array_insert_vals (GArray *array,
         guint index_,
         gconstpointer data,
         guint len)
{
  GArrayPriv *priv = (GArrayPriv*)array;
  guint extra = (priv->zero_terminated ? 1 : 0);

  g_return_val_if_fail (array != NULL, NULL);

  ensure_capacity (priv, array->len + len + extra);
  
  /* first move the existing elements out of the way */
  memmove (element_offset (priv, index_ + len),
     element_offset (priv, index_),
     element_length (priv, array->len - index_));

  /* then copy the new elements into the array */
  memmove (element_offset (priv, index_),
     data,
     element_length (priv, len));

  array->len += len;

  if (priv->zero_terminated) {
    memset (element_offset (priv, priv->array.len),
      0,
      priv->element_size);
  }

  return array;
}

GArray*
g_array_remove_index (GArray *array,
          guint index_)
{
  GArrayPriv *priv = (GArrayPriv*)array;

  g_return_val_if_fail (array != NULL, NULL);

  memmove (element_offset (priv, index_),
     element_offset (priv, index_ + 1),
     element_length (priv, array->len - index_));

  array->len --;

  if (priv->zero_terminated) {
    memset (element_offset (priv, priv->array.len),
      0,
      priv->element_size);
  }

  return array;
}

GArray*
g_array_remove_index_fast (GArray *array,
          guint index_)
{
  GArrayPriv *priv = (GArrayPriv*)array;

  g_return_val_if_fail (array != NULL, NULL);

  memmove (element_offset (priv, index_),
     element_offset (priv, array->len - 1),
     element_length (priv, 1));

  array->len --;

  if (priv->zero_terminated) {
    memset (element_offset (priv, priv->array.len),
      0,
      priv->element_size);
  }

  return array;
}

void
g_array_set_size (GArray *array, gint length)
{
  GArrayPriv *priv = (GArrayPriv*)array;

  g_return_if_fail (array != NULL);
  g_return_if_fail (length >= 0);

  if (length == priv->capacity)
    return; // nothing to be done

  if (length > priv->capacity) {
    // grow the array
    ensure_capacity (priv, length);
  }

  array->len = length;
}

#define GROW_IF_NECESSARY(s,l) { \
  if(s->len + l >= s->allocated_len) { \
    s->allocated_len = (s->allocated_len + l + 16) * 2; \
    s->str = (char*)g_realloc(s->str, s->allocated_len); \
  } \
}

void
g_string_null (GString *string)
{
  if (string->str)
      g_free(string->str);

  string->str = 0;
  string->len = 0;
  string->allocated_len = 0;
}

GString *
g_string_new_len (const gchar *init, gssize len)
{
  GString *ret = g_new (GString, 1);

  if (init == NULL) {
    ret->len = 0;
    ret->allocated_len = 0;
    ret->str = 0;
    return ret;
  }
  
  ret->len = len < 0 ? strlen(init) : len;
  ret->allocated_len = MAX(ret->len + 1, 16);
  ret->str = (char*)g_malloc(ret->allocated_len);
  if (init)
    memcpy(ret->str, init, ret->len);
  ret->str[ret->len] = 0;

  return ret;
}

GString *
g_string_new (const gchar *init)
{
  return g_string_new_len(init, -1);
}

GString *
g_string_sized_new (gsize default_size)
{
  GString *ret = g_new (GString, 1);

  ret->str = (char*)g_malloc (default_size);
  ret->str [0] = 0;
  ret->len = 0;
  ret->allocated_len = default_size;

  return ret;
}

gchar *
g_string_free (GString *string, gboolean free_segment)
{
  gchar *data;
  
  g_return_val_if_fail (string != NULL, NULL);

  data = string->str;
  g_free(string);
  
  if(!free_segment) {
    return data;
  }

  g_free(data);
  return NULL;
}

GString *
g_string_append_len (GString *string, const gchar *val, gssize len)
{
  g_return_val_if_fail(string != NULL, NULL);
  g_return_val_if_fail(val != NULL, string);

  if(len < 0) {
    len = strlen(val);
  }

  GROW_IF_NECESSARY(string, len);
  memcpy(string->str + string->len, val, len);
  string->len += len;
  string->str[string->len] = 0;

  return string;
}

GString *
g_string_append (GString *string, const gchar *val)
{
  g_return_val_if_fail(string != NULL, NULL);
  g_return_val_if_fail(val != NULL, string);

  return g_string_append_len(string, val, -1);
}

GString *
g_string_append_c (GString *string, gchar c)
{
  g_return_val_if_fail(string != NULL, NULL);

  GROW_IF_NECESSARY(string, 1);
  
  string->str[string->len] = c;
  string->str[string->len + 1] = 0;
  string->len++;

  return string;
}

GString *
g_string_prepend (GString *string, const gchar *val)
{
  gssize len;
  
  g_return_val_if_fail (string != NULL, string);
  g_return_val_if_fail (val != NULL, string);

  len = strlen (val);
  
  GROW_IF_NECESSARY(string, len); 
  memmove(string->str + len, string->str, string->len + 1);
  memcpy(string->str, val, len);

  return string;
}

GString *
g_string_insert (GString *string, gssize pos, const gchar *val)
{
  gssize len;
  
  g_return_val_if_fail (string != NULL, string);
  g_return_val_if_fail (val != NULL, string);
  g_return_val_if_fail (pos <= string->len, string);

  len = strlen (val);
  
  GROW_IF_NECESSARY(string, len); 
  memmove(string->str + pos + len, string->str + pos, string->len - pos - len + 1);
  memcpy(string->str + pos, val, len);

  return string;
}

GString *
g_string_truncate (GString *string, gsize len)
{
  g_return_val_if_fail (string != NULL, string);

  /* Silent return */
  if (len >= string->len)
    return string;
  
  string->len = len;
  string->str[len] = 0;
  return string;
}

GString *
g_string_set_size (GString *string, gsize len)
{
  g_return_val_if_fail (string != NULL, string);

  GROW_IF_NECESSARY(string, len);
  
  string->len = len;
  string->str[len] = 0;
  return string;
}

GString *
g_string_erase (GString *string, gssize pos, gssize len)
{
  g_return_val_if_fail (string != NULL, string);

  /* Silent return */
  if (pos >= string->len)
    return string;

  if (len == -1 || (pos + len) >= string->len) {
    string->str[pos] = 0;
  }
  else {
    memmove (string->str + pos, string->str + pos + len, string->len - (pos + len) + 1);
    string->len -= len;
  }

  return string;
}