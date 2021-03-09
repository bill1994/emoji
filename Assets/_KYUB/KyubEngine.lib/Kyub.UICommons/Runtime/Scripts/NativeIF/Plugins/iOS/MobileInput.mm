// ----------------------------------------------------------------------------
// The MIT License
// UnityMobileInput https://github.com/mopsicus/UnityMobileInput
// Copyright (c) 2018 Mopsicus <mail@mopsicus.ru>
// ----------------------------------------------------------------------------

#import <UIKit/UIKit.h>
#import <Foundation/Foundation.h>
#import "UnityForwardDecls.h"
#import <MobileCoreServices/UTCoreTypes.h>
#import "MobileInputCommon.h"
#import "IQKeyboardManager.h"

#define CREATE @"CREATE_EDIT"
#define REMOVE @"REMOVE_EDIT"
#define SET_TEXT @"SET_TEXT"
#define SET_RECT @"SET_RECT"
#define ON_FOCUS @"ON_FOCUS"
#define ON_UNFOCUS @"ON_UNFOCUS"
#define SET_FOCUS @"SET_FOCUS"
#define SET_VISIBLE @"SET_VISIBLE"
#define TEXT_CHANGE @"TEXT_CHANGE"
#define TEXT_END_EDIT @"TEXT_END_EDIT"
#define DONE_KEY_DOWN @"DONE_KEY_DOWN"
#define RETURN_PRESSED @"RETURN_PRESSED"
#define KEYBOARD_ACTION @"KEYBOARD_ACTION"
#define KEYBOARD_PREPARE @"KEYBOARD_PREPARE"
#define READY @"READY"

UIViewController *mainViewController = nil;
NSMutableDictionary *mobileInputList = nil;
NSString *plugin;

//
//
//

@interface MobileInput : NSObject <UITextFieldDelegate, UITextViewDelegate> {
    int inputId;
    int characterLimit;
    CGRect rectKeyboardFrame;
    UIView *editView;
    UIViewController *viewController;
    UIToolbar *keyboardDoneButtonView;
    UIBarButtonItem *doneButton;
}
+ (void)init:(UIViewController *)viewController;

+ (void)processMessage:(int)inputId data:(NSString *)data;

//+ (BOOL)hasActiveSelection:(int)inputId;

+ (void)destroy;

- (id)initWith:(UIViewController *)controller andTag:(int)inputId;

- (void)create:(NSDictionary *)data;

- (void)processData:(NSDictionary *)data;

- (void)showKeyboard:(BOOL)value;

- (BOOL)isFocused;
@end

@implementation MobileInput

+ (void)init:(UIViewController *)viewController {
    mainViewController = viewController;
    mobileInputList = [[NSMutableDictionary alloc] init];
}

+ (void)processMessage:(int)inputId data:(NSString *)data {
    NSDictionary *message = [MobileInputCommon jsonToDict:data];
    NSString *msg = [message valueForKey:@"msg"];
    if ([msg isEqualToString:CREATE]) {
        MobileInput *input = [[MobileInput alloc] initWith:mainViewController andTag:inputId];
        [input create:message];
        [mobileInputList setObject:input forKey:[NSNumber numberWithInt:inputId]];
    } else {
        MobileInput *input = [mobileInputList objectForKey:[NSNumber numberWithInt:inputId]];
        if (input) {
            [input processData:message];
        }
    }
}

//+ (BOOL)hasActiveSelection:(int)inputId {
//	
//	if(mobileInputList != nil)
//	{
//		MobileInput *input = [mobileInputList objectForKey:[NSNumber numberWithInt:inputId]];
//		if(input) 
//		{
//			UIView* editView = input.editView;
//			UITextView* textView = editView != nil && [editView isKindOfClass:[UITextView class]]? (UITextView*) editView : nil;
//			if(textView != nil)
//			{
//				UITextRange *selectedRange = [textView selectedTextRange];
//				return selectedRange == nil || [selectedRange empty];
//			}
//			
//			UITextField* textField = editView != nil && [editView isKindOfClass:[UITextField class]]? (UITextField*) editView : nil;
//			if(textField != nil)
//			{
//				UITextRange *selectedRange = [textField selectedTextRange];
//				return selectedRange == nil || [selectedRange empty];
//			}
//		}
//	}
	return false;
//}

+ (void)destroy {
    NSArray *list = [mobileInputList allValues];
    for (MobileInput *input in list) {
        [input remove];
    }
    mobileInputList = nil;
}

