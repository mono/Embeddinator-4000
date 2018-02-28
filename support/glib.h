#ifndef __GLIB_H
#define __GLIB_H
#include <stdarg.h>
#include <stdlib.h>
#include <string.h>
#include <stdio.h>
#include <stddef.h>
#include <ctype.h>
#include <limits.h>

#include <stdint.h>

#ifdef G_HAVE_ALLOCA_H
#include <alloca.h>
#endif

#ifdef WIN32
/* For alloca */
#include <malloc.h>
#endif

#ifndef offsetof
#   define offsetof(s_name,n_name) (size_t)(char *)&(((s_name*)0)->m_name)
#endif

#define __EGLIB_X11 1

#ifdef  __cplusplus
#define G_BEGIN_DECLS  extern "C" {
#define G_END_DECLS    }
#else
#define G_BEGIN_DECLS
#define G_END_DECLS
#endif

G_BEGIN_DECLS

/*
 * Basic data types
 */
typedef int            gint;
typedef unsigned int   guint;
typedef short          gshort;
typedef unsigned short gushort;
typedef long           glong;
typedef unsigned long  gulong;
typedef void *         gpointer;
typedef const void *   gconstpointer;
typedef char           gchar;
typedef unsigned char  guchar;

/* Types defined in terms of the stdint.h */
typedef int8_t         gint8;
typedef uint8_t        guint8;
typedef int16_t        gint16;
typedef uint16_t       guint16;
typedef int32_t        gint32;
typedef uint32_t       guint32;
typedef int64_t        gint64;
typedef uint64_t       guint64;
typedef float          gfloat;
typedef double         gdouble;
typedef int32_t        gboolean;

typedef guint16 gunichar2;
typedef guint32 gunichar;

typedef unsigned long gsize;
typedef signed long gssize;

/*
 * Macros
 */
#define G_N_ELEMENTS(s)      (sizeof(s) / sizeof ((s) [0]))

#define FALSE                0
#define TRUE                 1

#define G_MINSHORT           SHRT_MIN
#define G_MAXSHORT           SHRT_MAX
#define G_MAXUSHORT          USHRT_MAX
#define G_MAXINT             INT_MAX
#define G_MININT             INT_MIN
#define G_MAXINT32           INT32_MAX
#define G_MAXUINT32          UINT32_MAX
#define G_MININT32           INT32_MIN
#define G_MININT64           INT64_MIN
#define G_MAXINT64	     INT64_MAX
#define G_MAXUINT64	     UINT64_MAX

#define G_LITTLE_ENDIAN 1234
#define G_BIG_ENDIAN    4321
#define G_STMT_START    do 
#define G_STMT_END      while (0)

#define G_USEC_PER_SEC  1000000

#ifndef ABS
#define ABS(a)         ((a) > 0 ? (a) : -(a))
#endif

#define G_STRUCT_OFFSET(p_type,field) offsetof(p_type,field)

#define EGLIB_STRINGIFY(x) #x
#define EGLIB_TOSTRING(x) EGLIB_STRINGIFY(x)
#define G_STRLOC __FILE__ ":" EGLIB_TOSTRING(__LINE__) ":"

#define G_CONST_RETURN const

#define G_GUINT64_FORMAT PRIu64
#define G_GINT64_FORMAT PRIi64
#define G_GUINT32_FORMAT PRIu32
#define G_GINT32_FORMAT PRIi32

/*
 * Allocation
 */
void g_free (void *ptr);
gpointer g_realloc (gpointer obj, gsize size);
gpointer g_malloc (gsize x);
gpointer g_malloc0 (gsize x);
gpointer g_calloc (gsize n, gsize x);
gpointer g_try_malloc (gsize x);
gpointer g_try_realloc (gpointer obj, gsize size);

#define g_new(type,size)        ((type *) g_malloc (sizeof (type) * (size)))
#define g_new0(type,size)       ((type *) g_malloc0 (sizeof (type)* (size)))
#define g_newa(type,size)       ((type *) alloca (sizeof (type) * (size)))

#define g_memmove(dest,src,len) memmove (dest, src, len)
#define g_renew(struct_type, mem, n_structs) g_realloc (mem, sizeof (struct_type) * n_structs)
#define g_alloca(size)		alloca (size)

gpointer g_memdup (gconstpointer mem, guint byte_size);
gchar   *g_strdup (const gchar *str);
gchar **g_strdupv (gchar **str_array);

/*
 * Precondition macros
 */
#define g_warn_if_fail(x)  G_STMT_START { if (!(x)) { g_warning ("%s:%d: assertion '%s' failed", __FILE__, __LINE__, #x); } } G_STMT_END
#define g_return_if_fail(x)  G_STMT_START { if (!(x)) { g_critical ("%s:%d: assertion '%s' failed", __FILE__, __LINE__, #x); return; } } G_STMT_END
#define g_return_val_if_fail(x,e)  G_STMT_START { if (!(x)) { g_critical ("%s:%d: assertion '%s' failed", __FILE__, __LINE__, #x); return (e); } } G_STMT_END

