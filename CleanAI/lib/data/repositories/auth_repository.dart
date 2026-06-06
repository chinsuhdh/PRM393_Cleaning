import 'package:flutter_riverpod/flutter_riverpod.dart';

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

  Future<bool> login(String email, String password, UserRole role) async {
    await Future.delayed(const Duration(seconds: 1));
    state = AuthState(
      isAuthenticated: true,
      userId: 'u1',
      userName: 'Bui Ngoc Tam',
      role: role,
    );
    return true;
  }

  Future<bool> register({
    required String name,
    required String email,
    required String phone,
    required String password,
    UserRole role = UserRole.client,
  }) async {
    await Future.delayed(const Duration(seconds: 1));
    state = AuthState(
      isAuthenticated: true,
      userId: 'u_new',
      userName: name,
      role: role,
    );
    return true;
  }

  void logout() {
    state = const AuthState();
  }
}

final authProvider = StateNotifierProvider<AuthNotifier, AuthState>((ref) {
  return AuthNotifier();
});