+ (void)setPlugin:(NSString *)name {
    plugin = name;
}

- (id)initWith:(UIViewController *)controller andTag:(int)idInput {
    if (self = [super init]) {
        viewController = controller;
        inputId = idInput;
        [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(keyboardWillShow:) name:UIKeyboardWillShowNotification object:nil];
        [[NSNotificationCenter defaultCenter] addObserver:self selector:@selector(keyboardWillHide:) name:UIKeyboardWillHideNotification object:nil];
    }
    return self;
}

- (BOOL)isFocused {
    return editView.isFirstResponder;
}

- (void)sendData:(NSMutableDictionary *)data {
    [data setValue:[NSNumber numberWithInt:inputId] forKey:@"id"];
    NSString *result = [MobileInputCommon dictToJson:data];
    [MobileInputCommon sendData:plugin data:result];
}

- (void)processData:(NSDictionary *)data {
    NSString *msg = [data valueForKey:@"msg"];
    if ([msg isEqualToString:REMOVE]) {
        [self remove];
    } else if ([msg isEqualToString:SET_TEXT]) {
        NSString *text = [data valueForKey:@"text"];
        [self setText:text];
    } else if ([msg isEqualToString:SET_RECT]) {
        [self setRect:data];
    } else if ([msg isEqualToString:SET_FOCUS]) {
        BOOL isFocus = [[data valueForKey:@"is_focus"] boolValue];
        [self setFocus:isFocus];
    } else if ([msg isEqualToString:SET_VISIBLE]) {
        BOOL isVisible = [[data valueForKey:@"is_visible"] boolValue];
        [self setVisible:isVisible];
    }
}

- (void)setRect:(NSDictionary *)data {
    float x = [[data valueForKey:@"x"] floatValue] * viewController.view.bounds.size.width;
    float y = [[data valueForKey:@"y"] floatValue] * viewController.view.bounds.size.height;
    float width = [[data valueForKey:@"width"] floatValue] * viewController.view.bounds.size.width;
    float height = [[data valueForKey:@"height"] floatValue] * viewController.view.bounds.size.height;
    //x -= editView.superview.frame.origin.x;
    //y -= editView.superview.frame.origin.y;
    editView.frame = CGRectMake(x, y, width, height);
}

BOOL multiline;

