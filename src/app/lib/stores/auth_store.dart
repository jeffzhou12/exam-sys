import 'dart:convert';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../api/api.dart';
import '../api/api_client.dart';
import '../api/models/auth_models.dart';

class AuthState {
  final String? token;
  final String? userId;
  final String? username;
  final String? role;
  final String? tenantId;
  final bool isLoading;
  final String? error;

  const AuthState({
    this.token,
    this.userId,
    this.username,
    this.role,
    this.tenantId,
    this.isLoading = false,
    this.error,
  });

  bool get isLoggedIn => token != null;

  AuthState copyWith({
    String? token,
    String? userId,
    String? username,
    String? role,
    String? tenantId,
    bool? isLoading,
    String? error,
    bool clearToken = false,
  }) =>
      AuthState(
        token: clearToken ? null : (token ?? this.token),
        userId: clearToken ? null : (userId ?? this.userId),
        username: clearToken ? null : (username ?? this.username),
        role: clearToken ? null : (role ?? this.role),
        tenantId: clearToken ? null : (tenantId ?? this.tenantId),
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
      state = state.copyWith(
        token: token,
        userId: _extractUserIdFromJwt(token),
      );
    }
  }

  String? _extractUserIdFromJwt(String token) {
    try {
      final parts = token.split('.');
      if (parts.length != 3) return null;
      final normalized = base64Url.normalize(parts[1]);
      final payload = jsonDecode(utf8.decode(base64Url.decode(normalized))) as Map<String, dynamic>;
      return payload['sub'] as String? ?? payload['nameid'] as String?;
    } catch (_) {
      return null;
    }
  }

  Future<bool> login(String identifier, String password) async {
    state = state.copyWith(isLoading: true, error: null);
    try {
      final LoginResponse resp = await authApi.login(identifier, password);
      await saveCredentials(token: resp.token, tenantId: resp.tenantId);
      state = state.copyWith(
        token: resp.token,
        userId: _extractUserIdFromJwt(resp.token),
        username: resp.username,
        role: resp.role,
        tenantId: resp.tenantId,
        isLoading: false,
      );
      return true;
    } catch (e) {
      state = state.copyWith(isLoading: false, error: _parseError(e));
      return false;
    }
  }

  Future<bool> loginWithCode(String target, String code, {String? tenantId, String? role}) async {
    state = state.copyWith(isLoading: true, error: null);
    try {
      final LoginResponse resp = await authApi.loginWithCode(
        target: target,
        code: code,
        tenantId: tenantId,
        role: role,
      );
      await saveCredentials(token: resp.token, tenantId: resp.tenantId);
      state = state.copyWith(
        token: resp.token,
        userId: _extractUserIdFromJwt(resp.token),
        username: resp.username,
        role: resp.role,
        tenantId: resp.tenantId,
        isLoading: false,
      );
      return true;
    } catch (e) {
      state = state.copyWith(isLoading: false, error: _parseError(e));
      return false;
    }
  }

  Future<bool> register({
    required String username,
    required String password,
    required String role,
    required String tenantId,
    String? phoneNumber,
    String? email,
  }) async {
    state = state.copyWith(isLoading: true, error: null);
    try {
      await authApi.register(
        username: username,
        password: password,
        role: role,
        tenantId: tenantId,
        phoneNumber: phoneNumber,
        email: email,
      );
      // Auto-login after register
      return await login(username, password);
    } catch (e) {
      state = state.copyWith(isLoading: false, error: _parseError(e));
      return false;
    }
  }

  Future<void> logout() async {
    await clearCredentials();
    state = const AuthState();
  }

  String _parseError(Object e) {
    if (e is Exception) return e.toString().replaceAll('Exception: ', '');
    return '操作失败，请重试';
  }
}

final authStoreProvider =
    NotifierProvider<AuthStore, AuthState>(AuthStore.new);
