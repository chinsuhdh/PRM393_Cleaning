import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../core/theme/app_colors.dart';
import '../../data/repositories/admin_repository.dart';
import '../../data/repositories/auth_repository.dart';

class AdminDashboardScreen extends ConsumerWidget {
  const AdminDashboardScreen({super.key});

  // Khởi tạo Activity tạm thời trống cho đến khi viết API log hoạt động gần đây
  static const List<Map<String, String>> _activity = [];

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);

    // Lắng nghe Provider thống kê từ AdminRepository
    final statsAsync = ref.watch(adminStatsProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Admin Dashboard',
            style: TextStyle(fontWeight: FontWeight.w800)),
        actions: [
          IconButton(
            icon: const Icon(Icons.logout_rounded),
            tooltip: 'Đăng xuất',
            onPressed: () {
              // Gọi hàm logout void đồng bộ để xoá token và reset state
              ref.read(authProvider.notifier).logout();
              context.go('/login');
            },
          ),
        ],
      ),
      body: CustomScrollView(
        slivers: [
          // 1. Welcome banner hiển thị tổng quan hệ thống
          SliverToBoxAdapter(
            child: Container(
              margin: const EdgeInsets.all(16),
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                gradient: const LinearGradient(
                  colors: [Color(0xFF1E1B4B), Color(0xFF312E81)],
                  begin: Alignment.topLeft,
                  end: Alignment.bottomRight,
                ),
                borderRadius: BorderRadius.circular(20),
              ),
              child: Row(
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Welcome, Admin',
                            style: TextStyle(
                                color: Colors.white.withValues(alpha: 0.8),
                                fontSize: 13)),
                        const SizedBox(height: 4),
                        const Text('Platform Overview',
                            style: TextStyle(
                                color: Colors.white,
                                fontSize: 20,
                                fontWeight: FontWeight.w800)),
                        const SizedBox(height: 4),
                        Text(DateTime.now().toString().split(' ')[0],
                            style: TextStyle(
                                color: Colors.white.withValues(alpha: 0.6),
                                fontSize: 12)),
                      ],
                    ),
                  ),
                  Container(
                    width: 56,
                    height: 56,
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.15),
                      shape: BoxShape.circle,
                    ),
                    child: const Icon(
                        Icons.admin_panel_settings_rounded,
                        color: Colors.white,
                        size: 30),
                  ),
                ],
              ),
            ),
          ),

          // 2. Grid thống kê dữ liệu thời gian thực lấy từ PostgreSQL
          statsAsync.when(
            loading: () => const SliverToBoxAdapter(
              child: Center(
                child: Padding(
                  padding: EdgeInsets.all(32.0),
                  child: CircularProgressIndicator(),
                ),
              ),
            ),
            error: (err, _) => SliverToBoxAdapter(
              child: Center(
                child: Padding(
                  padding: const EdgeInsets.all(16.0),
                  child: Text('Lỗi tải thống kê: $err',
                      style: const TextStyle(color: Colors.red, fontWeight: FontWeight.bold)),
                ),
              ),
            ),
            data: (data) {
              // Ánh xạ dữ liệu JSON trả về từ ASP.NET Core Controller sang List cục bộ
              final List<Map<String, dynamic>> realStats = [
                {
                  'icon': Icons.people_rounded,
                  'label': 'Total Clients',
                  'value': data['totalClients']?.toString() ?? '0',
                  'change': 'Live',
                  'color': kPrimary
                },
                {
                  'icon': Icons.engineering_rounded,
                  'label': 'Total Workers',
                  'value': data['totalWorkers']?.toString() ?? '0',
                  'change': 'Live',
                  'color': kSecondary
                },
                {
                  'icon': Icons.list_alt_rounded,
                  'label': 'Total Bookings',
                  'value': data['totalBookings']?.toString() ?? '0',
                  'change': 'Live',
                  'color': kTertiary
                },
                {
                  'icon': Icons.attach_money_rounded,
                  'label': 'Total Revenue',
                  'value': '\$${data['totalRevenue']?.toString() ?? '0.00'}',
                  'change': 'Done',
                  'color': Colors.green
                },
              ];

              return SliverPadding(
                padding: const EdgeInsets.symmetric(horizontal: 16),
                sliver: SliverGrid(
                  gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                    crossAxisCount: 2,
                    crossAxisSpacing: 12,
                    mainAxisSpacing: 12,
                    childAspectRatio: 1.4,
                  ),
                  delegate: SliverChildListDelegate(
                    realStats.map((stat) => _StatCard(stat: stat)).toList(),
                  ),
                ),
              );
            },
          ),

          // 3. Phân hệ điều hướng quản lý nhanh (Quick Management)
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 24, 16, 12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Management',
                      style: theme.textTheme.titleMedium
                          ?.copyWith(fontWeight: FontWeight.w700)),
                  const SizedBox(height: 12),
                  Row(
                    children: [
                      Expanded(child: _ManageButton(icon: Icons.people_rounded, label: 'Users', onTap: () {})),
                      const SizedBox(width: 10),
                      Expanded(child: _ManageButton(icon: Icons.engineering_rounded, label: 'Workers', onTap: () {})),
                      const SizedBox(width: 10),
                      Expanded(child: _ManageButton(icon: Icons.list_alt_rounded, label: 'Bookings', onTap: () {})),
                      const SizedBox(width: 10),
                      Expanded(child: _ManageButton(icon: Icons.bar_chart_rounded, label: 'Reports', onTap: () {})),
                    ],
                  ),
                ],
              ),
            ),
          ),

          // 4. Luồng hoạt động gần đây của toàn hệ thống
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
              child: Text('Recent Activity',
                  style: theme.textTheme.titleMedium
                      ?.copyWith(fontWeight: FontWeight.w700)),
            ),
          ),
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 0, 16, 32),
            sliver: SliverList(
              delegate: SliverChildListDelegate(
                _activity.isEmpty
                    ? [
                  Padding(
                    padding: const EdgeInsets.all(16.0),
                    child: Center(
                      child: Text('Chưa có hoạt động nào gần đây.',
                          style: TextStyle(color: theme.colorScheme.onSurfaceVariant, fontSize: 13)),
                    ),
                  )
                ]
                    : _activity.map((a) => _ActivityRow(activity: a)).toList(),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  final Map<String, dynamic> stat;
  const _StatCard({required this.stat});

  @override
  Widget build(BuildContext context) {
    final color = stat['color'] as Color;
    return Card(
      elevation: 0,
      color: color.withValues(alpha: 0.1),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Icon(stat['icon'] as IconData, color: color, size: 24),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                  decoration: BoxDecoration(
                    color: color.withValues(alpha: 0.15),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text(stat['change']!,
                      style: TextStyle(
                          color: color,
                          fontWeight: FontWeight.w700,
                          fontSize: 10)),
                ),
              ],
            ),
            const Spacer(),
            Text(stat['value']!,
                style: TextStyle(
                    fontSize: 22,
                    fontWeight: FontWeight.w900,
                    color: color)),
            Text(stat['label']!,
                style: TextStyle(
                    fontSize: 11, color: color.withValues(alpha: 0.8))),
          ],
        ),
      ),
    );
  }
}