/*
 * Array
 */

typedef struct _GArray GArray;
struct _GArray {
	gchar *data;
	gint len;
};

GArray *g_array_new               (gboolean zero_terminated, gboolean clear_, guint element_size);
GArray *g_array_sized_new         (gboolean zero_terminated, gboolean clear_, guint element_size, guint reserved_size);
gchar*  g_array_free              (GArray *array, gboolean free_segment);
GArray *g_array_append_vals       (GArray *array, gconstpointer data, guint len);
GArray* g_array_insert_vals       (GArray *array, guint index_, gconstpointer data, guint len);
GArray* g_array_remove_index      (GArray *array, guint index_);
GArray* g_array_remove_index_fast (GArray *array, guint index_);
void    g_array_set_size          (GArray *array, gint length);

#define g_array_append_val(a,v)   (g_array_append_vals((a),&(v),1))
#define g_array_insert_val(a,i,v) (g_array_insert_vals((a),(i),&(v),1))
#define g_array_index(a,t,i)      *(t*)(((a)->data) + sizeof(t) * (i))

/*
 * String type
 */
typedef struct _GString {
	char *str;
	gsize len;
	gsize allocated_len;
} GString;

GString     *g_string_new           (const gchar *init);
GString     *g_string_new_len       (const gchar *init, gssize len);
GString     *g_string_sized_new     (gsize default_size);
gchar       *g_string_free          (GString *string, gboolean free_segment);
GString     *g_string_append        (GString *string, const gchar *val);
GString     *g_string_append_c      (GString *string, gchar c);
GString     *g_string_append        (GString *string, const gchar *val);
GString     *g_string_append_len    (GString *string, const gchar *val, gssize len);
GString     *g_string_truncate      (GString *string, gsize len);
GString     *g_string_prepend       (GString *string, const gchar *val);
GString     *g_string_insert        (GString *string, gssize pos, const gchar *val);
GString     *g_string_set_size      (GString *string, gsize len);
GString     *g_string_erase         (GString *string, gssize pos, gssize len);
void         g_string_null          (GString *string);

#define g_string_sprintfa g_string_append_printf

#ifndef MAX
#define MAX(a,b) (((a)>(b)) ? (a) : (b))
#endif

#ifndef MIN
#define MIN(a,b) (((a)<(b)) ? (a) : (b))
#endif

#ifndef CLAMP
#define CLAMP(a,low,high) (((a) < (low)) ? (low) : (((a) > (high)) ? (high) : (a)))
#endif

#if defined(_MSC_VER)
#define  eg_unreachable() __assume(0)
#elif defined(__GNUC__) && ((__GNUC__ > 4) || (__GNUC__ == 4 && (__GNUC_MINOR__ >= 5)))
#define  eg_unreachable() __builtin_unreachable()
#else
#define  eg_unreachable()
#endif

void g_assertion_message (const gchar *format, ...);

#define  g_assert(x)     G_STMT_START { if (G_UNLIKELY (!(x))) g_assertion_message ("* Assertion at %s:%d, condition `%s' not met\n", __FILE__, __LINE__, #x);  } G_STMT_END
#define  g_assert_not_reached() G_STMT_START { g_assertion_message ("* Assertion: should not be reached at %s:%d\n", __FILE__, __LINE__); eg_unreachable(); } G_STMT_END

#define g_critical(format, ...) 

/*
 * Path
 */
#ifdef _MSC_VER
#define G_DIR_SEPARATOR          '\\'
#define G_DIR_SEPARATOR_S        "\\"
#else
#define G_DIR_SEPARATOR          '/'
#define G_DIR_SEPARATOR_S        "/"
#endif

gchar  *g_build_path           (const gchar *separator, const gchar *first_element, ...);
#define g_build_filename(x, ...) g_build_path(G_DIR_SEPARATOR_S, x, __VA_ARGS__)
gchar  *g_path_get_dirname     (const gchar *filename);
gchar  *g_path_get_basename    (const char *filename);
gchar  *g_find_program_in_path (const gchar *program);
gchar  *g_get_current_dir      (void);
gboolean g_path_is_absolute    (const char *filename);

#define _EGLIB_MAJOR  2
#define _EGLIB_MIDDLE 4
#define _EGLIB_MINOR  0
 
#define GLIB_CHECK_VERSION(a,b,c) ((a < _EGLIB_MAJOR) || (a == _EGLIB_MAJOR && (b < _EGLIB_MIDDLE || (b == _EGLIB_MIDDLE && c <= _EGLIB_MINOR))))
 
G_END_DECLS

#endif