- (void)create:(NSDictionary *)data {
    NSString *placeholder = [data valueForKey:@"placeholder"];
	NSString *font = [data valueForKey:@"font"];
	NSString *fontTrueName = [font stringByDeletingPathExtension];
	
    float fontSize = [[data valueForKey:@"font_size"] floatValue];
    float x = [[data valueForKey:@"x"] floatValue] * viewController.view.bounds.size.width;
    float y = [[data valueForKey:@"y"] floatValue] * viewController.view.bounds.size.height;
    float width = [[data valueForKey:@"width"] floatValue] * viewController.view.bounds.size.width;
    float height = [[data valueForKey:@"height"] floatValue] * viewController.view.bounds.size.height;
	
	//Pan Content Offset
	//float pan_x = [[data valueForKey:@"pan_content_x"] floatValue] * viewController.view.bounds.size.width;
    float pan_y = [[data valueForKey:@"pan_content_y"] floatValue] * viewController.view.bounds.size.height;
    //float pan_width = [[data valueForKey:@"pan_content_width"] floatValue] * viewController.view.bounds.size.width;
    //float pan_height = [[data valueForKey:@"pan_content_height"] floatValue] * viewController.view.bounds.size.height;
	
	//Setup AutoToolbar in IQKeyboardManager
	float pan_offset =  y - pan_y; //Zero is on top of the screen, so we must subtract y - pan_y to pickup the offset
	pan_offset = pan_offset > 0? pan_offset : 0;
	[[IQKeyboardManager sharedManager] setEnableAutoToolbar:NO];
	[[IQKeyboardManager sharedManager] setKeyboardDistanceFromTextField:pan_offset];
	//[[IQKeyboardManager sharedManager] setEnable:NO];
	
    characterLimit = [[data valueForKey:@"character_limit"] intValue];
    float textColor_r = [[data valueForKey:@"text_color_r"] floatValue];
    float textColor_g = [[data valueForKey:@"text_color_g"] floatValue];
    float textColor_b = [[data valueForKey:@"text_color_b"] floatValue];
    float textColor_a = [[data valueForKey:@"text_color_a"] floatValue];
    UIColor *textColor = [UIColor colorWithRed:textColor_r green:textColor_g blue:textColor_b alpha:textColor_a];
    float backColor_r = [[data valueForKey:@"back_color_r"] floatValue];
    float backColor_g = [[data valueForKey:@"back_color_g"] floatValue];
    float backColor_b = [[data valueForKey:@"back_color_b"] floatValue];
    float backColor_a = [[data valueForKey:@"back_color_a"] floatValue];
    UIColor *backgroundColor = [UIColor colorWithRed:backColor_r green:backColor_g blue:backColor_b alpha:backColor_a];
    float placeHolderColor_r = [[data valueForKey:@"placeholder_color_r"] floatValue];
    float placeHolderColor_g = [[data valueForKey:@"placeholder_color_g"] floatValue];
    float placeHolderColor_b = [[data valueForKey:@"placeholder_color_b"] floatValue];
    float placeHolderColor_a = [[data valueForKey:@"placeholder_color_a"] floatValue];
    UIColor *placeHolderColor = [UIColor colorWithRed:placeHolderColor_r green:placeHolderColor_g blue:placeHolderColor_b alpha:placeHolderColor_a];
    NSString *contentType = [data valueForKey:@"content_type"];
    NSString *alignment = [data valueForKey:@"align"];
    BOOL withDoneButton = [[data valueForKey:@"with_done_button"] boolValue];
    BOOL withClearButton = [[data valueForKey:@"with_clear_button"] boolValue];
    multiline = [[data valueForKey:@"multiline"] boolValue];
    BOOL autoCorrection = NO;
    BOOL password = NO;
    NSString *inputType = [data valueForKey:@"input_type"];
    NSString *keyboardType = [data valueForKey:@"keyboard_type"];
    UIKeyboardType keyType = UIKeyboardTypeDefault;
    if ([contentType isEqualToString:@"Autocorrected"]) {
        autoCorrection = YES;
    } else if ([contentType isEqualToString:@"IntegerNumber"]) {
        keyType = UIKeyboardTypeNumberPad;
    } else if ([contentType isEqualToString:@"DecimalNumber"]) {
        keyType = UIKeyboardTypeDecimalPad;
    } else if ([contentType isEqualToString:@"Alphanumeric"]) {
        keyType = UIKeyboardTypeAlphabet;
    } else if ([contentType isEqualToString:@"Name"]) {
        keyType = UIKeyboardTypeNamePhonePad;
    } else if ([contentType isEqualToString:@"EmailAddress"]) {
        keyType = UIKeyboardTypeEmailAddress;
    } else if ([contentType isEqualToString:@"Password"]) {
        password = YES;
    } else if ([contentType isEqualToString:@"Pin"]) {
        keyType = UIKeyboardTypePhonePad;
    } else if ([contentType isEqualToString:@"Custom"]) {
        if ([keyboardType isEqualToString:@"ASCIICapable"]) {
            keyType = UIKeyboardTypeASCIICapable;
        } else if ([keyboardType isEqualToString:@"NumbersAndPunctuation"]) {
            keyType = UIKeyboardTypeNumbersAndPunctuation;
        } else if ([keyboardType isEqualToString:@"URL"]) {
            keyType = UIKeyboardTypeURL;
        } else if ([keyboardType isEqualToString:@"NumberPad"]) {
            keyType = UIKeyboardTypeNumberPad;
        } else if ([keyboardType isEqualToString:@"PhonePad"]) {
            keyType = UIKeyboardTypePhonePad;
        } else if ([keyboardType isEqualToString:@"NamePhonePad"]) {
            keyType = UIKeyboardTypeNamePhonePad;
        } else if ([keyboardType isEqualToString:@"EmailAddress"]) {
            keyType = UIKeyboardTypeEmailAddress;
        } else if ([keyboardType isEqualToString:@"Social"]) {
            keyType = UIKeyboardTypeTwitter;
        } else if ([keyboardType isEqualToString:@"Search"]) {
            keyType = UIKeyboardTypeWebSearch;
        } else {
            keyType = UIKeyboardTypeDefault;
        }
        if ([inputType isEqualToString:@"Standard"]) {
        } else if ([inputType isEqualToString:@"AutoCorrect"]) {
            autoCorrection = YES;
        } else if ([inputType isEqualToString:@"Password"]) {
            password = YES;
        }
    }
    UIControlContentHorizontalAlignment halign = UIControlContentHorizontalAlignmentLeft;
    UIControlContentVerticalAlignment valign = UIControlContentVerticalAlignmentCenter;
    NSTextAlignment textAlign = NSTextAlignmentCenter;
    if ([alignment isEqualToString:@"UpperLeft"]) {
        valign = UIControlContentVerticalAlignmentTop;
        halign = UIControlContentHorizontalAlignmentLeft;
        textAlign = NSTextAlignmentLeft;
    } else if ([alignment isEqualToString:@"UpperCenter"]) {
        valign = UIControlContentVerticalAlignmentTop;
        halign = UIControlContentHorizontalAlignmentCenter;
        textAlign = NSTextAlignmentCenter;
    } else if ([alignment isEqualToString:@"UpperRight"]) {
        valign = UIControlContentVerticalAlignmentTop;
        halign = UIControlContentHorizontalAlignmentRight;
        textAlign = NSTextAlignmentRight;
    } else if ([alignment isEqualToString:@"MiddleLeft"]) {
        valign = UIControlContentVerticalAlignmentCenter;
        halign = UIControlContentHorizontalAlignmentLeft;
        textAlign = NSTextAlignmentLeft;
    } else if ([alignment isEqualToString:@"MiddleCenter"]) {
        valign = UIControlContentVerticalAlignmentCenter;
        halign = UIControlContentHorizontalAlignmentCenter;
        textAlign = NSTextAlignmentCenter;
    } else if ([alignment isEqualToString:@"MiddleRight"]) {
        valign = UIControlContentVerticalAlignmentCenter;
        halign = UIControlContentHorizontalAlignmentRight;
        textAlign = NSTextAlignmentRight;
    } else if ([alignment isEqualToString:@"LowerLeft"]) {
        valign = UIControlContentVerticalAlignmentBottom;
        halign = UIControlContentHorizontalAlignmentLeft;
        textAlign = NSTextAlignmentLeft;
    } else if ([alignment isEqualToString:@"LowerCenter"]) {
        valign = UIControlContentVerticalAlignmentBottom;
        halign = UIControlContentHorizontalAlignmentCenter;
        textAlign = NSTextAlignmentCenter;
    } else if ([alignment isEqualToString:@"LowerRight"]) {
        valign = UIControlContentVerticalAlignmentBottom;
        halign = UIControlContentHorizontalAlignmentRight;
        textAlign = NSTextAlignmentRight;
    }
    if (withDoneButton) {
        keyboardDoneButtonView = [[UIToolbar alloc] init];
        [keyboardDoneButtonView sizeToFit];
        doneButton = [[UIBarButtonItem alloc] initWithTitle:@"Done" style:UIBarButtonItemStyleDone target:self action:@selector(doneClicked:)];
        UIBarButtonItem *flexibleSpace = [[UIBarButtonItem alloc] initWithBarButtonSystemItem:UIBarButtonSystemItemFlexibleSpace target:nil action:nil];
        [keyboardDoneButtonView setItems:[NSArray arrayWithObjects:flexibleSpace, flexibleSpace, doneButton, nil]];
    } else {
        keyboardDoneButtonView = nil;
    }
	
    UIReturnKeyType returnKeyType = UIReturnKeyDefault;
    NSString *returnKeyTypeString = [data valueForKey:@"return_key_type"];
    if ([returnKeyTypeString isEqualToString:@"Next"]) {
        returnKeyType = UIReturnKeyNext;
    } else if ([returnKeyTypeString isEqualToString:@"Done"]) {
        returnKeyType = UIReturnKeyDone;
    } else if ([returnKeyTypeString isEqualToString:@"Search"]) {
        returnKeyType = UIReturnKeySearch;
    }
    fontSize = fontSize / [UIScreen mainScreen].scale;
	
	//Create Font Asset
	UIFont *uiFont = nil;
    if (!password && [fontTrueName length] > 0)
    {
        uiFont = [UIFont fontWithName:fontTrueName size:fontSize];
    }
    if(uiFont == nil || [fontTrueName length] == 0)
    {
        uiFont = [UIFont systemFontOfSize:fontSize]; //[UIFont fontWithName:@"Helvetica" size:fontSize];
    } 
	
    if (multiline) {
        UITextView *textView = [[UITextView alloc] initWithFrame:CGRectMake(x, y, width, height)];
        textView.keyboardType = keyType;
        [textView setFont:uiFont];
        textView.scrollEnabled = TRUE;
        textView.delegate = self;
        textView.tag = inputId;
        textView.text = @"";
        textView.textColor = textColor;
		//[textView setTextColor:textColor]
        textView.backgroundColor = backgroundColor;
        textView.returnKeyType = returnKeyType;
        textView.textAlignment = textAlign;
        textView.autocorrectionType = autoCorrection ? UITextAutocorrectionTypeYes : UITextAutocorrectionTypeNo;
        textView.contentInset = UIEdgeInsetsMake(0.0f, 0.0f, 0.0f, 0.0f);
        //textView.placeholder = placeholder;
        //textView.placeholderColor = placeHolderColor;
        textView.delegate = self;
        if (keyType == UIKeyboardTypeEmailAddress) {
            textView.autocapitalizationType = UITextAutocapitalizationTypeNone;
        }
        [textView setSecureTextEntry:password];
        if (keyboardDoneButtonView != nil) {
            textView.inputAccessoryView = keyboardDoneButtonView;
        }
        editView = textView;
    } else {
        UITextField *textField = [[UITextField alloc] initWithFrame:CGRectMake(x, y, width, height)];
        textField.keyboardType = keyType;
        [textField setFont:uiFont];
        textField.delegate = self;
        textField.tag = inputId;
        textField.text = @"";
        textField.textColor = textColor;
        textField.backgroundColor = backgroundColor;
        textField.returnKeyType = returnKeyType;
        textField.autocorrectionType = autoCorrection ? UITextAutocorrectionTypeYes : UITextAutocorrectionTypeNo;
        textField.contentVerticalAlignment = valign;
        textField.contentHorizontalAlignment = halign;
        textField.textAlignment = textAlign;
		
        if (withClearButton) {
            textField.clearButtonMode = UITextFieldViewModeWhileEditing;
        }
        textField.attributedPlaceholder = [[NSAttributedString alloc] initWithString:placeholder attributes:@{NSForegroundColorAttributeName: placeHolderColor}];
        textField.delegate = self;
        if (keyType == UIKeyboardTypeEmailAddress) {
            textField.autocapitalizationType = UITextAutocapitalizationTypeNone;
        }
        [textField addTarget:self action:@selector(textFieldDidChange:) forControlEvents:UIControlEventEditingChanged];
        [textField addTarget:self action:@selector(textFieldActive:) forControlEvents:UIControlEventEditingDidBegin];
        [textField addTarget:self action:@selector(textFieldInActive:) forControlEvents:UIControlEventEditingDidEnd];
        [textField setSecureTextEntry:password];
        if (keyboardDoneButtonView != nil) {
            textField.inputAccessoryView = keyboardDoneButtonView;
        }
        editView = textField;
    }
    [mainViewController.view addSubview:editView];
    NSMutableDictionary *msg = [[NSMutableDictionary alloc] init];
    [msg setValue:READY forKey:@"msg"];
    [self sendData:msg];
}

