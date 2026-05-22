import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../api/api.dart';
import '../api/api_client.dart';
import '../api/models/auth_models.dart';

class AuthState {
  final String? token;
  final String? username;
  final bool isLoading;
  final String? error;

  const AuthState({
    this.token,
    this.username,
    this.isLoading = false,
    this.error,
  });

  AuthState copyWith({
    String? token,
    String? username,
    bool? isLoading,
    String? error,
    bool clearToken = false,
  }) =>
      AuthState(
        token: clearToken ? null : (token ?? this.token),
        username: clearToken ? null : (username ?? this.username),
        isLoading: isLoading ?? this.isLoading,
        error: error,
      );
}

class AuthStore extends Notifier<AuthState> {
  @override
  AuthState build() {
    _init();
    return const AuthState();
  }

  Future<void> _init() async {
    final token = await readToken();
    if (token != null) {
      state = state.copyWith(token: token);
    }
  }

  Future<bool> login(String username, String password) async {
    state = state.copyWith(isLoading: true, error: null);
    try {
      final LoginResponse resp = await authApi.login(username, password);
      await saveCredentials(token: resp.token, tenantId: resp.tenantId);
      state = state.copyWith(
        token: resp.token,
        username: resp.username,
        isLoading: false,
      );
      return true;
    } catch (e) {
      state = state.copyWith(
        isLoading: false,
        error: _parseError(e),
      );
      return false;
    }
  }

  Future<void> logout() async {
    await clearCredentials();
    state = const AuthState();
  }

  String _parseError(Object e) {
    if (e is Exception) return e.toString().replaceAll('Exception: ', '');
    return '登录失败，请重试';
  }
}

final authStoreProvider =
    NotifierProvider<AuthStore, AuthState>(AuthStore.new);
