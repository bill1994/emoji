#import <LocalAuthentication/LocalAuthentication.h>
#import "Biometrics.h"

static Biometrics *biometrics = nil;
static LAContext *context = nil;
static NSString *methodName = @"OnAuthenticationBridgeDidFinish";

extern void UnitySendMessage(const char*, const char*, const char*);

@implementation Biometrics

- (Biometrics *)init {
    if (self = [super init]) {
        context = [[LAContext alloc] init];
    }
    return self;
}


- (Biometrics *)initWithUnityObjectName:(NSString *)objectName withAuthReason:(NSString *)reason {
    if (self = [super init]) {
        context = [[LAContext alloc] init];
        context.localizedFallbackTitle = @"";
        _localizedAuthReason = reason;
        _objectName = objectName;
    }
    return self;
}

- (NSString *)getCurrentAuthReason {
    return _localizedAuthReason;
}

- (void) setCurrentAuthReason:(NSString *)reason {
    _localizedAuthReason = reason;
}

- (void) authenticate 
{
    NSError *authError = nil;
    
    if ([context canEvaluatePolicy:LAPolicyDeviceOwnerAuthenticationWithBiometrics error: &authError]) {
        [context evaluatePolicy:LAPolicyDeviceOwnerAuthenticationWithBiometrics localizedReason:_localizedAuthReason reply:^(BOOL success, NSError * _Nullable error) {
            NSString *errorMsg = (error != nil) ? error.localizedDescription : @"";
            UnitySendMessage([_objectName UTF8String], [methodName UTF8String], [errorMsg UTF8String]);
        }];
    } else {
        NSString *errorMsg = @"Authentication policy cannot be evaluated.";
        UnitySendMessage([_objectName UTF8String], [methodName UTF8String], [errorMsg UTF8String]);
    }
}

@end

// Helpers
NSString *BiometricsCreateNSString (const char* string) {
    if (string) {
        return [NSString stringWithUTF8String:string];
    }else {
        return [NSString stringWithUTF8String:""];
    }
}

char* BiometricsMakeStringCopy (const char* string) {
    if (string == NULL)
        return NULL;
    
    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    return res;
}

// External Methods...
extern "C" {
    void _StartBiometricsAuth(const char* objectName, const char* authenticationReason)
    {
        if (biometrics == nil) {
            biometrics = [[Biometrics alloc] initWithUnityObjectName:BiometricsCreateNSString(objectName) withAuthReason:BiometricsCreateNSString(authenticationReason)];
        }
        
        [biometrics authenticate];
    }
    
    const char* _GetAuthenticationReason() {
        return BiometricsMakeStringCopy([[biometrics getCurrentAuthReason] UTF8String]);
    }
    
    int _SetAuthenticationReason(const char* reason) {
        if (biometrics == nil)
            return 0;
        
        [biometrics setCurrentAuthReason:BiometricsCreateNSString(reason)];
        return 1;
    }
    
}