- (void)setText:(NSString *)text {
    if ([editView isKindOfClass:[UITextField class]]) {
        [((UITextField *) editView) setText:text];
    } else if ([editView isKindOfClass:[UITextView class]]) {
        [((UITextView *) editView) setText:text];
    }
}

- (IBAction) doneClicked:(id)sender {
    //[self setVisible:false];
	//[self setFocus:false];
	NSMutableDictionary *msg = [[NSMutableDictionary alloc] init];
    [msg setValue:DONE_KEY_DOWN forKey:@"msg"];
    [self sendData:msg];
}

- (void)remove {
    [[NSNotificationCenter defaultCenter] removeObserver:self];
    [editView resignFirstResponder];
    [editView removeFromSuperview];
    if (keyboardDoneButtonView != nil) {
        doneButton = nil;
        keyboardDoneButtonView = nil;
    }
}

- (void)setFocus:(BOOL)isFocus {
    if (isFocus) {
        [editView becomeFirstResponder];
    } else {
        [editView resignFirstResponder];
    }
}

- (void)showKeyboard:(BOOL)isShow 
{
	//Enable IQKeyboardManager
	//[[IQKeyboardManager sharedManager] setEnable:(isShow ? YES : NO)];
    [editView endEditing:(isShow ? YES : NO)];
}

