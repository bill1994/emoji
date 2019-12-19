#include "UnityViewControllerBase.h"


extern "C"
{
	bool _IsIOSStatusBarActive()
	{
		return ![UIApplication sharedApplication].isStatusBarHidden;
	}
	
	float _GetStatusBarHeight()
	{
		CGSize statusBarSize = [[UIApplication sharedApplication] statusBarFrame].size;
		return MIN(statusBarSize.width, statusBarSize.height);
	}
}
