import 'dart:io';
import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:pretty_dio_logger/pretty_dio_logger.dart';

const _kTokenKey = 'auth_token';
const _kTenantKey = 'tenant_id';

/// 全局安全存储实例（在 interceptor 内同步读取）
const _secureStorage = FlutterSecureStorage(
  aOptions: AndroidOptions(encryptedSharedPreferences: true),
  iOptions: IOSOptions(accessibility: KeychainAccessibility.first_unlock),
);

Dio createDio({String? baseUrl}) {
  final dio = Dio(
    BaseOptions(
      baseUrl: baseUrl ?? const String.fromEnvironment(
        'API_BASE_URL',
        defaultValue: 'http://10.0.2.2:5146/api', // Android 模拟器访问本机
      ),
      connectTimeout: const Duration(seconds: 10),
      receiveTimeout: const Duration(seconds: 30),
      contentType: Headers.jsonContentType,
    ),
  );

  // ── 请求拦截：注入 JWT + X-Tenant-ID ──────────────────────────────────
  dio.interceptors.add(
    InterceptorsWrapper(
      onRequest: (options, handler) async {
        final token = await _secureStorage.read(key: _kTokenKey);
        final tenantId = await _secureStorage.read(key: _kTenantKey);
        if (token != null) {
          options.headers['Authorization'] = 'Bearer $token';
        }
        if (tenantId != null) {
          options.headers['X-Tenant-ID'] = tenantId;
        }
        handler.next(options);
      },
      onError: (error, handler) async {
        if (error.response?.statusCode == 401) {
          await _secureStorage.delete(key: _kTokenKey);
          // TODO: 触发全局登出事件（可通过 Riverpod ref.invalidate 实现）
        }
        handler.next(error);
      },
    ),
  );

  // 开发模式日志（release 时自动关闭）
  assert(() {
    dio.interceptors.add(PrettyDioLogger(
      requestHeader: true,
      requestBody: true,
    ));
    return true;
  }());

  return dio;
}

/// 读取存储的 Token
Future<String?> readToken() => _secureStorage.read(key: _kTokenKey);

/// 持久化 Token + TenantId
Future<void> saveCredentials({
  required String token,
  String? tenantId,
}) async {
  await _secureStorage.write(key: _kTokenKey, value: token);
  if (tenantId != null) {
    await _secureStorage.write(key: _kTenantKey, value: tenantId);
  }
}

/// 清除所有凭据
Future<void> clearCredentials() async {
  await _secureStorage.deleteAll();
}
