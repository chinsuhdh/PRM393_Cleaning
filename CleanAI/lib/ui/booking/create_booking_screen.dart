import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';
import '../../core/theme/app_colors.dart';
import '../../core/network/dio_client.dart';
import '../../data/repositories/booking_repository.dart';

// Provider Lấy Địa chỉ
final userAddressesProvider = FutureProvider.autoDispose<List<Map<String, dynamic>>>((ref) async {
  final response = await DioClient.instance.get('/UserAddresses');
  final list = List<Map<String, dynamic>>.from(response.data);
  list.sort((a, b) => (b['isDefault'] == true ? 1 : 0).compareTo(a['isDefault'] == true ? 1 : 0));
  return list;
});

// Provider Lấy Chi tiết Dịch vụ thật để làm Hóa đơn
final bookingServiceDetailProvider = FutureProvider.autoDispose.family<Map<String, dynamic>, String>((ref, id) async {
  try {
    final response = await DioClient.instance.get('/ServiceCatalog/services/$id');
    return response.data;
  } catch (e) {
    // Nếu BE không có Get By Id, dùng hàm lọc từ danh sách
    final res = await DioClient.instance.get('/ServiceCatalog/services');
    final list = List<Map<String, dynamic>>.from(res.data);
    return list.firstWhere((s) => s['id'] == id);
  }
});

class CreateBookingScreen extends ConsumerStatefulWidget {
  final String serviceId;
  const CreateBookingScreen({super.key, required this.serviceId});

  @override
  ConsumerState<CreateBookingScreen> createState() => _CreateBookingScreenState();
}

class _CreateBookingScreenState extends ConsumerState<CreateBookingScreen> {
  int _currentStep = 0;
  bool _isBooking = false;

  Map<String, dynamic>? _selectedAddress;
  DateTime _selectedDate = DateTime.now().add(const Duration(days: 1));
  TimeOfDay _selectedTime = const TimeOfDay(hour: 9, minute: 0);
  final TextEditingController _notesController = TextEditingController();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final addressesAsync = ref.watch(userAddressesProvider);
    final serviceAsync = ref.watch(bookingServiceDetailProvider(widget.serviceId));

    if (_selectedAddress == null && addressesAsync.hasValue && addressesAsync.value!.isNotEmpty) {
      WidgetsBinding.instance.addPostFrameCallback((_) => setState(() => _selectedAddress = addressesAsync.value!.first));
    }

