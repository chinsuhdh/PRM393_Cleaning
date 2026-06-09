import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/network/dio_client.dart';

// Provider cung cấp instance của AdminRepository
final adminRepositoryProvider = Provider<AdminRepository>((ref) {
  return AdminRepository();
});

// Provider tự động gọi API và quản lý state (Loading, Data, Error) cho giao diện
final adminStatsProvider = FutureProvider.autoDispose<Map<String, dynamic>>((ref) async {
  final repo = ref.read(adminRepositoryProvider);
  return repo.getDashboardStats();
});

class AdminRepository {
  // Hàm gọi API tới ASP.NET Core Backend
  Future<Map<String, dynamic>> getDashboardStats() async {
    try {
      // Đường dẫn gọi đến AdminController mà bạn đã viết ở backend
      final response = await DioClient.instance.get('/Admin/dashboard-stats');

      // Trả về dữ liệu kiểu Map (JSON)
      return response.data as Map<String, dynamic>;
    } catch (e) {
      throw Exception('Không thể tải dữ liệu thống kê: $e');
    }
  }
}