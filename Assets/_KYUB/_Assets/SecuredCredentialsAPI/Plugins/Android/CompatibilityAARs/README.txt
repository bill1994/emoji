This folder contains the legacy versions of the API.

* The version 1.0 will always use in-unity popup
* This version will use FingerPrintManager so it cannot display Iris Biometrics from Android 9.0
* Inside this folder will have a version compatible with appcompat-v7 and a version compatible with androidX (SDK 28)
* After Activate One version you must deactivate other aars of this plugin
* Dont forget to change BiometricDependences.xml

PS: com.google.materials dependences os only used in plugin 2.0+ so if you want to activate this you can deativate this dependence