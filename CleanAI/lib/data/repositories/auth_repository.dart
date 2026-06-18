import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/network/dio_client.dart';

enum UserRole { client, worker, admin }

class AuthState {
  final bool isAuthenticated;
  final String? userId;
  final String? userName;
  final UserRole role;

  const AuthState({
    this.isAuthenticated = false,
    this.userId,
    this.userName,
    this.role = UserRole.client,
  });

  // Adding copyWith to help with state updates if needed
  AuthState copyWith({
    bool? isAuthenticated,
    String? userId,
    String? userName,
    UserRole? role,
  }) {
    return AuthState(
      isAuthenticated: isAuthenticated ?? this.isAuthenticated,
      userId: userId ?? this.userId,
      userName: userName ?? this.userName,
      role: role ?? this.role,
    );
  }
}

class AuthNotifier extends StateNotifier<AuthState> {
  AuthNotifier() : super(const AuthState());

  // ==========================================
  // 1. LOGIN API
  // ==========================================
  Future<bool> login(String emailOrPhone, String password, UserRole role) async {
    try {
      final response = await DioClient.instance.post(
        '/Auth/login',
        data: {
          "emailOrPhone": emailOrPhone,
          "password": password
        },
      );

      final data = response.data;
      final token = data['accessToken'];
      final fullName = data['fullName'] ?? 'Người dùng';
      final profileId = data['profileId'];

      // Inject Token into Dio for subsequent requests
      DioClient.setAuthToken(token);

      // Update App State
      state = AuthState(
        isAuthenticated: true,
        userId: profileId,
        userName: fullName,
        role: role,
      );

      return true;
    } on DioException catch (e) {
      // [ĐÃ SỬA] Xử lý lỗi an toàn hơn tránh crash String/int
      final data = e.response?.data;
      String errorMsg = "Lỗi đăng nhập";

      if (data is Map<String, dynamic> && data['message'] != null) {
        // Nếu BE trả về JSON chuẩn
        errorMsg = data['message'];
      } else {
        // Nếu BE trả về cục lỗi 500 dạng text hoặc HTML
        errorMsg = "Tài khoản chưa xác thực hoặc sai thông tin.";
      }

      print("Login Error: $errorMsg");
      return false;
    }
  }

  // ==========================================
  // 2. REGISTER API
  // ==========================================
  Future<bool> register({
    required String name,
    required String email,
    required String phone,
    required String password,
    UserRole role = UserRole.client,
  }) async {
    try {
      // Map UserRole enum to string expected by .NET backend
      String roleString;
      switch (role) {
        case UserRole.admin:
          roleString = "Admin";
          break;
        case UserRole.worker:
          roleString = "Worker";
          break;
        case UserRole.client:
        default:
          roleString = "Client";
          break;
      }

      final response = await DioClient.instance.post(
        '/Auth/register',
        data: {
          "email": email,
          "phoneNumber": phone,
          "password": password,
          "fullName": name,
          "role": roleString,
        },
      );

      // Note: Your .NET API returns `{ message: "Đăng ký thành công!" }`
      // It does NOT return a token. The user must verify OTP and login afterward.
      // Therefore, we return true but DO NOT change the isAuthenticated state here.

      if (response.statusCode == 200) {
        print("Registration successful. Please verify OTP.");
        return true;
      }
      return false;

    } on DioException catch (e) {
      print("Registration Error: ${e.response?.data ?? e.message}");
      return false;
    }
  }

  // ==========================================
  // 3. LOGOUT
  // ==========================================
  void logout() {
    DioClient.clearAuthToken();
    state = const AuthState();
  }
}

final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  return AuthNotifier();
});

// ==========================================
// XÁC THỰC OTP (SAU KHI ĐĂNG KÝ)
// ==========================================
Future<bool> verifyAccount(String email, String otpCode) async {
  try {
    final response = await DioClient.instance.post(
      '/Auth/verify',
      data: {
        "email": email,
        "otpCode": otpCode,
      },
    );
    return response.statusCode == 200;
  } on DioException catch (e) {
    print("Lỗi xác thực: ${e.response?.data['message'] ?? e.message}");
    return false;
  }
}

// ==========================================
// QUÊN MẬT KHẨU (GỬI OTP)
// ==========================================
Future<bool> forgotPassword(String email) async {
  try {
    final response = await DioClient.instance.post(
      '/Auth/forgot-password',
      data: { "email": email },
    );
    return response.statusCode == 200;
  } on DioException catch (e) {
    print("Lỗi quên MK: ${e.response?.data['message'] ?? e.message}");
    return false;
  }
}

// ==========================================
// ĐẶT LẠI MẬT KHẨU
// ==========================================
Future<bool> resetPassword(String email, String otpCode, String newPassword) async {
  try {
    final response = await DioClient.instance.post(
      '/Auth/reset-password',
      data: {
        "email": email,
        "otpCode": otpCode,
        "newPassword": newPassword
      },
    );
    return response.statusCode == 200;
  } on DioException catch (e) {
    print("Lỗi đổi MK: ${e.response?.data['message'] ?? e.message}");
    return false;
  }
}