    return Scaffold(
      appBar: AppBar(title: const Text('Book Service', style: TextStyle(fontWeight: FontWeight.w800))),
      body: Column(
        children: [
          // Header các bước (Giữ nguyên giao diện của bạn)
          Padding(
            padding: const EdgeInsets.fromLTRB(24, 16, 24, 24),
            child: Row(
              children: List.generate(3, (i) {
                final isActive = i <= _currentStep;
                final isCompleted = i < _currentStep;
                final labels = ['Address', 'Date & Time', 'Summary'];
                return Expanded(
                  child: Row(
                    children: [
                      Column(
                        children: [
                          AnimatedContainer(
                            duration: const Duration(milliseconds: 300),
                            width: 32, height: 32,
                            decoration: BoxDecoration(color: isActive ? kPrimary : theme.colorScheme.surfaceContainerHighest, shape: BoxShape.circle),
                            child: Center(
                              child: isCompleted ? const Icon(Icons.check_rounded, color: Colors.white, size: 18)
                                  : Text('${i + 1}', style: TextStyle(color: isActive ? Colors.white : theme.colorScheme.onSurfaceVariant, fontWeight: FontWeight.w700)),
                            ),
                          ),
                          const SizedBox(height: 4),
                          Text(labels[i], style: theme.textTheme.labelSmall?.copyWith(color: isActive ? kPrimary : theme.colorScheme.onSurfaceVariant, fontWeight: isActive ? FontWeight.w600 : FontWeight.normal)),
                        ],
                      ),
                      if (i < 2) Expanded(child: AnimatedContainer(duration: const Duration(milliseconds: 300), height: 2, margin: const EdgeInsets.only(bottom: 20), color: i < _currentStep ? kPrimary : theme.colorScheme.surfaceContainerHighest)),
                    ],
                  ),
                );
              }),
            ),
          ),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 24),
              child: IndexedStack(
                index: _currentStep,
                children: [
                  _AddressStep(selectedAddress: _selectedAddress, onAddressSelected: (addr) => setState(() => _selectedAddress = addr)),
                  _buildDateTimeStep(context),
                  _buildSummaryStep(serviceAsync),
                ],
              ),
            ),
          ),
        ],
      ),
      bottomNavigationBar: Container(
        padding: const EdgeInsets.all(16),
        child: Row(
          children: [
            if (_currentStep > 0)
              Expanded(
                child: OutlinedButton(
                  onPressed: () => setState(() => _currentStep--),
                  child: const Text('Back'),
                ),
              ),
            if (_currentStep > 0) const SizedBox(width: 16),
            Expanded(
              child: FilledButton(
                onPressed: _isBooking ? null : () async {
                  if (_currentStep == 0 && _selectedAddress == null) {
                    ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Vui lòng chọn địa chỉ!'), backgroundColor: Colors.red));
                    return;
                  }
                  if (_currentStep < 2) {
                    setState(() => _currentStep++);
                  } else {
                    setState(() => _isBooking = true);
                    try {
                      // Tính toán chuẩn DateTime để đẩy lên Backend
                      final scheduledTime = DateTime(
                        _selectedDate.year, _selectedDate.month, _selectedDate.day,
                        _selectedTime.hour, _selectedTime.minute,
                      );

                      // DỮ LIỆU ĐỘNG 100% GỬI LÊN API
                      final payload = {
                        "serviceId": widget.serviceId,
                        "addressId": _selectedAddress?['id'],
                        "scheduledTime": scheduledTime.toUtc().toIso8601String(),
                        "durationHours": 2, // Có thể làm input sau
                        "quantity": 1, // Có thể làm input sau
                        "notes": _notesController.text.isNotEmpty ? _notesController.text : "Không có ghi chú",
                      };

                      final newBooking = await ref.read(bookingRepositoryProvider).createBooking(payload);
                      final realBookingId = newBooking.id;

                      await DioClient.instance.post('/Ai/match-worker/$realBookingId');
                      ref.invalidate(bookingsProvider);
                      if (mounted) context.push('/finding-worker/$realBookingId');
                    } catch (e) {
                      if (mounted) ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi: $e'), backgroundColor: Colors.red));
                    } finally {
                      if (mounted) setState(() => _isBooking = false);
                    }
                  }
                },
                child: _isBooking ? const SizedBox(width: 24, height: 24, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2)) : Text(_currentStep == 2 ? 'Confirm Booking' : 'Next'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  // Bước 2: CHỌN NGÀY VÀ GIỜ THẬT
  Widget _buildDateTimeStep(BuildContext context) {
    return SingleChildScrollView(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('Date & Time', style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700)),
          const SizedBox(height: 16),
          ListTile(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12), side: BorderSide(color: Colors.grey.shade300)),
            leading: const Icon(Icons.calendar_today_rounded, color: kPrimary),
            title: const Text('Date'),
            subtitle: Text(DateFormat('MMM dd, yyyy').format(_selectedDate), style: const TextStyle(fontWeight: FontWeight.bold, color: Colors.black)),
            onTap: () async {
              final picked = await showDatePicker(context: context, initialDate: _selectedDate, firstDate: DateTime.now(), lastDate: DateTime.now().add(const Duration(days: 365)));
              if (picked != null) setState(() => _selectedDate = picked);
            },
          ),
          const SizedBox(height: 16),
          ListTile(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12), side: BorderSide(color: Colors.grey.shade300)),
            leading: const Icon(Icons.access_time_rounded, color: kPrimary),
            title: const Text('Time'),
            subtitle: Text(_selectedTime.format(context), style: const TextStyle(fontWeight: FontWeight.bold, color: Colors.black)),
            onTap: () async {
              final picked = await showTimePicker(context: context, initialTime: _selectedTime);
              if (picked != null) setState(() => _selectedTime = picked);
            },
          ),
          const SizedBox(height: 24),
          const Text('Additional Notes', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
          const SizedBox(height: 8),
          TextField(
            controller: _notesController,
            maxLines: 3,
            decoration: InputDecoration(
              hintText: 'Nhà có nuôi thú cưng, cần mang máy hút bụi...',
              border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
            ),
          )
        ],
      ),
    );
  }

  // Bước 3: HÓA ĐƠN VỚI SỐ TIỀN THẬT
  Widget _buildSummaryStep(AsyncValue<Map<String, dynamic>> serviceAsync) {
    return serviceAsync.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (e, _) => Center(child: Text('Lỗi tải dữ liệu hóa đơn: $e')),
      data: (service) {
        final addressText = _selectedAddress?['addressText'] ?? 'Chưa chọn địa chỉ';
        final price = service['basePrice'] ?? service['price'] ?? 0.0;
        final tax = (price * 0.05); // Giả lập VAT 5%
        final total = price + tax;

        return SingleChildScrollView(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('Booking Summary', style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700)),
              const SizedBox(height: 16),
              Card(
                elevation: 0,
                color: Colors.grey.shade100,
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                child: Padding(
                  padding: const EdgeInsets.all(20),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(service['name'] ?? 'Dịch vụ', style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
                      const SizedBox(height: 12),
                      _SummaryRow(label: 'Date', value: DateFormat('MMM dd, yyyy').format(_selectedDate)),
                      const SizedBox(height: 6),
                      _SummaryRow(label: 'Time', value: _selectedTime.format(context)),
                      const SizedBox(height: 6),
                      _SummaryRow(label: 'Address', value: addressText),
                      const SizedBox(height: 16),
                      const Divider(),
                      const SizedBox(height: 12),
                      _SummaryRow(label: 'Service Fee', value: '$price VND'),
                      const SizedBox(height: 6),
                      _SummaryRow(label: 'Tax (5%)', value: '$tax VND'),
                      const SizedBox(height: 12),
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          const Text('Total', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w700)),
                          Text('$total VND', style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: kPrimary)),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
            ],
          ),
        );
      },
    );
  }
}

