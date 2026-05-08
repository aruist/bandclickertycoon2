//
//  SCCloudServicesHandler.m
//

#import "SCCloudServicesHandler.h"

@implementation SCCloudServicesHandler

#define kKeyForValueChangedKeys					@"keys"
#define kKeyForChangeReason						@"reason"
#define kKeyValuePairChangedExternallyEvent	 	"CloudKeyValueStoreDidChangeExternally"

- (id)init
{
	NSLog(@"SCCloudServicesHandler init");
	self	= [super init];
	
	if (self)
	{
		// Register for events
		NSUbiquitousKeyValueStore *store = [NSUbiquitousKeyValueStore defaultStore];
		
		[[NSNotificationCenter defaultCenter] addObserver:self
												 selector:@selector(iCloudKeyValueStoreDidChange:)
													 name:NSUbiquitousKeyValueStoreDidChangeExternallyNotification
												   object:store];
	}
	
	return self;
}

- (void)dealloc
{
	// Unregister if already registered
	[[NSNotificationCenter  defaultCenter]  removeObserver:self];
	
	[super dealloc];
}

#pragma mark - Event Callbacks

- (void)iCloudKeyValueStoreDidChange:(NSNotification *)notification
{
	NSLog(@"iCloudKeyValueStoreDidChange");
	NSDictionary *userInfo	= [notification userInfo];
	NSNumber *changeReason	= [userInfo objectForKey:NSUbiquitousKeyValueStoreChangeReasonKey];
	
	// Check if value is valid
	if (!changeReason)
		return;
	
	NSArray *changedKeys	= [userInfo objectForKey:NSUbiquitousKeyValueStoreChangedKeysKey];
	
	// Notify Unity
	NSMutableDictionary *dataDict	= [NSMutableDictionary dictionary];
	
	[dataDict setObject:changeReason forKey:kKeyForChangeReason];
	
	if (changedKeys)
		[dataDict setObject:changedKeys forKey:kKeyForValueChangedKeys];
	
	//NotifyEventListener(kKeyValuePairChangedExternallyEvent, ToJsonCString(dataDict));
    UnitySendMessage("SCPlugins", kKeyValuePairChangedExternallyEvent, ToJsonCString(dataDict));
}

@end
