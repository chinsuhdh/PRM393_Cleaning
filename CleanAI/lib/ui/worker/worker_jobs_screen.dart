import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:url_launcher/url_launcher.dart'; // THƯ VIỆN MỞ LINK
import '../../core/theme/app_colors.dart';
import '../../data/models/booking.dart';
import '../../data/repositories/booking_repository.dart';

class WorkerJobsScreen extends ConsumerStatefulWidget {
  const WorkerJobsScreen({super.key});

  @override
  ConsumerState<WorkerJobsScreen> createState() => _WorkerJobsScreenState();
}

class _WorkerJobsScreenState extends ConsumerState<WorkerJobsScreen> with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
  }

  @override
  Widget build(BuildContext context) {
    // Gọi 2 nguồn dữ liệu khác biệt
    final myBookingsAsync = ref.watch(workerBookingsProvider); // Đơn của tôi (Active, Completed)
    final availableBookingsAsync = ref.watch(availableBookingsProvider); // Đơn đang trống chờ nhận (Pending)

    return Scaffold(
      appBar: AppBar(
        title: const Text('My Jobs', style: TextStyle(fontWeight: FontWeight.w800)),
        bottom: TabBar(
          controller: _tabController,
          labelColor: kPrimary,
          unselectedLabelColor: Colors.grey,
          indicatorColor: kPrimary,
          tabs: const [
            Tab(text: 'Active'),
            Tab(text: 'Available'),
            Tab(text: 'Completed')
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [
          // TAB 1: ACTIVE
          myBookingsAsync.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (err, _) => Center(child: Text('Lỗi: $err')),
            data: (bookings) => _buildJobList(
              bookings.where((b) => b.status == 'Accepted' || b.status == 'InProgress').toList(),
              isAvailableTab: false,
            ),
          ),

          // TAB 2: AVAILABLE
          availableBookingsAsync.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (err, _) => Center(child: Text('Lỗi: $err')),
            data: (bookings) => _buildJobList(
              bookings.where((b) => b.status == 'Pending').toList(),
              isAvailableTab: true,
            ),
          ),

          // TAB 3: COMPLETED
          myBookingsAsync.when(
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (err, _) => Center(child: Text('Lỗi: $err')),
            data: (bookings) => _buildJobList(
              bookings.where((b) => b.status == 'Completed').toList(),
              isAvailableTab: false,
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildJobList(List<Booking> list, {required bool isAvailableTab}) {
    if (list.isEmpty) {
      return Center(
        child: Text(
          isAvailableTab ? 'Không có đơn đặt lịch mới nào.' : 'Chưa có đơn hàng nào trong mục này.',
          style: TextStyle(color: Colors.grey.shade600),
        ),
      );
    }

    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: list.length,
      separatorBuilder: (_, __) => const SizedBox(height: 12),
      itemBuilder: (context, i) => _RealJobCard(booking: list[i], isAvailableJob: isAvailableTab),
    );
  }
}

class _RealJobCard extends ConsumerWidget {
  final Booking booking;
  final bool isAvailableJob;
  const _RealJobCard({required this.booking, required this.isAvailableJob});

  // ==========================================
  // HÀM MỞ GOOGLE MAPS (PHIÊN BẢN CHUẨN)
  // ==========================================
  Future<void> _openGoogleMaps(BuildContext context, double? lat, double? lng) async {
    if (lat == null || lng == null) return;

    // 1. Link gọi thẳng App Bản đồ
    final Uri geoUrl = Uri.parse('geo:$lat,$lng?q=$lat,$lng');

    // 2. Link web xịn của Google Maps (phòng hờ)
    final Uri webUrl = Uri.parse('https://www.google.com/maps/search/?api=1&query=$lat,$lng');

    try {
      if (await canLaunchUrl(geoUrl)) {
        await launchUrl(geoUrl, mode: LaunchMode.externalApplication);
      } else if (await canLaunchUrl(webUrl)) {
        await launchUrl(webUrl, mode: LaunchMode.externalApplication);
      } else {
        if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Không thể mở bản đồ. Vui lòng kiểm tra lại điện thoại.')),
          );
        }
      }
    } catch (e) {
      debugPrint('Lỗi mở Google Maps: $e');
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);

    return Card(
      elevation: 0,
      color: isAvailableJob ? kPrimaryContainer : theme.colorScheme.surfaceContainerHighest,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Expanded(
                  child: Text(
                      booking.serviceName ?? 'Dịch vụ',
                      style: theme.textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w800,
                        color: isAvailableJob ? kOnPrimaryContainer : theme.colorScheme.onSurface,
                      )
                  ),
                ),
                Text('\$${booking.price.toStringAsFixed(2)}',
                    style: theme.textTheme.titleMedium?.copyWith(
                        fontWeight: FontWeight.w800, color: kPrimary
                    )
                ),
              ],
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Icon(Icons.calendar_today_rounded, size: 16, color: theme.colorScheme.onSurfaceVariant),
                const SizedBox(width: 6),
                // Format ngày tuỳ theo format bạn cấu hình
                Text(booking.date, style: TextStyle(color: theme.colorScheme.onSurfaceVariant)),
                const SizedBox(width: 16),
                Icon(Icons.access_time_rounded, size: 16, color: theme.colorScheme.onSurfaceVariant),
                const SizedBox(width: 6),
                // Format giờ tuỳ theo format bạn cấu hình
                Text(booking.time, style: TextStyle(color: theme.colorScheme.onSurfaceVariant)),
              ],
            ),

            // KHU VỰC HIỂN THỊ ĐỊA CHỈ & NÚT MAPS
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: Colors.white.withValues(alpha: 0.5),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                children: [
                  const Icon(Icons.location_on_rounded, color: Colors.redAccent, size: 20),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      booking.addressText ?? 'Chưa xác định địa chỉ',
                      style: const TextStyle(fontWeight: FontWeight.w500, fontSize: 13),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis,
                    ),
                  ),
                  if (booking.latitude != null && booking.longitude != null)
                    IconButton(
                      icon: const Icon(Icons.map_rounded, color: kPrimary),
                      tooltip: 'Chỉ đường',
                      onPressed: () => _openGoogleMaps(context, booking.latitude, booking.longitude),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 16),

            // Xử lý hiển thị nút bấm tùy theo Tab
            if (isAvailableJob)
              FilledButton(
                onPressed: () async {
                  try {
                    await ref.read(bookingRepositoryProvider).acceptBooking(booking.id);
                    ref.invalidate(availableBookingsProvider);
                    ref.invalidate(workerBookingsProvider);

                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Nhận đơn thành công!')));
                    }
                  } catch (e) {
                    if (context.mounted) {
                      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi: $e'), backgroundColor: Colors.red));
                    }
                  }
                },
                style: FilledButton.styleFrom(minimumSize: const Size.fromHeight(44)),
                child: const Text('Accept Job'),
              )
            else
              Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(vertical: 10),
                decoration: BoxDecoration(
                    color: booking.status == 'Completed' ? Colors.green.withValues(alpha: 0.1) : kPrimary.withValues(alpha: 0.1),
                    borderRadius: BorderRadius.circular(8)
                ),
                child: Text(
                  booking.status.toUpperCase(),
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    color: booking.status == 'Completed' ? Colors.green : kPrimary,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              )
          ],
        ),
      ),
    );
  }
}