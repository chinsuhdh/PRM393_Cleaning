import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';

class WorkerWalletScreen extends StatelessWidget {
  const WorkerWalletScreen({super.key});

  static const List<Map<String, String>> _transactions = [
    {'title': 'Deep Cleaning - John Doe', 'amount': '+\$80', 'date': 'Oct 15, 2026', 'type': 'credit'},
    {'title': 'House Cleaning - Jane Smith', 'amount': '+\$60', 'date': 'Oct 14, 2026', 'type': 'credit'},
    {'title': 'Withdrawal to Bank', 'amount': '-\$100', 'date': 'Oct 13, 2026', 'type': 'debit'},
    {'title': 'Sofa Cleaning - Bob Johnson', 'amount': '+\$45', 'date': 'Oct 12, 2026', 'type': 'credit'},
    {'title': 'Carpet Cleaning - Alice Brown', 'amount': '+\$55', 'date': 'Oct 10, 2026', 'type': 'credit'},
  ];

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(
        title: const Text('Wallet',
            style: TextStyle(fontWeight: FontWeight.w800)),
      ),
      body: CustomScrollView(
        slivers: [
          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                children: [
                  // Balance card
                  Container(
                    width: double.infinity,
                    padding: const EdgeInsets.all(28),
                    decoration: BoxDecoration(
                      gradient: const LinearGradient(
                        colors: [kPrimary, Color(0xFF1D4ED8)],
                        begin: Alignment.topLeft,
                        end: Alignment.bottomRight,
                      ),
                      borderRadius: BorderRadius.circular(24),
                      boxShadow: [
                        BoxShadow(
                          color: kPrimary.withValues(alpha: 0.4),
                          blurRadius: 20,
                          offset: const Offset(0, 8),
                        ),
                      ],
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text('Available Balance',
                            style: TextStyle(
                                color: Colors.white.withValues(alpha: 0.8),
                                fontSize: 14)),
                        const SizedBox(height: 8),
                        const Text('\$240.00',
                            style: TextStyle(
                                color: Colors.white,
                                fontSize: 40,
                                fontWeight: FontWeight.w900,
                                letterSpacing: -1)),
                        const SizedBox(height: 20),
                        Row(
                          children: [
                            _BalanceStat(
                                label: 'This Month',
                                value: '\$1,240'),
                            const SizedBox(width: 24),
                            _BalanceStat(
                                label: 'Total Earned',
                                value: '\$8,420'),
                          ],
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                  // Action buttons
                  Row(
                    children: [
                      Expanded(
                        child: FilledButton.icon(
                          onPressed: () {},
                          icon: const Icon(Icons.arrow_upward_rounded),
                          label: const Text('Withdraw'),
                          style: FilledButton.styleFrom(
                              backgroundColor: kSecondary,
                              minimumSize: const Size.fromHeight(50)),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: OutlinedButton.icon(
                          onPressed: () {},
                          icon: const Icon(Icons.history_rounded),
                          label: const Text('Full History'),
                          style: OutlinedButton.styleFrom(
                              minimumSize: const Size.fromHeight(50),
                              shape: RoundedRectangleBorder(
                                  borderRadius: BorderRadius.circular(12))),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 24),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text('Transactions',
                          style: theme.textTheme.titleMedium
                              ?.copyWith(fontWeight: FontWeight.w700)),
                      TextButton(
                          onPressed: () {},
                          child: const Text('See All')),
                    ],
                  ),
                ],
              ),
            ),
          ),
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(20, 0, 20, 24),
            sliver: SliverList(
              delegate: SliverChildBuilderDelegate(
                (context, i) {
                  final tx = _transactions[i];
                  final isCredit = tx['type'] == 'credit';
                  return Card(
                    elevation: 0,
                    color: theme.colorScheme.surfaceContainerHighest,
                    margin: const EdgeInsets.only(bottom: 10),
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(14)),
                    child: ListTile(
                      leading: Container(
                        width: 44,
                        height: 44,
                        decoration: BoxDecoration(
                          color: isCredit
                              ? kSecondaryContainer
                              : const Color(0xFFFFDAD6),
                          borderRadius: BorderRadius.circular(12),
                        ),
                        child: Icon(
                          isCredit
                              ? Icons.arrow_downward_rounded
                              : Icons.arrow_upward_rounded,
                          color:
                              isCredit ? kSecondary : Colors.red,
                        ),
                      ),
                      title: Text(tx['title']!,
                          style: const TextStyle(
                              fontWeight: FontWeight.w500, fontSize: 14),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis),
                      subtitle: Text(tx['date']!),
                      trailing: Text(
                        tx['amount']!,
                        style: TextStyle(
                          color: isCredit ? kSecondary : Colors.red,
                          fontWeight: FontWeight.w800,
                          fontSize: 15,
                        ),
                      ),
                    ),
                  );
                },
                childCount: _transactions.length,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _BalanceStat extends StatelessWidget {
  final String label;
  final String value;
  const _BalanceStat({required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label,
            style: TextStyle(
                color: Colors.white.withValues(alpha: 0.7), fontSize: 11)),
        Text(value,
            style: const TextStyle(
                color: Colors.white,
                fontWeight: FontWeight.w700,
                fontSize: 16)),
      ],
    );
  }
}
