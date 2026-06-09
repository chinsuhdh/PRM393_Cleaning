import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/network/dio_client.dart';
import '../models/notification_item.dart';

class NotificationRepository {
  Future<List<NotificationItem>> getNotifications() async {
    try {
      final response = await DioClient.instance.get('/Notifications');
      // Giả sử API trả về list trực tiếp, nếu bọc trong 'data' thì đổi thành response.data['data']
      final raw = response.data;
      if (raw is List) {
        return raw.whereType<Map<String, dynamic>>().map((json) {
          return NotificationItem(
            id: json['id']?.toString() ?? '',
            title: json['title']?.toString() ?? 'Thông báo',
            message: json['message']?.toString() ?? '',
            isUnread: json['isUnread'] ?? false,
          );
        }).toList();
      }
      return [];
    } catch (e) {
      return [];
    }
  }

  Future<void> markAsRead(String notificationId) async {
    try {
      await DioClient.instance.patch('/Notifications/$notificationId/read');
    } catch (e) {
      // Ignore
    }
  }
}

final notificationRepositoryProvider = Provider<NotificationRepository>((ref) {
  return NotificationRepository();
});

final notificationsProvider = FutureProvider.autoDispose<List<NotificationItem>>((ref) async {
  return ref.read(notificationRepositoryProvider).getNotifications();
});