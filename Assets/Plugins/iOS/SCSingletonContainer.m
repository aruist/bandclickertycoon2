//
//  SCSingletonContainer.m
//

#import "SCSingletonContainer.h"

static SCSingletonContainer* sharedInstance = nil;

@implementation SCSingletonContainer

#pragma Properties

@synthesize instanceContainer;

#pragma Single Instance Methods

+ (SCSingletonContainer *)SharedInstance
{
    @synchronized([SCSingletonContainer class])
    {
        if (!sharedInstance)
            [[self alloc] init];
		
        return sharedInstance;
    }
	
    return nil;
}

+ (id)alloc
{
    @synchronized([SCSingletonContainer class])
    {
        NSAssert(sharedInstance == nil, @"Attempted to allocate a second instance of a singleton.");
        sharedInstance = [super alloc];
		
        return sharedInstance;
    }
	
    return nil;
}

- (id)init
{
	self	= [super init];
	
	if (self)
	{
		self.instanceContainer	= [NSMutableDictionary dictionary];
	}
	
	return self;
}

- (void)dealloc
{
	// Release
	self.instanceContainer		= nil;
	
	[super dealloc];
}

#pragma mark - Container Methods

+ (id)GetSingletonInstance:(Class)class
{
	// Get instance from container
	NSMutableDictionary *instanceContainer	= [[SCSingletonContainer SharedInstance] instanceContainer];
	NSString *className						= NSStringFromClass(class);
	id classInstance						= [instanceContainer objectForKey:className];
	
	@synchronized(class)
    {
        if (!classInstance)
		{
			// Create new instance
            classInstance	= [[[class alloc] init] autorelease];
			
			// Cache it in instance container
			[instanceContainer setObject:classInstance forKey:className];
		}
		
        return classInstance;
    }
	
	return NULL;
}

@end
