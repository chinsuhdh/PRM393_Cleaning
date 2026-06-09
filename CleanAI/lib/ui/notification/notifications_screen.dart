import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../core/theme/app_colors.dart';
import '../../data/models/notification_item.dart';
import '../../data/repositories/notification_repository.dart';

class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // Sử dụng provider thật từ Repository
    final notificationsAsync = ref.watch(notificationsProvider);
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_rounded),
          onPressed: () => context.pop(),
        ),
        title: const Text('Notifications', style: TextStyle(fontWeight: FontWeight.w800)),
        actions: [
          TextButton(
            onPressed: () async {
              // Gọi API đánh dấu tất cả đã đọc (nếu backend hỗ trợ)
              // Tạm thời invalidate để load lại list
              ref.invalidate(notificationsProvider);
            },
            child: const Text('Mark all read'),
          ),
        ],
      ),
      body: notificationsAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (err, _) => Center(child: Text('Lỗi: $err')),
        data: (notifications) {
          final unreadCount = notifications.where((n) => n.isUnread).length;

          if (notifications.isEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.notifications_none_rounded,
                      size: 64, color: theme.colorScheme.onSurfaceVariant.withValues(alpha: 0.4)),
                  const SizedBox(height: 16),
                  Text('No notifications yet',
                      style: theme.textTheme.bodyLarge
                          ?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
                ],
              ),
            );
          }

          return ListView.separated(
            padding: const EdgeInsets.all(16),
            itemCount: notifications.length,
            separatorBuilder: (_, __) => const SizedBox(height: 12),
            itemBuilder: (context, i) {
              final notif = notifications[i];
              return InkWell(
                onTap: () async {
                  if (notif.isUnread) {
                    try {
                      await ref.read(notificationRepositoryProvider).markAsRead(notif.id);
                      ref.invalidate(notificationsProvider); // Cập nhật lại UI
                    } catch (e) {
                      // ignore
                    }
                  }
                },
                child: _NotificationRow(notification: notif),
              );
            },
          );
        },
      ),
    );
  }
}

class _NotificationRow extends StatelessWidget {
  final NotificationItem notification;
  const _NotificationRow({required this.notification});

  IconData _icon(String title) {
    final lowerTitle = title.toLowerCase();
    if (lowerTitle.contains('booking')) return Icons.event_available_rounded;
    if (lowerTitle.contains('worker')) return Icons.engineering_rounded;
    return Icons.campaign_rounded;
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: notification.isUnread
            ? kPrimary.withValues(alpha: 0.06)
            : theme.colorScheme.surface,
        borderRadius: BorderRadius.circular(16),
        border: notification.isUnread
            ? Border.all(color: kPrimary.withValues(alpha: 0.2))
            : Border.all(color: theme.colorScheme.surfaceContainerHighest),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 48,
            height: 48,
            decoration: BoxDecoration(
              color: notification.isUnread
                  ? kPrimaryContainer
                  : theme.colorScheme.surfaceContainerHighest,
              shape: BoxShape.circle,
            ),
            child: Icon(
              _icon(notification.title),
              color: notification.isUnread ? kPrimary : theme.colorScheme.onSurfaceVariant,
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        notification.title,
                        style: theme.textTheme.titleSmall?.copyWith(
                          fontWeight: notification.isUnread ? FontWeight.w700 : FontWeight.w500,
                        ),
                      ),
                    ),
                    if (notification.isUnread)
                      Container(
                        width: 10,
                        height: 10,
                        decoration: const BoxDecoration(color: kPrimary, shape: BoxShape.circle),
                      ),
                  ],
                ),
                const SizedBox(height: 4),
                Text(
                  notification.message,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: theme.colorScheme.onSurfaceVariant,
                    height: 1.4,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}