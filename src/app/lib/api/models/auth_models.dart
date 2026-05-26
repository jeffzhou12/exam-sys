class LoginResponse {
  final String token;
  final String username;
  final String? tenantId;
  final String? role;

  const LoginResponse({
    required this.token,
    required this.username,
    this.tenantId,
    this.role,
  });

  factory LoginResponse.fromJson(Map<String, dynamic> json) => LoginResponse(
        token: (json['token'] ?? json['accessToken']) as String,
        username: json['username'] as String? ?? '',
        tenantId: json['tenantId'] as String?,
        role: json['role'] as String?,
      );
}

class TenantItem {
  final String id;
  final String name;

  const TenantItem({required this.id, required this.name});

  factory TenantItem.fromJson(Map<String, dynamic> json) => TenantItem(
        id: json['id'] as String,
        name: json['name'] as String,
      );
}