- (void)setVisible:(BOOL)isVisible {
    editView.hidden = (isVisible ? NO : YES);
	if(!isVisible) {
		[self showKeyboard:false];
	}
}

- (void)onTextChange:(NSString *)text {
    NSMutableDictionary *msg = [[NSMutableDictionary alloc] init];
    [msg setValue:TEXT_CHANGE forKey:@"msg"];
    [msg setValue:text forKey:@"text"];
    [self sendData:msg];
}

- (void)onTextEditEnd:(NSString *)text {
    NSMutableDictionary *msg = [[NSMutableDictionary alloc] init];
    [msg setValue:TEXT_END_EDIT forKey:@"msg"];
    [msg setValue:text forKey:@"text"];
    [self sendData:msg];
}

- (void)textViewDidChange:(UITextView *)textView {
    [self onTextChange:textView.text];
}

- (void)textViewDidBeginEditing:(UITextView *)textView {
    if (multiline) {
        NSMutableDictionary *msg = [[NSMutableDictionary alloc] init];
        [msg setValue:ON_FOCUS forKey:@"msg"];
        [self sendData:msg];
    }
}

- (void)textViewDidEndEditing:(UITextView *)textView {
    if (multiline) {
        NSMutableDictionary *msg = [[NSMutableDictionary alloc] init];
        [msg setValue:ON_UNFOCUS forKey:@"msg"];
        [self sendData:msg];
    }
    [self onTextEditEnd:textView.text];
}

