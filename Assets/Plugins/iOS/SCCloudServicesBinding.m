//
//  SCCloudServicesBinding.m
//

#import "SCCloudServicesBinding.h"
#import "SCCloudServicesHandler.h"

#pragma mark - Initialise

void scCloudServicesInitialise ()
{
	NSLog(@"scCloudServicesInitialise");
	[SCCloudServicesHandler Instance];
}

#pragma mark - Setting values

void scCloudServicesSetBool (const char *key, bool value)
{
	[[NSUbiquitousKeyValueStore defaultStore] setBool:value
											   forKey:ConvertToNSString(key)];
}

void scCloudServicesSetLong (const char *key, long value)
{
	[[NSUbiquitousKeyValueStore defaultStore] setLongLong:value
												   forKey:ConvertToNSString(key)];
}

void scCloudServicesSetDouble (const char *key, double value)
{
	NSLog(@"scCloudServicesSetDouble");
	[[NSUbiquitousKeyValueStore defaultStore] setDouble:value
												 forKey:ConvertToNSString(key)];
}

void scCloudServicesSetString (const char *key, const char *value)
{
	[[NSUbiquitousKeyValueStore defaultStore] setString:ConvertToNSString(value)
												 forKey:ConvertToNSString(key)];
}

void scCloudServicesSetList (const char *key, const char *value)
{
	[[NSUbiquitousKeyValueStore defaultStore] setArray:FromJson(value)
												forKey:ConvertToNSString(key)];
}

void scCloudServicesSetDictionary (const char *key, const char *value)
{
	[[NSUbiquitousKeyValueStore defaultStore] setDictionary:FromJson(value)
													 forKey:ConvertToNSString(key)];
}

#pragma mark -  Gettings values

bool scCloudServicesGetBool (const char *key)
{
	return [[NSUbiquitousKeyValueStore defaultStore] boolForKey:ConvertToNSString(key)];
}

long scCloudServicesGetLong (const char *key)
{
	return [[NSUbiquitousKeyValueStore defaultStore] longLongForKey:ConvertToNSString(key)];
}

double scCloudServicesGetDouble (const char *key)
{
	return [[NSUbiquitousKeyValueStore defaultStore] doubleForKey:ConvertToNSString(key)];
}

char* scCloudServicesGetString (const char *key)
{
	NSString *value	= [[NSUbiquitousKeyValueStore defaultStore] stringForKey:ConvertToNSString(key)];
	
	return value ? CStringCopy([value UTF8String]) : NULL;
}

char* scCloudServicesGetList (const char *key)
{
	NSArray	*value	= [[NSUbiquitousKeyValueStore defaultStore] arrayForKey:ConvertToNSString(key)];
	
	return value ? CStringCopy(ToJsonCString(value)) : NULL;
}

char* scCloudServicesGetDictionary (const char *key)
{
	NSDictionary *value	= [[NSUbiquitousKeyValueStore defaultStore] dictionaryForKey:ConvertToNSString(key)];
	
	return value ? CStringCopy(ToJsonCString(value)) : NULL;
}

#pragma mark - Synchronise

bool scCloudServicesSynchronise ()
{
	NSLog(@"scCloudServicesSynchronise");
	return [[NSUbiquitousKeyValueStore defaultStore] synchronize];
}

#pragma mark - Removing values

void scCloudServicesRemoveKey (const char *key)
{
	[[NSUbiquitousKeyValueStore defaultStore] removeObjectForKey:ConvertToNSString(key)];
}

