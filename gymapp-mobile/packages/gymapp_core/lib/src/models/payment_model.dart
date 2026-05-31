// ignore_for_file: invalid_annotation_target

import 'package:freezed_annotation/freezed_annotation.dart';
import 'session_package_model.dart';

part 'payment_model.freezed.dart';
part 'payment_model.g.dart';

@freezed
class PaymentModel with _$PaymentModel {
  const factory PaymentModel({
    required String id,
    @Default('') String gymId,
    @Default('') String memberId,
    String? memberName,
    @Default(0) double amount,
    @Default('EGP') String currency,
    @Default('') String method,
    @Default('') String purpose,
    @Default('Completed') String status,
    String? receiptNumber,
    String? notes,
    String? recordedBy,
    String? recordedByName,
    DateTime? paidAt,
    DateTime? voidedAt,
    String? voidReason,
    DateTime? createdAt,
    // PRD /me/payments fields.
    // Nested money envelope `{amount, currency}`. When present it takes
    // precedence over the legacy top-level [amount]/[currency] fields.
    // Populated by [_PaymentModelJsonParser] from the server's `amount`
    // object; ignored during (de)serialisation of its own key so the legacy
    // top-level `amount: <double>` shape keeps working.
    @JsonKey(includeFromJson: false, includeToJson: false)
    MoneyModel? amountMoney,
    // Server-localised alias for the payment-method enum.
    String? paymentMethod,
    // FK to the plan/package/product the payment was for (nullable).
    String? referenceId,
    // Human-readable label for the reference (e.g. "10 Sessions — PT").
    String? referenceLabel,
    // Historical display name of the staff member who recorded the payment.
    String? recordedByStaffName,
  }) = _PaymentModel;

  factory PaymentModel.fromJson(Map<String, dynamic> json) =>
      _PaymentModelJsonParser.parse(json);
}

/// Reconciles the two historical shapes for [PaymentModel.amount]:
/// - Legacy: `{ "amount": 500.0, "currency": "EGP", ... }`
/// - PRD /me: `{ "amount": { "amount": 500.0, "currency": "EGP" }, ... }`
///
/// The freezed-generated `fromJson` expects a fixed shape for each field, so
/// we normalise here before handing off to the generator, then attach the
/// nested money envelope via copyWith.
class _PaymentModelJsonParser {
  static PaymentModel parse(Map<String, dynamic> json) {
    final rawAmount = json['amount'];
    if (rawAmount is Map<String, dynamic>) {
      // PRD shape: lift `amount.amount` and `amount.currency` to the top-level
      // fields expected by the generated parser.
      final normalised = Map<String, dynamic>.from(json);
      normalised['amount'] = (rawAmount['amount'] as num?)?.toDouble() ?? 0.0;
      if (rawAmount['currency'] is String) {
        normalised['currency'] = rawAmount['currency'];
      }
      final base = _$PaymentModelFromJson(normalised);
      return base.copyWith(amountMoney: MoneyModel.fromJson(rawAmount));
    }
    return _$PaymentModelFromJson(json);
  }
}

@freezed
class RecordPaymentRequest with _$RecordPaymentRequest {
  const factory RecordPaymentRequest({
    required String memberId,
    required double amount,
    required String method,
    required String purpose,
    String? notes,
  }) = _RecordPaymentRequest;

  factory RecordPaymentRequest.fromJson(Map<String, dynamic> json) =>
      _$RecordPaymentRequestFromJson(json);
}

@freezed
class PaymentSummaryModel with _$PaymentSummaryModel {
  const factory PaymentSummaryModel({
    required double totalRevenue,
    required double monthlyRevenue,
    required int totalPayments,
    required int monthlyPayments,
    required Map<String, double> revenueByMethod,
  }) = _PaymentSummaryModel;

  factory PaymentSummaryModel.fromJson(Map<String, dynamic> json) =>
      _$PaymentSummaryModelFromJson(json);
}
