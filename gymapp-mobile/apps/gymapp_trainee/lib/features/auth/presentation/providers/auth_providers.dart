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

/// Registration flow state
class RegistrationState {
  final String email;
  final String password;
  final String firstName;
  final String lastName;
  final String? phone;
  final bool dataProcessingConsent;
  final bool marketingConsent;
  final bool isLoading;
  final String? error;

  const RegistrationState({
    this.email = '',
    this.password = '',
    this.firstName = '',
    this.lastName = '',
    this.phone,
    this.dataProcessingConsent = false,
    this.marketingConsent = false,
    this.isLoading = false,
    this.error,
  });

  RegistrationState copyWith({
    String? email,
    String? password,
    String? firstName,
    String? lastName,
    String? phone,
    bool? dataProcessingConsent,
    bool? marketingConsent,
    bool? isLoading,
    String? error,
  }) {
    return RegistrationState(
      email: email ?? this.email,
      password: password ?? this.password,
      firstName: firstName ?? this.firstName,
      lastName: lastName ?? this.lastName,
      phone: phone ?? this.phone,
      dataProcessingConsent:
          dataProcessingConsent ?? this.dataProcessingConsent,
      marketingConsent: marketingConsent ?? this.marketingConsent,
      isLoading: isLoading ?? this.isLoading,
      error: error,
    );
  }
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

  void updateEmail(String email) => state = state.copyWith(email: email);
  void updatePassword(String pw) => state = state.copyWith(password: pw);
  void updateFirstName(String name) =>
      state = state.copyWith(firstName: name);
  void updateLastName(String name) => state = state.copyWith(lastName: name);
  void updatePhone(String? phone) => state = state.copyWith(phone: phone);
  void setDataConsent(bool v) =>
      state = state.copyWith(dataProcessingConsent: v);
  void setMarketingConsent(bool v) =>
      state = state.copyWith(marketingConsent: v);

  Future<bool> register() async {
    state = state.copyWith(isLoading: true, error: null);
    try {
      final request = RegisterRequest(
        email: state.email,
        password: state.password,
        firstName: state.firstName,
        lastName: state.lastName,
        phone: state.phone ?? '',
        role: 'Trainee',
        dataProcessingConsent: state.dataProcessingConsent,
        marketingConsent: state.marketingConsent,
      );
      final response = await _repo.register(request);
      await _authNotifier.login(response);
      state = state.copyWith(isLoading: false);
      return true;
    } on ApiException catch (e) {
      state = state.copyWith(isLoading: false, error: e.message);
      return false;
    } catch (e) {
      state = state.copyWith(isLoading: false, error: e.toString());
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
