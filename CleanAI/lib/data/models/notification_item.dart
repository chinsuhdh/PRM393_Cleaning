class NotificationItem {
  final String id;
  final String title;
  final String message;
  final bool isUnread;

  const NotificationItem({
    required this.id,
    required this.title,
    required this.message,
    this.isUnread = false,
  });

  NotificationItem copyWith({
    String? id,
    String? title,
    String? message,
    bool? isUnread,
  }) {
    return NotificationItem(
      id: id ?? this.id,
      title: title ?? this.title,
      message: message ?? this.message,
      isUnread: isUnread ?? this.isUnread,
    );
  }
}
