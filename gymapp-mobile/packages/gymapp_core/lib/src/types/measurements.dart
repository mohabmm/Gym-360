/// Small value types for quantities the UI formats, so a water amount cannot
/// be passed where calories are expected (both are plain `int`s on the wire).
///
/// The extension types are zero-cost and implement their representation type
/// (`int` / `double`), so arithmetic and comparisons keep working on them.
library;

/// A money amount in Egyptian pounds (the platform's only currency).
extension type const Egp(double amount) implements double {}

/// A volume of water in millilitres.
extension type const Milliliters(int value) implements int {}

/// An energy amount in kilocalories ("calories" in the UI).
extension type const Kilocalories(int value) implements int {}

/// A length in centimetres (body height).
extension type const Centimeters(double value) implements double {}

/// A percentage on the 0–100 scale (`42.5` means 42.5 %, not a 0–1 ratio).
extension type const Percent(double value) implements double {}

/// An average star rating on the 1–5 scale.
extension type const StarRating(double value) implements double {}

/// The unit a body/lift weight is expressed in.
enum WeightUnit {
  kg('kg'),
  lbs('lbs');

  const WeightUnit(this.symbol);

  /// Display suffix, e.g. `80.2 kg`.
  final String symbol;
}

/// Sessions consumed from a package: [used] out of [total].
class SessionUsage {
  const SessionUsage({required this.used, required this.total});

  final int used;
  final int total;
}
