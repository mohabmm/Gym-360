# google_mlkit_text_recognition (InBody scan OCR) references the optional
# Chinese / Devanagari / Japanese / Korean script models. We only bundle the
# Latin model, so tell R8 those classes are intentionally absent.
-dontwarn com.google.mlkit.vision.text.chinese.**
-dontwarn com.google.mlkit.vision.text.devanagari.**
-dontwarn com.google.mlkit.vision.text.japanese.**
-dontwarn com.google.mlkit.vision.text.korean.**
