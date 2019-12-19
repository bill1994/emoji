Biometrics configurations:

-----------------------ANDROID------------------------------------
Add fingerprint permission in AndroidManifest.

Ex:
<manifest...>
  .
  .
  .
  <!-- FingerPrint -->
  <uses-feature android:name="android.hardware.fingerprint" android:required="false"/>
  <uses-permission android:name="android.permission.USE_FINGERPRINT" />
</manifest>

-----------------------IOS------------------------------------
To make the plugin work on iOS, you need to add the framework:
LocalAuthentication.framework to your project. You can add the framework to your
iOS project within Xcode. Open your generated Xcode project and click on your project
(the first item in the left panel), going over to the second-to-last tab: Build Phases and
under Link Binary with Libraries click on the 'plus' sign and add the
LocalAuthentication.framework. **