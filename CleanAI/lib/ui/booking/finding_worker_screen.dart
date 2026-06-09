import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../core/theme/app_colors.dart';
import '../../data/models/worker.dart';
import '../../data/repositories/worker_repository.dart';

class FindingWorkerScreen extends ConsumerWidget {
  final String bookingId;

  const FindingWorkerScreen({super.key, required this.bookingId});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    // SỬ DỤNG ID ĐỘNG ĐƯỢC TRUYỀN VÀO TỪ ROUTER
    final workersAsync = ref.watch(recommendedWorkersProvider(bookingId));
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('AI Matched Workers', style: TextStyle(fontWeight: FontWeight.w800)),
        leading: IconButton(
          icon: const Icon(Icons.close_rounded),
          onPressed: () => context.go('/home'), // Đóng về trang chủ
        ),
      ),
      body: workersAsync.when(
        data: (workers) {
          if (workers.isEmpty) {
            return Center(
              child: Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  Icon(Icons.search_off_rounded, size: 64, color: theme.colorScheme.onSurfaceVariant),
                  const SizedBox(height: 16),
                  Text(
                    'No suitable workers found yet.\nWe are expanding our network!',
                    textAlign: TextAlign.center,
                    style: TextStyle(color: theme.colorScheme.onSurfaceVariant),
                  ),
                ],
              ),
            );
          }

          return ListView.separated(
            padding: const EdgeInsets.all(20),
            itemCount: workers.length,
            separatorBuilder: (_, __) => const SizedBox(height: 16),
            itemBuilder: (context, i) => _MatchedWorkerCard(worker: workers[i], bookingId: bookingId),
          );
        },
        loading: () => Center(
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              const CircularProgressIndicator(),
              const SizedBox(height: 24),
              Text('AI is analyzing the best matches...',
                  style: theme.textTheme.titleMedium?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
            ],
          ),
        ),
        error: (e, _) => Center(child: Text('Error: $e')),
      ),
    );
  }
}

class _MatchedWorkerCard extends StatelessWidget {
  final Worker worker;
  final String bookingId;
  const _MatchedWorkerCard({required this.worker, required this.bookingId});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: theme.colorScheme.surface,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.06),
            blurRadius: 12,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              CircleAvatar(
                radius: 28,
                backgroundColor: kPrimaryContainer,
                child: Text(
                  worker.initials,
                  style: const TextStyle(color: kOnPrimaryContainer, fontWeight: FontWeight.w700, fontSize: 18),
                ),
              ),
              const SizedBox(width: 16),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      worker.name,
                      style: theme.textTheme.titleLarge?.copyWith(fontWeight: FontWeight.w700),
                    ),
                    Row(
                      children: [
                        const Icon(Icons.star_rounded, color: kTertiary, size: 18),
                        const SizedBox(width: 4),
                        Text(
                          '${worker.rating} (${worker.reviews} jobs)',
                          style: theme.textTheme.bodyMedium,
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                decoration: BoxDecoration(
                  color: kSecondaryContainer,
                  borderRadius: BorderRadius.circular(20),
                ),
                child: Text(
                  '${worker.matchPercentage}% Match',
                  style: theme.textTheme.labelMedium?.copyWith(
                    color: kOnSecondaryContainer,
                    fontWeight: FontWeight.w800,
                  ),
                ),
              ),
            ],
          ),
          const SizedBox(height: 16),
          FilledButton(
            onPressed: () {
              // Bấm vào để xác nhận chọn thợ này cho Booking
              ScaffoldMessenger.of(context).showSnackBar(
                SnackBar(content: Text('Selected ${worker.name} for your booking!')),
              );
              context.go('/home');
            },
            style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(48)),
            child: const Text('Select this Worker'),
          ),
        ],
      ),
    );
  }
}