class _ManageButton extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;
  const _ManageButton({required this.icon, required this.label, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return GestureDetector(
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
              style: const TextStyle(fontSize: 11, fontWeight: FontWeight.w500)),
        ],
      ),
    );
  }
}

class _ActivityRow extends StatelessWidget {
  final Map<String, String> activity;
  const _ActivityRow({required this.activity});

  IconData _icon(String type) {
    switch (type) {
      case 'booking': return Icons.event_available_rounded;
      case 'user': return Icons.person_add_rounded;
      case 'worker': return Icons.verified_rounded;
      case 'cancel': return Icons.cancel_rounded;
      case 'revenue': return Icons.trending_up_rounded;
      default: return Icons.info_rounded;
    }
  }

  Color _color(String type) {
    switch (type) {
      case 'booking': return kPrimary;
      case 'user': return kSecondary;
      case 'worker': return kTertiary;
      case 'cancel': return Colors.red;
      case 'revenue': return const Color(0xFF8B5CF6);
      default: return Colors.grey;
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final color = _color(activity['type']!);
    return Padding(
      padding: const EdgeInsets.only(bottom: 12),
      child: Row(
        children: [
          Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              color: color.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(12),
            ),
            child: Icon(_icon(activity['type']!), color: color, size: 20),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(activity['title']!,
                    style: theme.textTheme.bodyMedium
                        ?.copyWith(fontWeight: FontWeight.w600)),
                Text(activity['subtitle']!,
                    style: theme.textTheme.bodySmall?.copyWith(
                        color: theme.colorScheme.onSurfaceVariant)),
              ],
            ),
          ),
        ],
      ),
    );
  }
}