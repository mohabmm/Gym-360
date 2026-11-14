import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_mlkit_text_recognition/google_mlkit_text_recognition.dart';

import 'inbody_sheet_parser.dart';

/// Reads an InBody sheet photo into values. On-device only: the photo never leaves the phone.
abstract interface class InBodyTextReader {
  Future<InBodyReading> read(String imagePath);
}

/// Google ML Kit (Latin script — InBody sheets print labels and numbers in Latin characters).
class MlKitInBodyTextReader implements InBodyTextReader {
  final InBodySheetParser _parser;

  const MlKitInBodyTextReader([this._parser = const InBodySheetParser()]);

  @override
  Future<InBodyReading> read(String imagePath) async {
    final recognizer = TextRecognizer(script: TextRecognitionScript.latin);
    try {
      final text = await recognizer.processImage(InputImage.fromFilePath(imagePath));
      final lines = [
        for (final block in text.blocks)
          for (final line in block.lines)
            OcrLine(
              line.text,
              top: line.boundingBox.top,
              bottom: line.boundingBox.bottom,
              left: line.boundingBox.left,
            ),
      ];
      return _parser.parse(lines);
    } finally {
      await recognizer.close();
    }
  }
}

final inBodyTextReaderProvider = Provider<InBodyTextReader>((_) => const MlKitInBodyTextReader());
