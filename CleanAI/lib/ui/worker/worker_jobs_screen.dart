import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';

class WorkerJobsScreen extends StatefulWidget {
  const WorkerJobsScreen({super.key});

  @override
  State<WorkerJobsScreen> createState() => _WorkerJobsScreenState();
}

class _WorkerJobsScreenState extends State<WorkerJobsScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  final List<String> _tabs = ['Active', 'Pending', 'Completed'];

  static const List<Map<String, String>> _jobs = [
    {'customer': 'John Doe', 'service': 'Deep Cleaning', 'address': '123 Main St, NY', 'time': '09:00 AM', 'status': 'Active', 'amount': '\$80'},
    {'customer': 'Jane Smith', 'service': 'Sofa Cleaning', 'address': '456 Oak Ave, NY', 'time': '11:00 AM', 'status': 'Pending', 'amount': '\$45'},
    {'customer': 'Bob Johnson', 'service': 'House Cleaning', 'address': '789 Pine Rd, NY', 'time': '02:00 PM', 'status': 'Completed', 'amount': '\$60'},
    {'customer': 'Alice Brown', 'service': 'Carpet Cleaning', 'address': '321 Elm St, NY', 'time': '04:00 PM', 'status': 'Completed', 'amount': '\$55'},
  ];

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: _tabs.length, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('My Jobs',
            style: TextStyle(fontWeight: FontWeight.w800)),
        bottom: TabBar(
          controller: _tabController,
          tabs: _tabs.map((t) => Tab(text: t)).toList(),
          indicatorColor: kPrimary,
          labelColor: kPrimary,
          unselectedLabelColor: Colors.grey,
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: _tabs.map((status) {
          final filtered =
              _jobs.where((j) => j['status'] == status).toList();
          if (filtered.isEmpty) {
            return Center(
              child: Text('No $status jobs',
                  style: TextStyle(
                      color: Theme.of(context).colorScheme.onSurfaceVariant)),
            );
          }
          return ListView.separated(
            padding: const EdgeInsets.all(16),
            itemCount: filtered.length,
            separatorBuilder: (_, __) => const SizedBox(height: 12),
            itemBuilder: (context, i) => _JobCard(job: filtered[i]),
          );
        }).toList(),
      ),
    );
  }
}

class _JobCard extends StatelessWidget {
  final Map<String, String> job;
  const _JobCard({required this.job});

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final isActive = job['status'] == 'Active';
    final isPending = job['status'] == 'Pending';
    return Card(
      elevation: 0,
      color: theme.colorScheme.surfaceContainerHighest,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text(job['service']!,
                    style: theme.textTheme.titleMedium
                        ?.copyWith(fontWeight: FontWeight.w700)),
                Text(job['amount']!,
                    style: theme.textTheme.titleMedium?.copyWith(
                        color: kPrimary, fontWeight: FontWeight.w800)),
              ],
            ),
            const SizedBox(height: 8),
            Row(children: [
              const Icon(Icons.person_rounded, size: 16, color: kPrimary),
              const SizedBox(width: 6),
              Text(job['customer']!,
                  style: theme.textTheme.bodyMedium),
            ]),
            const SizedBox(height: 4),
            Row(children: [
              const Icon(Icons.location_on_rounded, size: 16, color: kPrimary),
              const SizedBox(width: 6),
              Text(job['address']!, style: theme.textTheme.bodySmall),
            ]),
            const SizedBox(height: 4),
            Row(children: [
              const Icon(Icons.access_time_rounded, size: 16, color: kPrimary),
              const SizedBox(width: 6),
              Text(job['time']!, style: theme.textTheme.bodySmall),
            ]),
            if (isActive || isPending) ...[
              const SizedBox(height: 12),
              Row(children: [
                if (isActive)
                  Expanded(
                    child: FilledButton.icon(
                      onPressed: () {},
                      icon: const Icon(Icons.check_rounded, size: 18),
                      label: const Text('Mark Complete'),
                      style: FilledButton.styleFrom(
                          backgroundColor: kSecondary,
                          minimumSize: const Size(0, 44)),
                    ),
                  ),
                if (isPending) ...[
                  Expanded(
                    child: FilledButton.icon(
                      onPressed: () {},
                      icon: const Icon(Icons.check_rounded, size: 18),
                      label: const Text('Accept'),
                      style: FilledButton.styleFrom(
                          minimumSize: const Size(0, 44)),
                    ),
                  ),
                  const SizedBox(width: 8),
                  OutlinedButton(
                    onPressed: () {},
                    style: OutlinedButton.styleFrom(
                        minimumSize: const Size(0, 44),
                        foregroundColor: Colors.red,
                        side: const BorderSide(color: Colors.red)),
                    child: const Text('Decline'),
                  ),
                ],
              ]),
            ],
          ],
        ),
      ),
    );
  }
}
