/** Installs an hook that returns the path to the given assembly */
typedef const char* (*mono_managed_to_native_search_hook_t)(const char*);
void* mono_managed_to_native_install_search_hook(mono_managed_to_native_search_hook_t hook);



/** Searches and returns the path to the given assembly */
char* mono_managed_to_native_search_assembly(const char* assembly);
