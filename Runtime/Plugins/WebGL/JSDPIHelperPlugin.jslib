var JSDPIHelperPlugin = {

	_JSGetScreenDPI: function () {
		return window.devicePixelRatio * 96;
	}
};

mergeInto(LibraryManager.library, JSDPIHelperPlugin);