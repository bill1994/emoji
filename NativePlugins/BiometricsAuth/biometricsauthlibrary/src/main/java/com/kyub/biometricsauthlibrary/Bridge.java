 package com.kyub.biometricsauthlibrary;

 import android.Manifest;
 import android.app.KeyguardManager;
 import android.content.Context;
 import android.content.pm.PackageManager;
 import android.os.Build;

 import com.kyub.biometricprompt.*;
 import com.unity3d.player.UnityPlayer;
 //import androidx.biometric.*;
 import androidx.core.app.ActivityCompat;
 import androidx.core.hardware.fingerprint.FingerprintManagerCompat;

 public class Bridge {
     private Context context;
     private static boolean useLegacyMode = false;
     private static Bridge instance;

     public Bridge() {
         instance = this;
     }

     public static Bridge instance() {
         if (instance == null) {
             instance = new Bridge();
         }
         return instance;
     }

     public void setLegacyMode(boolean useLegacyMode) {
         this.useLegacyMode = useLegacyMode;
     }

     public void setContext(Context context) {
         this.context = context;
     }

     public void startBiometricsAuth(String objectName, String title, String subtitle, String description, String cancelName) {
         authenticateUser(objectName, title, subtitle, description, cancelName);
     }

     private void cancelBiometricsAuth() {
         try {
             if (!isBiometricPromptEnabled()) {
                 FingerprintHandler instance = FingerprintHandler.instance();
                 instance.cancelAuth();
             } else {
                 BiometricHandler instance = BiometricHandler.instance();
                 instance.cancelAuth();
             }
         }
         catch (Exception e) {}
     }

     public boolean isBiometricPromptEnabled() {
         if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.P) {
             return true;
         } else {
             return !useLegacyMode;
         }
     }

     public boolean isBiometricHardwareAvailable() {
         try {
             return BiometricUtils.isSdkVersionSupported() && BiometricUtils.isFingerprintAvailable(context);
         }
         catch (Exception e) {}

         return false;
     }

     public boolean isPermissionGranted() {
         try {
            return BiometricUtils.isPermissionGranted(context);
         }
         catch (Exception e) {}

         return false;
     }

     private void authenticateUser(final String objectName, String title, String subtitle, String description, String cancelName) {
         try {
             if (!isBiometricPromptEnabled()) {
                 legacyUserAuthentication(objectName);
             } else {
                 if (!isBiometricHardwareAvailable()) {
                     String error = "Your Device does not have a Biometric Sensor";

                     UnityPlayer.UnitySendMessage(objectName, BiometricHandler.UNITY_CALLBACK_FINALIZE_NAME, error);

                 } else if (!isPermissionGranted()) {
                     String error = "Biometric authentication permission not granted";

                     UnityPlayer.UnitySendMessage(objectName, BiometricHandler.UNITY_CALLBACK_FINALIZE_NAME, error);
                 } else {

                     BiometricManager biometricManager = new BiometricManager.BiometricBuilder(this.context)
                             .setTitle(title != null && !title.trim().isEmpty() ? title : "Touch ID")
                             .setSubtitle(subtitle != null && !subtitle.trim().isEmpty() ? subtitle : "")
                             .setDescription(description != null && !description.trim().isEmpty() ? description : "")
                             .setNegativeButtonText(cancelName != null && !cancelName.trim().isEmpty() ? cancelName : "Cancel")
                             .build();

                     BiometricHandler instance = BiometricHandler.instance();
                     instance.Init(this.context);
                     instance.startAuth(biometricManager, new BiometricHandler.CallBackHandler() {
                         public void AuthDidFinish(String e, boolean success, boolean terminateSignal) {
                             String error = success ? "" : (e != null && !e.isEmpty() ? e : "Biometric Authentication failed.");
                             UnityPlayer.UnitySendMessage(objectName,
                                     terminateSignal ? BiometricHandler.UNITY_CALLBACK_FINALIZE_NAME : BiometricHandler.UNITY_CALLBACK_RETURN_NAME,
                                     error);
                         }
                     });
                 }
             }
         }
         catch (Exception e)
         {
             String error = "Biometric Exception";
             UnityPlayer.UnitySendMessage(objectName, BiometricHandler.UNITY_CALLBACK_FINALIZE_NAME, error);
         }
     }

     private void legacyUserAuthentication(final String objectName) {

         KeyguardManager keyguardManager = (KeyguardManager) this.context.getSystemService(Context.KEYGUARD_SERVICE);
         FingerprintManagerCompat fingerprintManager = FingerprintManagerCompat.from(this.context);//(FingerprintManager) this.context.getSystemService(Context.FINGERPRINT_SERVICE);
         if (!isBiometricHardwareAvailable()) {
             String error = "Your Device does not have a Biometric Sensor";

             UnityPlayer.UnitySendMessage(objectName, BiometricHandler.UNITY_CALLBACK_FINALIZE_NAME, error);

         } else if (!isPermissionGranted()) {
             String error = "Biometric authentication permission not granted";

             UnityPlayer.UnitySendMessage(objectName, BiometricHandler.UNITY_CALLBACK_FINALIZE_NAME, error);
         } else if (keyguardManager == null || !keyguardManager.isKeyguardSecure()) {
             String error = "Lock screen security not enabled in your device settings";

             UnityPlayer.UnitySendMessage(objectName, FingerprintHandler.UNITY_CALLBACK_FINALIZE_NAME, error);
         } else {

             CryptoData crypto = new CryptoData();
             crypto.generateKey();

             if (crypto.initCipher()) {
                 FingerprintManagerCompat.CryptoObject cryptoObject = new FingerprintManagerCompat.CryptoObject(crypto.getCipher());
                 FingerprintHandler instance = FingerprintHandler.instance();
                 instance.Init(this.context);
                 instance.startAuth(fingerprintManager, cryptoObject, new FingerprintHandler.CallBackHandler() {
                     public void AuthDidFinish(String e, boolean success, boolean terminateSignal) {
                         String error = success ? "" : (e != null && !e.isEmpty() ? e : "Fingerprint Authentication failed.");
                         UnityPlayer.UnitySendMessage(objectName,
                                 terminateSignal ? FingerprintHandler.UNITY_CALLBACK_FINALIZE_NAME : FingerprintHandler.UNITY_CALLBACK_RETURN_NAME,
                                 error);
                     }
                 });
             } else {
                 String error = "initCipher unknown error";
                 UnityPlayer.UnitySendMessage(objectName, FingerprintHandler.UNITY_CALLBACK_FINALIZE_NAME, error);
             }
         }
     }
 }