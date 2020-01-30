#ifndef Biometrics_h
#define Biometrics_h

@interface Biometrics : NSObject {
    NSString *_objectName;
    NSString *_localizedAuthReason;
}
- (Biometrics *)initWithUnityObjectName:(NSString *)objectName withAuthReason:(NSString *)reason;
- (Biometrics *)init;
- (void) setCurrentAuthReason:(NSString *)reason;
- (NSString *)getCurrentAuthReason;
- (void) authenticate;

@end

#endif Biometrics_h
