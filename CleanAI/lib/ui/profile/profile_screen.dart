import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../core/theme/app_colors.dart';
import '../../data/repositories/auth_repository.dart';
import '../../data/repositories/profile_repository.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final profileAsync = ref.watch(myProfileProvider);
    final authState = ref.watch(authProvider);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Profile',
            style: TextStyle(fontWeight: FontWeight.w800)),
        actions: [
          IconButton(
            icon: const Icon(Icons.settings_outlined),
            onPressed: () {},
          ),
        ],
      ),
      body: profileAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (_, __) {
          // Fallback to auth state info if profile API fails
          final name = authState.userName ?? 'User';
          final initials = name.isNotEmpty ? name[0].toUpperCase() : '?';
          return _ProfileBody(
            name: name,
            email: '', // Để trống chờ BE
            phone: '', // Để trống chờ BE
            initials: initials,
            avatarUrl: null,
          );
        },
        data: (profile) => _ProfileBody(
          name: profile.fullName,
          // Sử dụng dữ liệu từ BE, nếu property chưa có thì fallback về chuỗi rỗng
          email: profile.email ?? '',
          phone: profile.phoneNumber ?? '',
          initials: profile.initials,
          avatarUrl: profile.avatarUrl, // Nhận avatarUrl từ backend
        ),
      ),
    );
  }
}

class _ProfileBody extends StatelessWidget {
  final String name;
  final String email;
  final String phone;
  final String initials;
  final String? avatarUrl;

  const _ProfileBody({
    required this.name,
    required this.email,
    required this.phone,
    required this.initials,
    this.avatarUrl,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    // Kiểm tra xem có avatar hợp lệ không
    final hasAvatar = avatarUrl != null && avatarUrl!.isNotEmpty;

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        // Profile header card
        Card(
          elevation: 0,
          color: kPrimaryContainer,
          shape:
          RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Row(
              children: [
                CircleAvatar(
                  radius: 40,
                  backgroundColor: kPrimary,
                  backgroundImage: hasAvatar ? NetworkImage(avatarUrl!) : null,
                  child: !hasAvatar
                      ? Text(
                    initials,
                    style: const TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.w800,
                      fontSize: 24,
                    ),
                  )
                      : null,
                ),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        name,
                        style: theme.textTheme.titleLarge?.copyWith(
                          fontWeight: FontWeight.w800,
                          color: kOnPrimaryContainer,
                        ),
                      ),
                      if (email.isNotEmpty) ...[
                        const SizedBox(height: 4),
                        Text(
                          email,
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: kOnPrimaryContainer.withValues(alpha: 0.8),
                          ),
                        ),
                      ],
                      if (phone.isNotEmpty) ...[
                        const SizedBox(height: 2),
                        Text(
                          phone,
                          style: theme.textTheme.bodySmall?.copyWith(
                            color: kOnPrimaryContainer.withValues(alpha: 0.8),
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 8),
        // Stats row
        Row(
          children: [
            Expanded(child: _StatCard(value: '—', label: 'Bookings')),
            const SizedBox(width: 12),
            Expanded(child: _StatCard(value: '—', label: 'Rating')),
            const SizedBox(width: 12),
            Expanded(child: _StatCard(value: '—', label: 'Saved')),
          ],
        ),
        const SizedBox(height: 20),
        // Menu section
        _SectionHeader(title: 'Account'),
        _ProfileMenuItem(
          icon: Icons.person_outline_rounded,
          title: 'Edit Profile',
          onTap: () {},
        ),
        _ProfileMenuItem(
          icon: Icons.location_on_outlined,
          title: 'Saved Addresses',
          onTap: () => context.push('/address'),
        ),
        _ProfileMenuItem(
          icon: Icons.credit_card_outlined,
          title: 'Payment Methods',
          onTap: () {},
        ),
        _ProfileMenuItem(
          icon: Icons.history_rounded,
          title: 'Booking History',
          onTap: () => context.push('/bookings'),
        ),
        const SizedBox(height: 8),
        _SectionHeader(title: 'Preferences'),
        _ProfileMenuItem(
          icon: Icons.notifications_outlined,
          title: 'Notifications',
          onTap: () {},
        ),
        _ProfileMenuItem(
          icon: Icons.dark_mode_outlined,
          title: 'Appearance',
          onTap: () {},
        ),
        _ProfileMenuItem(
          icon: Icons.language_outlined,
          title: 'Language',
          subtitle: 'English',
          onTap: () {},
        ),
        const SizedBox(height: 8),
        _SectionHeader(title: 'Support'),
        _ProfileMenuItem(
          icon: Icons.help_outline_rounded,
          title: 'Help & Support',
          onTap: () {},
        ),
        _ProfileMenuItem(
          icon: Icons.privacy_tip_outlined,
          title: 'Privacy Policy',
          onTap: () {},
        ),
        const SizedBox(height: 24),
        // Log out
        OutlinedButton.icon(
          onPressed: () => context.go('/login'),
          icon: const Icon(Icons.logout_rounded),
          label: const Text('Log Out'),
          style: OutlinedButton.styleFrom(
            minimumSize: const Size.fromHeight(52),
            foregroundColor: Colors.red,
            side: const BorderSide(color: Colors.red),
            shape:
            RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          ),
        ),
        const SizedBox(height: 32),
      ],
    );
  }
}

class _StatCard extends StatelessWidget {
  final String value;
  final String label;
  const _StatCard({required this.value, required this.label});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Card(
      elevation: 0,
      color: theme.colorScheme.surfaceContainerHighest,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 16),
        child: Column(
          children: [
            Text(value,
                style: theme.textTheme.titleLarge?.copyWith(
                  fontWeight: FontWeight.w800,
                  color: kPrimary,
                )),
            const SizedBox(height: 4),
            Text(label,
                style: theme.textTheme.bodySmall?.copyWith(
                  color: theme.colorScheme.onSurfaceVariant,
                )),
          ],
        ),
      ),
    );
  }
}

class _SectionHeader extends StatelessWidget {
  final String title;
  const _SectionHeader({required this.title});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(left: 4, bottom: 8, top: 8),
      child: Text(
        title,
        style: Theme.of(context).textTheme.labelLarge?.copyWith(
          color: Theme.of(context).colorScheme.onSurfaceVariant,
          letterSpacing: 0.5,
        ),
      ),
    );
  }
}

class _ProfileMenuItem extends StatelessWidget {
  final IconData icon;
  final String title;
  final String? subtitle;
  final VoidCallback onTap;

  const _ProfileMenuItem({
    required this.icon,
    required this.title,
    this.subtitle,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return ListTile(
      contentPadding: const EdgeInsets.symmetric(horizontal: 4, vertical: 2),
      leading: Container(
        width: 40,
        height: 40,
        decoration: BoxDecoration(
          color: theme.colorScheme.surfaceContainerHighest,
          borderRadius: BorderRadius.circular(12),
        ),
        child: Icon(icon,
            color: theme.colorScheme.onSurfaceVariant, size: 22),
      ),
      title: Text(title,
          style: theme.textTheme.bodyLarge
              ?.copyWith(fontWeight: FontWeight.w500)),
      subtitle: subtitle != null
          ? Text(subtitle!,
          style: TextStyle(color: theme.colorScheme.onSurfaceVariant))
          : null,
      trailing:
      Icon(Icons.chevron_right_rounded, color: theme.colorScheme.onSurfaceVariant),
      onTap: onTap,
    );
  }
}