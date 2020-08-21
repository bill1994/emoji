 package com.kyub.biometricsauthlibrary;

 import android.annotation.TargetApi;
 import android.content.Context;

 //import android.hardware.fingerprint.FingerprintManager;
 //import android.support.v4.app.ActivityCompat;
 import androidx.core.app.ActivityCompat;
 import androidx.core.hardware.fingerprint.FingerprintManagerCompat;
 import androidx.core.os.CancellationSignal;
 import android.os.Handler;

 import com.unity3d.player.UnityPlayer;

 @TargetApi(23)
 public class FingerprintHandler extends FingerprintManagerCompat.AuthenticationCallback {
   public static final String UNITY_CALLBACK_RETURN_NAME = "OnAuthenticationReturn";
   public static final String UNITY_CALLBACK_FINALIZE_NAME = "OnAuthenticationBridgeDidFinish";

   private Context context;
   private CallBackHandler callBackHandler;
   private CancellationSignal cancellationSignal;
   private String objectName;
   private int waitTime;

   static FingerprintHandler instance = null;
   public static FingerprintHandler instance()
   {
     if(instance == null)
       instance = new FingerprintHandler();
     return instance;
   }

   private FingerprintHandler()
   {
     instance = this;
   }

   public void Init(Context mContext) {
     this.context = mContext;
   }

   public void startAuth(FingerprintManagerCompat manager, FingerprintManagerCompat.CryptoObject cryptoObject, String objectName, int waitSeconds) {
     this.objectName = objectName;
     this.waitTime = waitSeconds;
     CancellationSignal cancellationSignal = new CancellationSignal();
     if (ActivityCompat.checkSelfPermission(this.context, "android.permission.USE_FINGERPRINT") != 0) {
       return;
     }
     manager.authenticate(cryptoObject, 0, cancellationSignal, this, null);
   }

   public void startAuth(FingerprintManagerCompat manager, FingerprintManagerCompat.CryptoObject cryptoObject, CallBackHandler callBackHandler) {
     this.callBackHandler = callBackHandler;
     this.cancellationSignal = new CancellationSignal();
     if (ActivityCompat.checkSelfPermission(this.context, "android.permission.USE_FINGERPRINT") != 0) {
       return;
     }
     manager.authenticate(cryptoObject, 0, cancellationSignal, this, null);
     //manager.authenticate(cryptoObject, cancellationSignal, 0, this, null);
   }

   public void cancelAuth() {
     if(this.cancellationSignal != null) {
       this.cancellationSignal.cancel();
       this.cancellationSignal = null;

       update("Cancelled", Boolean.valueOf(false), Boolean.valueOf(true));
     }
   }

   public void onAuthenticationError(int errMsgId, CharSequence errString)
   {
     update("Fingerprint Authentication error\n" + errString, Boolean.valueOf(false), Boolean.valueOf(true));
   }

   public void onAuthenticationHelp(int helpMsgId, CharSequence helpString)
   {
     update("Fingerprint Authentication help\n" + helpString, Boolean.valueOf(false), Boolean.valueOf(false));
   }

   public void onAuthenticationFailed()
   {
     update("Fingerprint Authentication failed.", Boolean.valueOf(false), Boolean.valueOf(false));
   }

   public void onAuthenticationSucceeded(FingerprintManagerCompat.AuthenticationResult result)
   {
     update("Fingerprint Authentication succeeded.", Boolean.valueOf(true), Boolean.valueOf(true));
   }

   public void update(final String e, final Boolean success, final Boolean terminateSignal)
   {
     if(terminateSignal.booleanValue())
       this.cancellationSignal = null;

     if (this.callBackHandler != null) {
       this.callBackHandler.AuthDidFinish(e, success.booleanValue(), terminateSignal);
     } else {
       if (success.booleanValue()) {

         Handler handler = new Handler();
         handler.postDelayed(new Runnable()
         {
           public void run() {
             String error = success.booleanValue()? "" : (e != null && !e.isEmpty()? e : "Fingerprint Authentication failed." );

             UnityPlayer.UnitySendMessage(FingerprintHandler.this.objectName,
                     terminateSignal.booleanValue()? FingerprintHandler.UNITY_CALLBACK_FINALIZE_NAME : FingerprintHandler.UNITY_CALLBACK_RETURN_NAME,
                     error);
           } }
           , this.waitTime * 1000);
       }
     }
   }
   
   public static abstract interface CallBackHandler
   {
     public abstract void AuthDidFinish(String paramString, boolean paramBoolean, boolean terminate);
   }
 }