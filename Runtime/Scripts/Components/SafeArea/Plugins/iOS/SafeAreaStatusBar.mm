#include "UnityViewControllerBase.h"


extern "C"
{
	bool _IsIOSStatusBarActive()
	{
		return ![UIApplication sharedApplication].isStatusBarHidden;
	}
	
	float _GetStatusBarHeight()
	{
		if(@available(iOS 13.0, *))
		{
			UIStatusBarManager *manager = [UIApplication sharedApplication].keyWindow.windowScene.statusBarManager;
			CGFloat height = manager.statusBarFrame.size.height;
			return height;
		}
		else
		{
			CGSize statusBarSize = [[UIApplication sharedApplication] statusBarFrame].size;
			return MIN(statusBarSize.width, statusBarSize.height);
		}
	}
}
