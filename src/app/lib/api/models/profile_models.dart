class UserProfile {
  final String id;
  final String username;
  final String? nickname;
  final String? avatarUrl;
  final String? email;
  final String? phoneNumber;
  final String? gender;
  final String? address;
  final String? educationLevel;
  final List<String> interestedSubjects;

  const UserProfile({
    required this.id,
    required this.username,
    this.nickname,
    this.avatarUrl,
    this.email,
    this.phoneNumber,
    this.gender,
    this.address,
    this.educationLevel,
    required this.interestedSubjects,
  });

  factory UserProfile.fromJson(Map<String, dynamic> json) => UserProfile(
        id: json['id'] as String,
        username: json['username'] as String,
        nickname: json['nickname'] as String?,
        avatarUrl: json['avatarUrl'] as String?,
        email: json['email'] as String?,
        phoneNumber: json['phoneNumber'] as String?,
        gender: json['gender'] as String?,
        address: json['address'] as String?,
        educationLevel: json['educationLevel'] as String?,
        interestedSubjects: (json['interestedSubjects'] as List<dynamic>?)
                ?.map((e) => e as String)
                .toList() ??
            [],
      );
}
