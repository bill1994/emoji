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

    static boolean s_isPipMode = false;
    static BackgroundModeUtils s_instance = null;
    static boolean s_autoPipModeOnPause = false;
    static OnPipModeChangedListener s_pipModeChangedUnityCallback = null;

    public interface OnPipModeChangedListener {
        void Execute(boolean value);
    }

    public static void SetPipModeChangedUnityCallback(OnPipModeChangedListener pipModeChangedUnityCallback) {
        s_pipModeChangedUnityCallback = pipModeChangedUnityCallback;
    }

    public static void SetAutoPipModeOnPause(boolean supportPipModeOnPause) {
        s_autoPipModeOnPause = supportPipModeOnPause;

        TryCreateAndRegisterInstance();

        //Close Activity when state changed to false and is in pipmode
        if(!s_autoPipModeOnPause && s_isPipMode)
            UnityPlayer.currentActivity.moveTaskToBack(true);
    }

    public static boolean GetAutoPipModeOnPause() {

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

    protected static void SetPipModeStateCallingEvents_Internal(boolean isPipMode) {
        if(s_isPipMode != isPipMode)
        {
            s_isPipMode = isPipMode;
            if(s_pipModeChangedUnityCallback != null)
                s_pipModeChangedUnityCallback.Execute(s_isPipMode);
        }
    }

    @TargetApi(26)
    protected static boolean EnterInPipMode_API26() {
        UnityPlayer unityPlayerInstance = GetUnityPlayerInstance();
        if (unityPlayerInstance != null) {
            Rational rational = new Rational(1,1); //new Rational(unityPlayerInstance.getWidth(), unityPlayerInstance.getHeight());
            PictureInPictureParams params = new PictureInPictureParams.Builder()
                    .setAspectRatio(rational)
                    .build();

            if(!s_isPipMode) {
                boolean isPipMode = UnityPlayer.currentActivity.enterPictureInPictureMode(params);
                SetPipModeStateCallingEvents_Internal(isPipMode);
            }
            return s_isPipMode;
        }
        return false;
    }

    @TargetApi(26)
    protected static boolean IsInPipMode_API26() {
        return UnityPlayer.currentActivity.isInPictureInPictureMode();
    }

    protected static void TryCreateAndRegisterInstance() {
        try {
            if (s_instance == null) {
                s_instance = new BackgroundModeUtils();
            }

            if(UnityPlayer.currentActivity != null) {
                Application app = UnityPlayer.currentActivity.getApplication();
                app.unregisterActivityLifecycleCallbacks(s_instance);
                app.registerActivityLifecycleCallbacks(s_instance);
            }
        } catch (Exception e) {
            e.printStackTrace();
        }
    }

    protected static void UnregisterInstance() {
        try {
            s_delayedHandler = null;
            if (s_instance != null && UnityPlayer.currentActivity != null) {
                Application app = UnityPlayer.currentActivity.getApplication();

                app.unregisterActivityLifecycleCallbacks(s_instance);
            }
        } catch (Exception e) {
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
            unityPlayerInstance.pause();
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
        SetPipModeStateCallingEvents_Internal(false);
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
                        ResumeUnityPlayer();
                        EnterInPipMode();
                    }
                }
            }, 20);
        }
    }

    @Override
    public void onActivityStopped(Activity activity)
    {
        if(s_isPipMode || s_delayedHandler != null) {
            s_delayedHandler = null;

            SetPipModeStateCallingEvents_Internal(false);
            PauseUnityPlayer();
        }
    }

    @Override
    public void onActivitySaveInstanceState(Activity activity, Bundle bundle) {

    }

    @Override
    public void onActivityDestroyed(Activity activity) {
        s_delayedHandler = null;
    }
}
