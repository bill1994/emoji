package kyub.uicommons.mobileinput;

import android.content.Context;
import android.content.res.AssetManager;
import android.graphics.Typeface;
import android.util.Log;

import java.util.Hashtable;

public class TypefaceCache {
    private static final String TAG = "TypefaceCache";

    private static boolean isInitialized = false;
    private static Hashtable<String, Typeface> cache = new Hashtable<String, Typeface>();

    static void Initialize() {
        isInitialized = true;
        cache.put("Roboto-Regular", Typeface.create("sans-serif", Typeface.NORMAL));
        cache.put("Roboto-Light", Typeface.create("sans-serif-light", Typeface.NORMAL));
        cache.put("Roboto-Thin", Typeface.create("sans-serif-thin", Typeface.NORMAL));
        cache.put("Roboto-Bold", Typeface.create("sans-serif", Typeface.BOLD));
        cache.put("Roboto-Medium", Typeface.create("sans-serif-medium", Typeface.NORMAL));
    }

    public static Typeface GetOrCreate(Context context, String assetPath) {
        synchronized (cache) {
            if(!isInitialized)
                Initialize();

            if(context == null  || assetPath == null || assetPath.trim().isEmpty())
                return null;

            //Remove Extension
            String cacheKey = assetPath;
            int pos = cacheKey.lastIndexOf(".");
            if (pos > 0 && pos < (cacheKey.length() - 1)) { // If '.' is not the first or last character.
                cacheKey = cacheKey.substring(0, pos);
            }
            if(cacheKey.trim().isEmpty())
                return null;

            Typeface font = cache.containsKey(cacheKey)? cache.get(cacheKey) : null;
            if (font == null) {
                try {
                    AssetManager assetManager = context.getAssets();
                    font = Typeface.createFromAsset(assetManager, assetPath);
                    if(font != null) {
                        cache.put(cacheKey, font);
                    }
                } catch (Exception e) {
                    font = null;
                    Log.e(TAG, "Could not get typeface '" + assetPath
                            + "' because " + e.getMessage());
                }
            }
            return font;
        }
    }
}