- (BOOL)textFieldShouldReturn:(UITextField *)textField {
	if(editView != textField && ![editView isFirstResponder]) {
    //if (![editView isFirstResponder]) {
        return YES;
    }
    NSMutableDictionary *msg = [[NSMutableDictionary alloc] init];
    [msg setValue:RETURN_PRESSED forKey:@"msg"];
    [self sendData:msg];
    return YES;
}

- (BOOL)textField:(UITextField *)textField shouldChangeCharactersInRange:(NSRange)range replacementString:(NSString *)string {
    if (range.length + range.location > textField.text.length) {
        return NO;
    }
    NSUInteger newLength = [textField.text length] + [string length] - range.length;
    if (characterLimit > 0) {
        return newLength <= characterLimit;
    } else {
        return YES;
    }
}

- (void)textFieldActive:(UITextField *)theTextField {
    NSMutableDictionary *msg = [[NSMutableDictionary alloc] init];
    [msg setValue:ON_FOCUS forKey:@"msg"];
    [self sendData:msg];
}

- (void)textFieldInActive:(UITextField *)theTextField {
    NSMutableDictionary *msg = [[NSMutableDictionary alloc] init];
    [msg setValue:ON_UNFOCUS forKey:@"msg"];
    [self sendData:msg];
}

- (void)textFieldDidChange:(UITextField *)theTextField {
    [self onTextChange:theTextField.text];
}

- (void)keyboardWillShow:(NSNotification *)notification {
    if (![editView isFirstResponder]) {
        return;
    }
    NSMutableDictionary *tmp = [[NSMutableDictionary alloc] init];
    [tmp setValue:KEYBOARD_PREPARE forKey:@"msg"];
    [self sendData:tmp];
    NSDictionary *keyboardInfo = [notification userInfo];
    NSValue *keyboardFrameBegin = [keyboardInfo valueForKey:UIKeyboardFrameEndUserInfoKey];
    rectKeyboardFrame = [keyboardFrameBegin CGRectValue];
    CGFloat height = rectKeyboardFrame.size.height;
    NSMutableDictionary *msg = [[NSMutableDictionary alloc] init];
    [msg setValue:KEYBOARD_ACTION forKey:@"msg"];
    [msg setValue:[NSNumber numberWithBool:YES] forKey:@"show"];
    [msg setValue:[NSNumber numberWithFloat:height] forKey:@"height"];
    [self sendData:msg];
}

- (void)keyboardWillHide:(NSNotification *)notification {
	
    NSMutableDictionary *msg = [[NSMutableDictionary alloc] init];
    [msg setValue:KEYBOARD_ACTION forKey:@"msg"];
    [msg setValue:[NSNumber numberWithBool:NO] forKey:@"show"];
    [msg setValue:[NSNumber numberWithFloat:0] forKey:@"height"];
    [self sendData:msg];
}

@end


extern "C" {

void inputExecute(int inputId, const char *data) {
    [MobileInput processMessage:inputId data:[NSString stringWithUTF8String:data]];
}

//BOOL inputHasActiveSelection(int inputId) {
//    return [MobileInput hasActiveSelection:inputId]];
//}

void inputDestroy() {
    [MobileInput destroy];
}

void inputInit() {
    [MobileInput setPlugin:@"mobileinput"];
    [MobileInput init:UnityGetGLViewController()];
}

}
