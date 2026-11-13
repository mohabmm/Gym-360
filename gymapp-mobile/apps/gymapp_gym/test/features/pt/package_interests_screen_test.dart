import 'package:flutter_test/flutter_test.dart';
import 'package:gymapp_gym/features/pt/presentation/screens/package_interests_screen.dart';

void main() {
  group('whatsAppUri', () {
    test('local Egyptian mobile gets the 20 country code', () {
      expect(whatsAppUri('010 1234 5678').toString(), 'https://wa.me/201012345678');
    });

    test('international format keeps its digits', () {
      expect(whatsAppUri('+20 101 234 5678').toString(), 'https://wa.me/201012345678');
    });
  });
}
