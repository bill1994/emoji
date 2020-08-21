 package com.kyub.biometricsauthlibrary;

 import android.content.Context;
 import android.content.Intent;
 import android.os.Bundle;
 import android.os.Handler;

 import com.kyub.biometricprompt.BiometricCallback;
 import com.kyub.biometricprompt.BiometricManager;
 import com.unity3d.player.UnityPlayer;

 import androidx.annotation.NonNull;
 //import androidx.biometric.BiometricPrompt;
 import androidx.fragment.app.FragmentActivity;

 import java.util.concurrent.*;

 public class BiometricHandler implements BiometricCallback {
   public static final String UNITY_CALLBACK_RETURN_NAME = "OnAuthenticationReturn";
   public static final String UNITY_CALLBACK_FINALIZE_NAME = "OnAuthenticationBridgeDidFinish";

   private Context context;
   private CallBackHandler callBackHandler;
   private BiometricManager biometricManager;

   private String objectName;
   private int waitTime;

   static BiometricHandler instance = null;
   public static BiometricHandler instance()
   {
     if(instance == null)
       instance = new BiometricHandler();
     return instance;
   }

   private BiometricHandler()
   {
     instance = this;
   }

   public void Init(Context mContext) {
     this.context = mContext;
   }

   public void startAuth(BiometricManager biometricManagerParam, CallBackHandler callBackHandler) {
     this.callBackHandler = callBackHandler;
     this.biometricManager = biometricManagerParam;

     String err = "";

     try {
       this.biometricManager.authenticate(this);
     } catch (Exception e) {
       err = e.getMessage();
     }

     if (err != null && !err.trim().isEmpty()) {
       update("Biometric Authentication error.\n" + err, Boolean.valueOf(false), Boolean.valueOf(true));
     }
   }


   public void cancelAuth() {
     if(this.biometricManager != null)
     {
       this.biometricManager.cancelAuthentication();

       update("Cancelled", Boolean.valueOf(false), Boolean.valueOf(true));
     }
   }

   @Override
   public void onAuthenticationError(int errorCode, CharSequence errString) {
     update("Biometric Authentication error\n" + errString, Boolean.valueOf(false), Boolean.valueOf(true));
   }

   @Override
   public void onSdkVersionNotSupported() {
     update("Sdk Version not supported\n", Boolean.valueOf(false), Boolean.valueOf(true));
   }

   @Override
   public void onBiometricAuthenticationNotAvailable() {
     update("Biometric Authentication not available", Boolean.valueOf(false), Boolean.valueOf(true));
   }

   @Override
   public void onBiometricAuthenticationPermissionNotGranted() {
     update("Biometric Authentication permission not granted", Boolean.valueOf(false), Boolean.valueOf(true));
   }

   @Override
   public void onBiometricAuthenticationInternalError(String error) {
     update("Biometric Authentication internal error\n" + error, Boolean.valueOf(false), Boolean.valueOf(true));
   }

   @Override
   public void onAuthenticationFailed()
   {
     update("Biometric Authentication failed.", Boolean.valueOf(false), Boolean.valueOf(false));
   }

   @Override
   public void onAuthenticationCancelled() {
     update("Biometric Authentication cancelled\n", Boolean.valueOf(false), Boolean.valueOf(true));
   }

   @Override
   public void onAuthenticationHelp(int helpCode, CharSequence helpString) {
     update("Fingerprint Authentication help\n" + helpString, Boolean.valueOf(false), Boolean.valueOf(false));
   }

   @Override
   public void onAuthenticationSuccessful() {
     update("Biometric Authentication succeeded.", Boolean.valueOf(true), Boolean.valueOf(true));
   }

   /*@Override
   public void onAuthenticationSucceeded(@NonNull BiometricPrompt.AuthenticationResult result)
   {
     update("Biometric Authentication succeeded.", Boolean.valueOf(true), Boolean.valueOf(true));
   }*/

   public void update(final String e, final Boolean success, final Boolean terminateSignal)
   {
     if(terminateSignal.booleanValue()) {
       this.biometricManager = null;
     }

     if (this.callBackHandler != null) {
       this.callBackHandler.AuthDidFinish(e, success.booleanValue(), terminateSignal);
     } else {
       if (success.booleanValue()) {

         Handler handler = new Handler();
         handler.postDelayed(new Runnable()
         {
           public void run() {
             String error = success.booleanValue()? "" : (e != null && !e.isEmpty()? e : "Biometric Authentication failed." );

             UnityPlayer.UnitySendMessage(BiometricHandler.this.objectName,
                     terminateSignal.booleanValue()? BiometricHandler.UNITY_CALLBACK_FINALIZE_NAME : BiometricHandler.UNITY_CALLBACK_RETURN_NAME,
                     error);
           } }
           , this.waitTime * 1000);
       }
     }
   }

   public abstract interface CallBackHandler
   {
     public abstract void AuthDidFinish(String paramString, boolean paramBoolean, boolean terminate);
   }
 }