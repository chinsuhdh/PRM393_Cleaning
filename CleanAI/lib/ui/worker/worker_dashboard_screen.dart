import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../core/theme/app_colors.dart';
import '../../data/repositories/auth_repository.dart';
import '../../data/repositories/booking_repository.dart';
import '../../data/models/booking.dart';

class WorkerDashboardScreen extends ConsumerWidget {
  const WorkerDashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);

    // Lấy thông tin user thật từ Provider
    final authState = ref.watch(authProvider);
    final userName = authState.userName ?? 'Worker';
    final initials = userName.isNotEmpty ? userName[0].toUpperCase() : 'W';

    // Gọi API lấy danh sách các đơn hàng "Available" (Pending & chưa có thợ)
    final availableJobsAsync = ref.watch(availableBookingsProvider);

    return Scaffold(
      body: CustomScrollView(
        slivers: [
          // App bar with greeting & Logout button
          SliverAppBar(
            expandedHeight: 180,
            pinned: false,
            actions: [
              IconButton(
                icon: const Icon(Icons.logout_rounded, color: Colors.white),
                tooltip: 'Đăng xuất',
                onPressed: () async {
                  // Hiển thị dialog xác nhận đăng xuất
                  final confirm = await showDialog<bool>(
                    context: context,
                    builder: (context) => AlertDialog(
                      title: const Text('Đăng xuất'),
                      content: const Text('Bạn có chắc chắn muốn đăng xuất khỏi tài khoản này?'),
                      actions: [
                        TextButton(
                          onPressed: () => Navigator.pop(context, false),
                          child: const Text('Hủy'),
                        ),
                        FilledButton(
                          onPressed: () => Navigator.pop(context, true),
                          style: FilledButton.styleFrom(backgroundColor: Colors.red),
                          child: const Text('Đăng xuất'),
                        ),
                      ],
                    ),
                  );

                  if (confirm == true) {
                    // Gọi hàm logout từ AuthProvider
                     ref.read(authProvider.notifier).logout();

                    // Chuyển hướng về trang Đăng nhập
                    // (Lưu ý: Đảm bảo GoRouter của bạn có cấu hình route '/login')
                    if (context.mounted) {
                      context.go('/login');
                    }
                  }
                },
              ),
            ],
            flexibleSpace: FlexibleSpaceBar(
              background: Container(
                decoration: const BoxDecoration(
                  gradient: LinearGradient(
                    colors: [kPrimary, Color(0xFF1D4ED8)],
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                  ),
                ),
                padding: const EdgeInsets.fromLTRB(20, 56, 20, 20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text('Good Morning,',
                                style: TextStyle(
                                    color: Colors.white.withValues(alpha: 0.85),
                                    fontSize: 14)),
                            Text(userName, // Hiển thị tên thật
                                style: const TextStyle(
                                    color: Colors.white,
                                    fontSize: 22,
                                    fontWeight: FontWeight.w800)),
                          ],
                        ),
                        CircleAvatar(
                          radius: 24,
                          backgroundColor: Colors.white.withValues(alpha: 0.2),
                          child: Text(initials, // Chữ cái đầu thật
                              style: const TextStyle(
                                  color: Colors.white,
                                  fontWeight: FontWeight.w700)),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 12, vertical: 6),
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.2),
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: const [
                          Icon(Icons.circle, color: kSecondary, size: 10),
                          SizedBox(width: 6),
                          Text('Available',
                              style: TextStyle(
                                  color: Colors.white,
                                  fontWeight: FontWeight.w600,
                                  fontSize: 12)),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),

          SliverPadding(
            padding: const EdgeInsets.all(16),
            sliver: SliverList(
              delegate: SliverChildListDelegate([
                // Stats cards (Tạm để 0)
                Row(
                  children: [
                    Expanded(
                        child: _StatCard(
                            icon: Icons.attach_money_rounded,
                            label: "Today's Earn",
                            value: '\$0',
                            color: kSecondary)),
                    const SizedBox(width: 12),
                    Expanded(
                        child: _StatCard(
                            icon: Icons.work_rounded,
                            label: 'Jobs Today',
                            value: '0',
                            color: kPrimary)),
                    const SizedBox(width: 12),
                    Expanded(
                        child: _StatCard(
                            icon: Icons.star_rounded,
                            label: 'Rating',
                            value: '0.0',
                            color: kTertiary)),
                  ],
                ),
                const SizedBox(height: 24),
                // Quick actions
                Text('Quick Actions',
                    style: theme.textTheme.titleMedium
                        ?.copyWith(fontWeight: FontWeight.w700)),
                const SizedBox(height: 12),
                Row(
                  children: [
                    _QuickAction(
                        icon: Icons.work_rounded,
                        label: 'My Jobs',
                        onTap: () => context.push('/worker/jobs')),
                    const SizedBox(width: 12),
                    _QuickAction(
                        icon: Icons.account_balance_wallet_rounded,
                        label: 'Wallet',
                        onTap: () => context.push('/worker/wallet')),
                    const SizedBox(width: 12),
                    _QuickAction(
                        icon: Icons.schedule_rounded,
                        label: 'Schedule',
                        onTap: () {}),
                    const SizedBox(width: 12),
                    _QuickAction(
                        icon: Icons.support_agent_rounded,
                        label: 'Support',
                        onTap: () {}),
                  ],
                ),
                const SizedBox(height: 24),

                // Available jobs
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text('Available Jobs',
                        style: theme.textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.w700)),
                    TextButton(
                        onPressed: () => ref.refresh(availableBookingsProvider),
                        child: const Text('Refresh')
                    ),
                  ],
                ),
                const SizedBox(height: 12),

                // Render danh sách công việc thật từ API
                availableJobsAsync.when(
                  loading: () => const Center(
                    child: Padding(
                      padding: EdgeInsets.all(24.0),
                      child: CircularProgressIndicator(),
                    ),
                  ),
                  error: (err, stack) => Center(
                    child: Padding(
                      padding: const EdgeInsets.all(24.0),
                      child: Text('Lỗi tải đơn hàng: $err', style: const TextStyle(color: Colors.red)),
                    ),
                  ),
                  data: (jobs) {
                    if (jobs.isEmpty) {
                      return Center(
                        child: Padding(
                          padding: const EdgeInsets.all(24.0),
                          child: Text('Hiện không có đơn đặt lịch nào khả dụng.',
                              style: TextStyle(color: theme.colorScheme.onSurfaceVariant)),
                        ),
                      );
                    }
                    return Column(
                      children: jobs.map((job) => _JobMiniCard(job: job)).toList(),
                    );
                  },
                ),
              ]),
            ),
          ),
        ],
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color color;
  const _StatCard({required this.icon, required this.label, required this.value, required this.color});

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 0,
      color: color.withValues(alpha: 0.1),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, color: color, size: 24),
            const SizedBox(height: 8),
            Text(value,
                style: TextStyle(
                    fontSize: 20, fontWeight: FontWeight.w800, color: color)),
            Text(label,
                style: TextStyle(
                    fontSize: 11, color: color.withValues(alpha: 0.8))),
          ],
        ),
      ),
    );
  }
}

