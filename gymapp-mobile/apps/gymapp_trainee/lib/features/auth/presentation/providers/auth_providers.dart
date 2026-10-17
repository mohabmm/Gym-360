import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:gymapp_core/gymapp_core.dart';

import '../../../../core/providers.dart';
import '../../data/datasources/auth_remote_datasource.dart';
import '../../data/repositories/auth_repository.dart';

final authRemoteDataSourceProvider = Provider<AuthRemoteDataSource>((ref) {
  return AuthRemoteDataSource(apiClient: ref.watch(apiClientProvider));
});

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepository(
    remoteDataSource: ref.watch(authRemoteDataSourceProvider),
  );
});

/// Registration flow state: the sign-up form plus submission status.
class RegistrationState {
  /// The sign-up payload being collected; role is always `Trainee`.
  final RegisterRequest form;
  final bool isLoading;
  final String? error;

  static const RegisterRequest emptyForm = RegisterRequest(
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    phone: '',
    role: 'Trainee',
    dataProcessingConsent: false,
  );

  const RegistrationState({
    this.form = emptyForm,
    this.isLoading = false,
    this.error,
  });

  String get email => form.email;
  String get password => form.password;
  String get firstName => form.firstName;
  String get lastName => form.lastName;
  bool get dataProcessingConsent => form.dataProcessingConsent;
  bool get marketingConsent => form.marketingConsent;

  /// Replaces the form; any previous error is cleared.
  RegistrationState withForm(RegisterRequest form) =>
      RegistrationState(form: form, isLoading: isLoading);

  RegistrationState withStatus({required bool isLoading, String? error}) =>
      RegistrationState(form: form, isLoading: isLoading, error: error);
}

class RegistrationNotifier extends StateNotifier<RegistrationState> {
  final AuthRepository _repo;
  final AuthNotifier _authNotifier;

  RegistrationNotifier({
    required AuthRepository repo,
    required AuthNotifier authNotifier,
  })  : _repo = repo,
        _authNotifier = authNotifier,
        super(const RegistrationState());

  void _edit(RegisterRequest form) => state = state.withForm(form);

  void updateEmail(String email) => _edit(state.form.copyWith(email: email));
  void updatePassword(String pw) => _edit(state.form.copyWith(password: pw));
  void updateFirstName(String name) =>
      _edit(state.form.copyWith(firstName: name));
  void updateLastName(String name) =>
      _edit(state.form.copyWith(lastName: name));
  void updatePhone(String? phone) =>
      _edit(phone == null ? state.form : state.form.copyWith(phone: phone));
  void setDataConsent(bool v) =>
      _edit(state.form.copyWith(dataProcessingConsent: v));
  void setMarketingConsent(bool v) =>
      _edit(state.form.copyWith(marketingConsent: v));

  Future<bool> register() async {
    state = state.withStatus(isLoading: true);
    try {
      final response = await _repo.register(state.form);
      await _authNotifier.login(response);
      state = state.withStatus(isLoading: false);
      return true;
    } on ApiException catch (e) {
      state = state.withStatus(isLoading: false, error: e.message);
      return false;
    } catch (e) {
      state = state.withStatus(isLoading: false, error: e.toString());
      return false;
    }
  }
}

final registrationProvider =
    StateNotifierProvider<RegistrationNotifier, RegistrationState>((ref) {
  return RegistrationNotifier(
    repo: ref.watch(authRepositoryProvider),
    authNotifier: ref.watch(authProvider.notifier),
  );
});

/// Login form state
class LoginFormState {
  final bool isLoading;
  final String? error;

  const LoginFormState({this.isLoading = false, this.error});
}

final loginFormProvider =
    StateProvider.autoDispose<LoginFormState>((ref) => const LoginFormState());
