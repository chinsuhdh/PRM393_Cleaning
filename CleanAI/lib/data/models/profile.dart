class Profile {
  final String id;
  final String fullName;
  final String? avatarUrl;
  final String? email;
  final String? phoneNumber;

  Profile({
    required this.id,
    required this.fullName,
    this.avatarUrl,
    this.email,
    this.phoneNumber,
  });

  // Getter tự động lấy chữ cái đầu của tên để làm Avatar
  String get initials {
    if (fullName.trim().isEmpty) return '?';
    return fullName.trim()[0].toUpperCase();
  }

  factory Profile.fromJson(Map<String, dynamic> json) {
    return Profile(
      id: json['id']?.toString() ?? '',
      fullName: json['fullName']?.toString() ?? 'Người dùng',
      avatarUrl: json['avatarUrl']?.toString(),
      // Lấy thêm email và sđt (nếu backend có trả về)
      email: json['email']?.toString(),
      phoneNumber: json['phoneNumber']?.toString(),
    );
  }

  Map<String, dynamic> toJson() => {
    'id': id,
    'fullName': fullName,
    'avatarUrl': avatarUrl,
    'email': email,
    'phoneNumber': phoneNumber,
  };
}