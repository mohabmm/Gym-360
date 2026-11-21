/// Numbers typed in the package form, as an Egyptian keyboard types them: Arabic-Indic digits
/// (١٢ ، ١٨٠٠) or Western ones, with thousands separators ("1,800", "١٬٨٠٠").
class PackageNumbers {
  const PackageNumbers._();

  static const _arabicDigits = '٠١٢٣٤٥٦٧٨٩';

  /// "١٢" / " 12 " → 12; anything else → null.
  static int? sessions(String text) => int.tryParse(_western(text));

  /// "1,800" / "١٬٨٠٠" / "1800.50" / "١٨٠٠٫٥" → the amount; anything else → null.
  /// A comma is a thousands separator here (prices are whole pounds), never a decimal point.
  static double? price(String text) => double.tryParse(_western(text).replaceAll(',', ''));

  static String _western(String text) {
    final buffer = StringBuffer();
    for (final ch in text.trim().split('')) {
      final arabic = _arabicDigits.indexOf(ch);
      if (arabic >= 0) {
        buffer.write(arabic);
      } else if (ch == '٫') {
        buffer.write('.');
      } else if (ch != '٬' && ch != ' ') {
        buffer.write(ch);
      }
    }
    return buffer.toString();
  }
}
