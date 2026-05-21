class LoginResponse {
  final String token;
  final String username;
  final String? tenantId;

  const LoginResponse({
    required this.token,
    required this.username,
    this.tenantId,
  });

  factory LoginResponse.fromJson(Map<String, dynamic> json) => LoginResponse(
        token: json['token'] as String,
        username: json['username'] as String,
        tenantId: json['tenantId'] as String?,
      );
}
