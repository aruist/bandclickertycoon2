//
//  SCSingleton.m
//

#import "SCSingleton.h"
#import "SCSingletonContainer.h"

@implementation SCSingleton

+ (id)Instance
{
    return [SCSingletonContainer GetSingletonInstance:self];
}

@end