// Widget chọn địa chỉ giữ nguyên code của bạn
class _AddressStep extends ConsumerWidget {
  final Map<String, dynamic>? selectedAddress;
  final ValueChanged<Map<String, dynamic>> onAddressSelected;

  const _AddressStep({required this.selectedAddress, required this.onAddressSelected});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final addressesAsync = ref.watch(userAddressesProvider);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text('Select Address', style: TextStyle(fontSize: 20, fontWeight: FontWeight.w700)),
        const SizedBox(height: 16),
        Expanded(
          child: addressesAsync.when(
            data: (addresses) {
              if (addresses.isEmpty) return const Center(child: Text('Bạn chưa có địa chỉ nào.'));
              return ListView.separated(
                itemCount: addresses.length,
                separatorBuilder: (_, __) => const SizedBox(height: 12),
                itemBuilder: (context, index) {
                  final addr = addresses[index];
                  final isSelected = selectedAddress?['id'] == addr['id'];
                  return GestureDetector(
                    onTap: () => onAddressSelected(addr),
                    child: Card(
                      color: isSelected ? kPrimaryContainer : Colors.grey.shade100,
                      elevation: 0,
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16), side: BorderSide(color: isSelected ? kPrimary : Colors.transparent, width: 2)),
                      child: ListTile(
                        leading: Icon(Icons.location_on_rounded, color: isSelected ? kPrimary : Colors.grey),
                        title: Text(addr['label'] ?? 'Địa chỉ', style: TextStyle(fontWeight: FontWeight.w700, color: isSelected ? kOnPrimaryContainer : Colors.black)),
                        subtitle: Text(addr['addressText'] ?? '', maxLines: 2, overflow: TextOverflow.ellipsis),
                        trailing: isSelected ? const Icon(Icons.check_circle_rounded, color: kPrimary) : null,
                      ),
                    ),
                  );
                },
              );
            },
            loading: () => const Center(child: CircularProgressIndicator()),
            error: (e, _) => Center(child: Text('Lỗi: $e')),
          ),
        ),
      ],
    );
  }
}

class _SummaryRow extends StatelessWidget {
  final String label, value;
  const _SummaryRow({required this.label, required this.value});
  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: const TextStyle(color: Colors.grey)),
        const SizedBox(width: 16),
        Expanded(child: Text(value, style: const TextStyle(fontWeight: FontWeight.w600), textAlign: TextAlign.right)),
      ],
    );
  }
}