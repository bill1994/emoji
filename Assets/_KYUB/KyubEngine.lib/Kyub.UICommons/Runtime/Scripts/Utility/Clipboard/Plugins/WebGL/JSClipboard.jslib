var JSClipper = {

    _JSSetToClipboard: function(textPtr) {
        var text = Pointer_stringify(textPtr);

		if (navigator.clipboard)
		{
			navigator.clipboard.writeText(text)
			  .then(function ()
				{ 
					//Sucess!
				})
			  .catch(function (err)
			  {
				console.log('Clipboard error', err);
			  });
		}
    },
	
	_JSGetFromClipboard: function() {
	
		var returnStr = '';
		if (navigator.clipboard)
		{
			navigator.clipboard.readText()
				.then(function (text)
				{ 
					returnStr = text; 
				})
				.catch(function (err) 
				{
				  console.log('Clipboard error', err);
				});
		}
		var bufferSize = lengthBytesUTF8(returnStr) + 1;
		var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
		return buffer;
    }
};

mergeInto(LibraryManager.library, JSClipper);