//
//  SCCloudServicesBinding.h
//

#import <Foundation/Foundation.h>

// Initialise
UIKIT_EXTERN void scCloudServicesInitialise ();

// Setting values
UIKIT_EXTERN void scCloudServicesSetBool (const char *key, bool value);
UIKIT_EXTERN void scCloudServicesSetLong (const char *key, long value);
UIKIT_EXTERN void scCloudServicesSetDouble (const char *key, double value);
UIKIT_EXTERN void scCloudServicesSetString (const char *key, const char *value);
UIKIT_EXTERN void scCloudServicesSetList (const char *key, const char *value);
UIKIT_EXTERN void scCloudServicesSetDictionary (const char *key, const char *value);

// Gettings values
UIKIT_EXTERN bool scCloudServicesGetBool (const char *key);
UIKIT_EXTERN long scCloudServicesGetLong (const char *key);
UIKIT_EXTERN double scCloudServicesGetDouble (const char *key);
UIKIT_EXTERN char* scCloudServicesGetString (const char *key);
UIKIT_EXTERN char* scCloudServicesGetList (const char *key);
UIKIT_EXTERN char* scCloudServicesGetDictionary (const char *key);

// Synchronise
UIKIT_EXTERN bool scCloudServicesSynchronise ();

// Removing values
UIKIT_EXTERN void scCloudServicesRemoveKey (const char *key);
