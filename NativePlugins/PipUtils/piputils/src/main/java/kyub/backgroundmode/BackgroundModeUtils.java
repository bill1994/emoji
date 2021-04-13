package kyub.backgroundmode;

import android.annotation.TargetApi;
import android.app.Activity;
import android.app.Application;
import android.app.PictureInPictureParams;
import android.os.Build;
import android.os.Bundle;
import android.os.Handler;
import android.os.Looper;
import android.util.Log;
import android.util.Rational;

import com.unity3d.player.UnityPlayer;

import java.lang.reflect.Field;


public class BackgroundModeUtils implements Application.ActivityLifecycleCallbacks {

    static BackgroundModeUtils s_instance = null;
    static boolean s_autoPipModeOnPause = false;

    public static void SetAutoPipModeOnPause(boolean supportPipModeOnPause)
    {
        TryCreateAndRegisterInstance();
        s_autoPipModeOnPause = supportPipModeOnPause;
    }

    public static boolean GetAutoPipModeOnPause()
    {
        TryCreateAndRegisterInstance();
        return s_autoPipModeOnPause;
    }

    public static boolean IsPipModeSupported() {
        if (Build.VERSION.SDK_INT < 26) {
            return false;
        }

        return true;
    }

    public static boolean EnterInPipMode() {
        if (!IsPipModeSupported()) {
            return false;
        }

        return EnterInPipMode_API26();
    }

    public static boolean IsInPipMode() {
        if (!IsPipModeSupported()) {
            return false;
        }

        return IsInPipMode_API26();
    }

    //INTERNAL ONLY

    @TargetApi(26)
    protected static boolean EnterInPipMode_API26() {
        UnityPlayer unityPlayerInstance = GetUnityPlayerInstance();
        if(unityPlayerInstance != null) {
            Rational rational = new Rational(unityPlayerInstance.getWidth(), unityPlayerInstance.getHeight());
            PictureInPictureParams params = new PictureInPictureParams.Builder()
                    .setAspectRatio(rational)
                    .build();
            UnityPlayer.currentActivity.enterPictureInPictureMode(params);
            return true;
        }
        return false;
    }

    @TargetApi(26)
    protected static boolean IsInPipMode_API26() {
        return UnityPlayer.currentActivity.isInPictureInPictureMode();
    }

    protected static void TryCreateAndRegisterInstance()
    {
        try {
            if (s_instance == null && UnityPlayer.currentActivity != null) {
                s_instance = new BackgroundModeUtils();
                Application app = UnityPlayer.currentActivity.getApplication();

                app.unregisterActivityLifecycleCallbacks(s_instance);
                app.registerActivityLifecycleCallbacks(s_instance);
            }
        }
        catch (Exception e) {
            e.printStackTrace();
        }
    }

    protected static void ResumeUnityPlayer() {

        UnityPlayer unityPlayerInstance = GetUnityPlayerInstance();

        if (unityPlayerInstance != null)
            unityPlayerInstance.resume();
    }

    protected static void PauseUnityPlayer() {

        UnityPlayer unityPlayerInstance = GetUnityPlayerInstance();

        if (unityPlayerInstance != null)
            unityPlayerInstance.resume();
    }

    protected static UnityPlayer GetUnityPlayerInstance() {
        try {
            Field field = UnityPlayer.currentActivity.getClass().getDeclaredField("mUnityPlayer");
            field.setAccessible(true);

            UnityPlayer unityPlayerInstance = (UnityPlayer)field.get(UnityPlayer.currentActivity);

            return unityPlayerInstance;
        } catch (IllegalAccessException e) {
            e.printStackTrace();
        } catch (NoSuchFieldException e) {
            e.printStackTrace();
        }

        return null;
    }

    //ACTIVITY LIFE CYCLE

    @Override
    public void onActivityCreated(Activity activity, Bundle savedInstanceState) {

    }

    @Override
    public void onActivityStarted(Activity activity) {

    }

    @Override
    public void onActivityResumed(Activity activity) {
        s_delayedHandler = null;
    }

    //Used to control if activity is axecuting delayed
    static volatile Handler s_delayedHandler = null;
    @Override
    public void onActivityPaused(Activity activity) {

        if (activity == UnityPlayer.currentActivity &&
                s_autoPipModeOnPause &&
                IsPipModeSupported())
        {

            final Handler handler = new Handler(Looper.getMainLooper());
            s_delayedHandler = handler;

            s_delayedHandler.postDelayed(new Runnable() {
                @Override
                public void run() {
                    if(s_delayedHandler == handler && s_autoPipModeOnPause) {
                        EnterInPipMode();
                        ResumeUnityPlayer();
                        s_delayedHandler = null;
                    }
                }
            }, 20);
        }
    }

    @Override
    public void onActivityStopped(Activity activity) {
        s_delayedHandler = null;
    }

    @Override
    public void onActivitySaveInstanceState(Activity activity, Bundle bundle) {

    }

    @Override
    public void onActivityDestroyed(Activity activity) {
        s_delayedHandler = null;
    }
}
