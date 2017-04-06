#include <stdio.h>
#include <sys/types.h>
#include <sys/event.h>
#include <sys/time.h>
#include <Security/Security.h>

int main (int argc, char* argv[])
{
	AuthorizationItem item;
	item.flags = 0;
	item.name = "system.privilege.taskport.debug";
	item.value = NULL;
	item.valueLength = 0;
	AuthorizationRights rights = { 1, &item };
	AuthorizationFlags flags = 50; // kAuthorizationFlagExtendRights | kAuthorizationFlagInteractionAllowed | kAuthorizationFlagPreAuthorize;

	OSStatus status = AuthorizationCreate (&rights, NULL, flags, NULL);
	printf ("create status: %i flags: 0x%x=%i\n", status, flags, flags);
	return 0;
}
