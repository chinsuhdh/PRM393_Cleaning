import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/network/dio_client.dart';
import '../models/worker.dart';
import 'package:flutter/foundation.dart';

abstract class WorkerRepository {
  Future<Worker?> getMyWorkerProfile();
  Future<void> updateLocation(double lat, double lng);
  Future<List<Worker>> getRecommendedWorkers(String bookingId);

  /// Đăng ký thông tin định danh và kỹ năng cho thợ
  Future<void> registerAsWorker({
    required String identityCardNumber,
    required List<Map<String, dynamic>> skills,
  });
}

class ApiWorkerRepository implements WorkerRepository {
  @override
  Future<Worker?> getMyWorkerProfile() async {
    try {
      final response = await DioClient.instance.get('/Workers/me');
      if (response.data != null) {
        return Worker.fromJson(response.data as Map<String, dynamic>);
      }
      return null;
    } catch (e) {
      return null;
    }
  }

  @override
  Future<void> updateLocation(double lat, double lng) async {
    try {
      await DioClient.instance.patch(
        '/Workers/location',
        data: {'latitude': lat, 'longitude': lng},
      );
    } catch (e) {
      // Bỏ qua lỗi update location ngầm
    }
  }

  @override
  Future<List<Worker>> getRecommendedWorkers(String bookingId) async {
    try {
      final response = await DioClient.instance.get('/Ai/recommended-workers/$bookingId');

      final raw = response.data;
      if (raw is List) {
        return raw
            .whereType<Map<String, dynamic>>()
            .map((json) => Worker.fromJson(json))
            .toList();
      }
      return [];
    } catch (e) {
      debugPrint('Error fetching recommended workers: $e');
      return [];
    }
  }

  @override
  Future<void> registerAsWorker({
    required String identityCardNumber,
    required List<Map<String, dynamic>> skills,
  }) async {
    try {
      // Backend sẽ mapping dữ liệu vào bảng worker_profiles và worker_skills
      await DioClient.instance.post(
        '/Workers/register',
        data: {
          'identityCardNumber': identityCardNumber,
          'skills': skills,
        },
      );
    } on DioException catch (e) {
      throw Exception(e.response?.data['message'] ?? 'Lỗi khi đăng ký thông tin thợ.');
    }
  }
}

final workerRepositoryProvider = Provider<WorkerRepository>((ref) {
  return ApiWorkerRepository();
});

final workerProfileProvider = FutureProvider<Worker?>((ref) async {
  return ref.read(workerRepositoryProvider).getMyWorkerProfile();
});

final recommendedWorkersProvider = FutureProvider.family<List<Worker>, String>((ref, bookingId) async {
  final repo = ref.read(workerRepositoryProvider);
  return repo.getRecommendedWorkers(bookingId);
});