class _QuickAction extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;
  const _QuickAction({required this.icon, required this.label, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Expanded(
      child: GestureDetector(
        onTap: onTap,
        child: Column(
          children: [
            Container(
              width: 52,
              height: 52,
              decoration: BoxDecoration(
                color: theme.colorScheme.surfaceContainerHighest,
                borderRadius: BorderRadius.circular(14),
              ),
              child: Icon(icon, color: kPrimary),
            ),
            const SizedBox(height: 6),
            Text(label,
                style: const TextStyle(
                    fontSize: 11, fontWeight: FontWeight.w500),
                textAlign: TextAlign.center),
          ],
        ),
      ),
    );
  }
}

class _JobMiniCard extends StatelessWidget {
  final Booking job; // Model thật
  const _JobMiniCard({required this.job});

  Color _statusColor(String status) {
    if (status == 'Pending' || status == 'Upcoming') return Colors.orange;
    if (status == 'Accepted' || status == 'InProgress') return kSecondary;
    if (status == 'Completed') return kPrimary;
    return Colors.grey;
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    // Lấy ký tự đầu tiên của tên dịch vụ để làm Avatar
    final initial = job.serviceName.isNotEmpty ? job.serviceName[0].toUpperCase() : 'J';

    return Card(
      elevation: 0,
      color: theme.colorScheme.surfaceContainerHighest,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
      margin: const EdgeInsets.only(bottom: 12),
      child: ListTile(
        // Khi thợ bấm vào đơn, điều hướng sang trang Chi tiết để xem và Nhận đơn
        onTap: () => context.push('/booking/${job.id}'),
        leading: CircleAvatar(
          backgroundColor: kPrimaryContainer,
          child: Text(
            initial,
            style: const TextStyle(
                color: kOnPrimaryContainer, fontWeight: FontWeight.w700),
          ),
        ),
        title: Text(job.serviceName,
            style: const TextStyle(fontWeight: FontWeight.w600)),
        subtitle: Text('${job.date} · ${job.time}'),
        trailing: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text('${job.price} VND',
                style: const TextStyle(
                    fontWeight: FontWeight.w700, color: kPrimary)),
            const SizedBox(height: 4),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
              decoration: BoxDecoration(
                color: _statusColor(job.status).withValues(alpha: 0.15),
                borderRadius: BorderRadius.circular(6),
              ),
              child: Text(
                job.status,
                style: TextStyle(
                    fontSize: 10,
                    color: _statusColor(job.status),
                    fontWeight: FontWeight.w700),
              ),
            ),
          ],
        ),
      ),
    );
  }
}