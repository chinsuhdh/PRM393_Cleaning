import 'package:dio/dio.dart';
import 'package:flutter/foundation.dart'; // Thêm import này cho debugPrint

class DioClient {
  DioClient._();

  static final Dio _dio = Dio(
    BaseOptions(
      baseUrl: const String.fromEnvironment(
        'API_BASE_URL',
        // Dùng 10.0.2.2 để Emulator gọi xuống localhost của máy tính (Port HTTP 5066)
        // Lưu ý: Nếu các controller trong .NET của bạn có route là "api/[controller]"
        // thì bạn cần thêm hậu tố /api vào đây, ví dụ: 'http://10.0.2.2:5066/api'
        defaultValue: 'http://10.0.2.2:5066',
      ),
      connectTimeout: const Duration(seconds: 30),
      receiveTimeout: const Duration(seconds: 30),
      headers: {
        'Content-Type': 'application/json',
        'Accept': 'application/json',
      },
    ),
  )..interceptors.addAll([
    LogInterceptor(
      requestBody: true,
      responseBody: true,
      error: true,
      logPrint: (obj) => debugPrint(obj.toString()),
    ),
  ]);

  static Dio get instance => _dio;

  static void setAuthToken(String token) {
    _dio.options.headers['Authorization'] = 'Bearer $token';
  }

  static void clearAuthToken() {
    _dio.options.headers.remove('Authorization');
  }
}