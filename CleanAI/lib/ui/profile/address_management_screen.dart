import 'package:flutter/material.dart';
import '../../core/theme/app_colors.dart';
import '../../core/utils/location_helper.dart';
import '../../core/network/dio_client.dart';

class AddressManagementScreen extends StatefulWidget {
  const AddressManagementScreen({super.key});

  @override
  State<AddressManagementScreen> createState() => _AddressManagementScreenState();
}

class _AddressManagementScreenState extends State<AddressManagementScreen> {
  List<Map<String, dynamic>> _addresses = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _fetchAddresses();
  }

  Future<void> _fetchAddresses() async {
    setState(() => _isLoading = true);
    try {
      final response = await DioClient.instance.get('/UserAddresses');
      setState(() {
        _addresses = List<Map<String, dynamic>>.from(response.data);
        _addresses.sort((a, b) => (b['isDefault'] == true ? 1 : 0).compareTo(a['isDefault'] == true ? 1 : 0));
      });
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi: $e')));
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  Future<void> _deleteAddress(String id, int index) async {
    try {
      await DioClient.instance.delete('/UserAddresses/$id');
      setState(() => _addresses.removeAt(index));
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Đã xóa!')));
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Lỗi xóa!')));
      }
    }
  }

  Future<void> _setDefaultAddress(String id) async {
    try {
      await DioClient.instance.patch('/UserAddresses/$id/set-default');
      _fetchAddresses();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(const SnackBar(content: Text('Lỗi!')));
      }
    }
  }

  void _showAddressBottomSheet({Map<String, dynamic>? existingAddress, int? index}) {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(borderRadius: BorderRadius.vertical(top: Radius.circular(24))),
      builder: (context) => AddressBottomSheet(
        initialData: existingAddress,
        onAddressSaved: (savedAddress) => _fetchAddresses(),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Scaffold(
      appBar: AppBar(title: const Text('Saved Addresses', style: TextStyle(fontWeight: FontWeight.w800))),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _addresses.isEmpty
          ? const Center(child: Text('Bạn chưa có địa chỉ nào.'))
          : ListView.separated(
        padding: const EdgeInsets.all(16),
        itemCount: _addresses.length,
        separatorBuilder: (_, __) => const SizedBox(height: 12),
        itemBuilder: (context, i) {
          final addr = _addresses[i];
          return Card(
            elevation: 0,
            color: theme.colorScheme.surfaceContainerHighest,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Row(
                children: [
                  Container(
                    width: 44, height: 44,
                    decoration: BoxDecoration(color: kPrimaryContainer, borderRadius: BorderRadius.circular(12)),
                    child: Icon(addr['label']?.toLowerCase() == 'home' ? Icons.home_rounded : Icons.business_rounded, color: kPrimary),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            Expanded(
                              child: Text(
                                addr['label'] ?? 'Địa chỉ',
                                style: const TextStyle(fontWeight: FontWeight.w700),
                                overflow: TextOverflow.ellipsis,
                              ),
                            ),
                            if (addr['isDefault'] == true) ...[
                              const SizedBox(width: 8),
                              Container(
                                padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                                decoration: BoxDecoration(color: kPrimary.withValues(alpha: 0.1), borderRadius: BorderRadius.circular(4)),
                                child: const Text('Mặc định', style: TextStyle(fontSize: 10, color: kPrimary, fontWeight: FontWeight.bold)),
                              )
                            ]
                          ],
                        ),
                        Text(addr['addressText'] ?? '', style: theme.textTheme.bodySmall),
                      ],
                    ),
                  ),
                  PopupMenuButton<String>(
                    onSelected: (value) {
                      if (value == 'delete') {
                        _deleteAddress(addr['id'], i);
                      } else if (value == 'set_default') {
                        _setDefaultAddress(addr['id']);
                      } else if (value == 'edit') {
                        _showAddressBottomSheet(existingAddress: addr, index: i);
                      }
                    },
                    itemBuilder: (context) => [
                      if (addr['isDefault'] != true) const PopupMenuItem(value: 'set_default', child: Text('Đặt mặc định')),
                      const PopupMenuItem(value: 'edit', child: Text('Chỉnh sửa')),
                      const PopupMenuItem(value: 'delete', child: Text('Xóa', style: TextStyle(color: Colors.red))),
                    ],
                  ),
                ],
              ),
            ),
          );
        },
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => _showAddressBottomSheet(),
        icon: const Icon(Icons.add_rounded),
        label: const Text('Add Address'),
        backgroundColor: kPrimary,
      ),
    );
  }
}

class AddressBottomSheet extends StatefulWidget {
  final Map<String, dynamic>? initialData;
  final Function(Map<String, dynamic>) onAddressSaved;
  const AddressBottomSheet({super.key, this.initialData, required this.onAddressSaved});
  @override
  State<AddressBottomSheet> createState() => _AddressBottomSheetState();
}

class _AddressBottomSheetState extends State<AddressBottomSheet> {
  final _labelController = TextEditingController();
  final _addressController = TextEditingController();
  double? _lat, _lng;
  bool _isFetchingGPS = false, _isSaving = false;

  @override
  void initState() {
    super.initState();
    _labelController.text = widget.initialData?['label'] ?? '';
    _addressController.text = widget.initialData?['addressText'] ?? '';
    _lat = widget.initialData?['latitude'];
    _lng = widget.initialData?['longitude'];
  }

  Future<void> _handleAutoLocation() async {
    setState(() => _isFetchingGPS = true);
    try {
      final loc = await LocationHelper.getCurrentAddress();
      if (loc != null) {
        setState(() {
          _addressController.text = loc['addressText'];
          _lat = loc['latitude'];
          _lng = loc['longitude'];
        });
      }
    } finally {
      if (mounted) {
        setState(() => _isFetchingGPS = false);
      }
    }
  }

  Future<void> _handleSave() async {
    // Kiểm tra tính hợp lệ trước khi gửi API
    if (_addressController.text.trim().isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng nhập địa chỉ trước khi lưu!')),
      );
      return;
    }

    setState(() => _isSaving = true);
    final payload = {
      'label': _labelController.text.trim(),
      'addressText': _addressController.text.trim(),
      'latitude': _lat,
      'longitude': _lng,
      'isDefault': widget.initialData?['isDefault'] ?? false
    };

    try {
      if (widget.initialData != null) {
        await DioClient.instance.put('/UserAddresses/${widget.initialData!['id']}', data: payload);
      } else {
        await DioClient.instance.post('/UserAddresses', data: payload);
      }
      widget.onAddressSaved({});
      if (mounted) {
        Navigator.pop(context);
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text('Lỗi: $e')));
      }
    } finally {
      if (mounted) {
        setState(() => _isSaving = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(left: 24, right: 24, top: 24, bottom: MediaQuery.of(context).viewInsets.bottom + 24),
      child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            TextField(controller: _labelController, decoration: const InputDecoration(labelText: 'Label')),
            TextField(
                controller: _addressController,
                decoration: InputDecoration(
                    labelText: 'Address',
                    suffixIcon: _isFetchingGPS
                        ? const Padding(
                      padding: EdgeInsets.all(12.0),
                      child: SizedBox(
                          width: 20,
                          height: 20,
                          child: CircularProgressIndicator(strokeWidth: 2)
                      ),
                    )
                        : IconButton(
                        icon: const Icon(Icons.my_location),
                        onPressed: _handleAutoLocation
                    )
                )
            ),
            const SizedBox(height: 20),
            FilledButton(
                onPressed: _isSaving ? null : _handleSave,
                child: _isSaving
                    ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                    : const Text('Save')
            ),
          ]
      ),
    );
  }
}