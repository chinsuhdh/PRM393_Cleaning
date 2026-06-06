import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import '../../core/theme/app_colors.dart';

class AdminDashboardScreen extends StatelessWidget {
  const AdminDashboardScreen({super.key});

  static const List<Map<String, dynamic>> _stats = [
    {'icon': Icons.people_rounded, 'label': 'Total Users', 'value': '1,248', 'change': '+12%', 'color': kPrimary},
    {'icon': Icons.event_available_rounded, 'label': 'Active Bookings', 'value': '87', 'change': '+5%', 'color': kSecondary},
    {'icon': Icons.attach_money_rounded, 'label': 'Revenue', 'value': '\$24.5K', 'change': '+18%', 'color': kTertiary},
    {'icon': Icons.engineering_rounded, 'label': 'Workers', 'value': '342', 'change': '+7%', 'color': Color(0xFF8B5CF6)},
  ];

  static const List<Map<String, String>> _activity = [
    {'icon': 'booking', 'title': 'New booking: Deep Cleaning', 'subtitle': 'John Doe · 2 min ago', 'type': 'booking'},
    {'icon': 'user', 'title': 'New user registered', 'subtitle': 'sarah@example.com · 15 min ago', 'type': 'user'},
    {'icon': 'worker', 'title': 'Worker verified', 'subtitle': 'Maria Garcia · 1 hr ago', 'type': 'worker'},
    {'icon': 'booking', 'title': 'Booking cancelled', 'subtitle': 'Bob Johnson · 2 hr ago', 'type': 'cancel'},
    {'icon': 'revenue', 'title': 'Revenue milestone reached', 'subtitle': '\$25K total · 3 hr ago', 'type': 'revenue'},
  ];

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Admin Dashboard',
            style: TextStyle(fontWeight: FontWeight.w800)),
        actions: [
          IconButton(icon: const Icon(Icons.logout_rounded), onPressed: () => context.go('/login')),
        ],
      ),
      body: CustomScrollView(
        slivers: [
          // Welcome banner
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
                        Text('Friday, Oct 6, 2026',
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

          // Stats grid
          SliverPadding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            sliver: SliverGrid(
              gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                crossAxisCount: 2,
                crossAxisSpacing: 12,
                mainAxisSpacing: 12,
                childAspectRatio: 1.4,
              ),
              delegate: SliverChildListDelegate(
                _stats.map((stat) => _StatCard(stat: stat)).toList(),
              ),
            ),
          ),

          // Quick management
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

          // Recent Activity
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
                _activity
                    .map((a) => _ActivityRow(activity: a))
                    .toList(),
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
