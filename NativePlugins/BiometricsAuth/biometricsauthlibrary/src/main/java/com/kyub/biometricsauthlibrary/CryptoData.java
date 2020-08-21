 package com.kyub.biometricsauthlibrary;
 
 import android.annotation.TargetApi;
 import android.security.keystore.KeyGenParameterSpec;
 import android.security.keystore.KeyProperties;
 import android.security.keystore.KeyPermanentlyInvalidatedException;
 import java.io.IOException;
 import java.security.InvalidAlgorithmParameterException;
 import java.security.InvalidKeyException;
 import java.security.KeyStore;
 import java.security.KeyStoreException;
 import java.security.NoSuchAlgorithmException;
 import java.security.NoSuchProviderException;
 import java.security.UnrecoverableKeyException;
 import java.security.cert.CertificateException;
 import javax.crypto.Cipher;
 import javax.crypto.KeyGenerator;
 import javax.crypto.NoSuchPaddingException;
 import javax.crypto.SecretKey;

 public class CryptoData
 {
     private KeyStore keyStore;
     private static final String KEY_NAME = "biometrics";
     private Cipher cipher;

     @TargetApi(23)
     protected void generateKey() {
         try {
             this.keyStore = KeyStore.getInstance("AndroidKeyStore");
         } catch (Exception e) {
             e.printStackTrace();
         }

         KeyGenerator keyGenerator;
         try {
             keyGenerator = KeyGenerator.getInstance("AES", "AndroidKeyStore");
         } catch (NoSuchAlgorithmException | NoSuchProviderException e) {
             throw new RuntimeException("Failed to get KeyGenerator instance", e);
         }
         try {
             this.keyStore.load(null);
             keyGenerator.init(new KeyGenParameterSpec.Builder(KEY_NAME, KeyProperties.PURPOSE_ENCRYPT | KeyProperties.PURPOSE_DECRYPT)
                     .setBlockModes(new String[]{"CBC"})
                     .setUserAuthenticationRequired(true)
                     .setEncryptionPaddings(new String[]{"PKCS7Padding"})

                     .build());
             keyGenerator.generateKey();
         } catch (NoSuchAlgorithmException | InvalidAlgorithmParameterException | CertificateException | IOException e) {
             throw new RuntimeException(e);
         }
     }

     @TargetApi(23)
     public boolean initCipher() {
         try {
             this.cipher = Cipher.getInstance("AES/CBC/PKCS7Padding");
         } catch (NoSuchAlgorithmException | NoSuchPaddingException e) {
             throw new RuntimeException("Failed to get Cipher", e);
         }

         try {
             this.keyStore.load(null);
             SecretKey key = (SecretKey)this.keyStore.getKey(KEY_NAME, null);

             this.cipher.init(Cipher.ENCRYPT_MODE, key);
             return true;
         } catch (KeyPermanentlyInvalidatedException e) {
             return false;
         } catch (CertificateException | IOException | NoSuchAlgorithmException | InvalidKeyException | UnrecoverableKeyException | KeyStoreException e) {
             throw new RuntimeException("Failed to init Cipher", e);
         }
     }

     @TargetApi(23)
     public Cipher getCipher() {
         return this.cipher;
     }
 }