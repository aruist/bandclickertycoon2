//
//  SCSingletonContainer.h
//

#import <Foundation/Foundation.h>

@interface SCSingletonContainer : NSObject

// Properties
@property(nonatomic, retain)	NSMutableDictionary		*instanceContainer;

// Static instance
+ (id)GetSingletonInstance:(Class)class;

@end
