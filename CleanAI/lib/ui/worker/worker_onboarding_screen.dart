import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../core/theme/app_colors.dart';
import '../../data/repositories/worker_repository.dart';

class WorkerOnboardingScreen extends ConsumerStatefulWidget {
  const WorkerOnboardingScreen({super.key});

  @override
  ConsumerState<WorkerOnboardingScreen> createState() => _WorkerOnboardingScreenState();
}

class _WorkerOnboardingScreenState extends ConsumerState<WorkerOnboardingScreen> {
  final _formKey = GlobalKey<FormState>();
  final _cccdController = TextEditingController();

  bool _isLoading = false;

  // Giả lập danh sách dịch vụ từ Backend (Trong thực tế bạn sẽ fetch từ API /Services)
  final List<Map<String, dynamic>> _availableServices = [
    {'id': 'srv-001', 'name': 'Deep House Cleaning'},
    {'id': 'srv-002', 'name': 'AC Maintenance & Repair'},
    {'id': 'srv-003', 'name': 'Plumbing Services'},
    {'id': 'srv-004', 'name': 'Electrical Repair'},
  ];

  // Map để lưu trữ các kỹ năng được chọn: Key là service_id, Value là số tháng kinh nghiệm
  final Map<String, int> _selectedSkills = {};

  void _toggleSkill(String serviceId, bool? isSelected) {
    setState(() {
      if (isSelected == true) {
        _selectedSkills[serviceId] = 0; // Mặc định 0 tháng kinh nghiệm khi mới tick
      } else {
        _selectedSkills.remove(serviceId);
      }
    });
  }

  void _updateExperience(String serviceId, String monthsStr) {
    final months = int.tryParse(monthsStr) ?? 0;
    _selectedSkills[serviceId] = months;
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;

    if (_selectedSkills.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Vui lòng chọn ít nhất 1 kỹ năng chuyên môn!')),
      );
      return;
    }

    setState(() => _isLoading = true);

    try {
      // Chuyển đổi Map thành List<Map> theo đúng format Backend yêu cầu
      final skillsPayload = _selectedSkills.entries.map((e) => {
        'serviceId': e.key,
        'experienceMonths': e.value,
      }).toList();

      // Gọi API
      await ref.read(workerRepositoryProvider).registerAsWorker(
        identityCardNumber: _cccdController.text.trim(),
        skills: skillsPayload,
      );

      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Đăng ký hồ sơ thợ thành công! 🎉'), backgroundColor: kSecondary),
        );
        // Đẩy thợ vào Dashboard
        context.go('/worker');
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Lỗi: $e'), backgroundColor: Colors.red),
        );
      }
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  void dispose() {
    _cccdController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('Worker Registration', style: TextStyle(fontWeight: FontWeight.w800)),
        leading: IconButton(
          icon: const Icon(Icons.arrow_back_rounded),
          onPressed: () => context.pop(),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(24),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Step 1: Identity Verification', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
              const SizedBox(height: 8),
              Text('Theo quy định, bạn cần cung cấp số CCCD chính xác để bảo vệ an toàn cho khách hàng.',
                  style: TextStyle(color: theme.colorScheme.onSurfaceVariant, fontSize: 13)),
              const SizedBox(height: 16),
              TextFormField(
                controller: _cccdController,
                keyboardType: TextInputType.number,
                maxLength: 12,
                decoration: InputDecoration(
                  labelText: 'Identity Card Number (CCCD)',
                  prefixIcon: const Icon(Icons.badge_outlined),
                  border: OutlineInputBorder(borderRadius: BorderRadius.circular(16)),
                ),
                validator: (value) {
                  if (value == null || value.length != 12) return 'CCCD phải bao gồm đúng 12 số';
                  return null;
                },
              ),
              const SizedBox(height: 16),
              Row(
                children: [
                  Expanded(child: _ImageUploadBox(label: 'CCCD Mặt trước')),
                  const SizedBox(width: 16),
                  Expanded(child: _ImageUploadBox(label: 'CCCD Mặt sau')),
                ],
              ),

              const SizedBox(height: 32),
              const Divider(),
              const SizedBox(height: 24),

              Text('Step 2: Professional Skills', style: theme.textTheme.titleMedium?.copyWith(fontWeight: FontWeight.w700)),
              const SizedBox(height: 8),
              Text('Chọn các dịch vụ bạn có thể thực hiện và số tháng kinh nghiệm tương ứng.',
                  style: TextStyle(color: theme.colorScheme.onSurfaceVariant, fontSize: 13)),
              const SizedBox(height: 16),

              // Render danh sách Skills
              ..._availableServices.map((service) {
                final isSelected = _selectedSkills.containsKey(service['id']);

                return Card(
                  margin: const EdgeInsets.only(bottom: 12),
                  elevation: 0,
                  color: isSelected ? kPrimaryContainer : theme.colorScheme.surfaceContainerHighest,
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(16),
                    side: BorderSide(color: isSelected ? kPrimary : Colors.transparent, width: 1.5),
                  ),
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                    child: Column(
                      children: [
                        CheckboxListTile(
                          title: Text(service['name'], style: TextStyle(fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500)),
                          value: isSelected,
                          activeColor: kPrimary,
                          onChanged: (val) => _toggleSkill(service['id'], val),
                          controlAffinity: ListTileControlAffinity.leading,
                          contentPadding: EdgeInsets.zero,
                        ),
                        // Nếu được chọn, hiện thêm ô nhập số tháng kinh nghiệm
                        if (isSelected)
                          Padding(
                            padding: const EdgeInsets.only(left: 48, right: 16, bottom: 16),
                            child: TextFormField(
                              keyboardType: TextInputType.number,
                              decoration: InputDecoration(
                                labelText: 'Experience (Months)',
                                isDense: true,
                                border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
                                filled: true,
                                fillColor: theme.colorScheme.surface,
                              ),
                              onChanged: (val) => _updateExperience(service['id'], val),
                              validator: (val) {
                                if (isSelected && (val == null || val.isEmpty)) return 'Bắt buộc nhập';
                                return null;
                              },
                            ),
                          ),
                      ],
                    ),
                  ),
                );
              }),
            ],
          ),
        ),
      ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(24),
          child: FilledButton(
            onPressed: _isLoading ? null : _submit,
            style: FilledButton.styleFrom(
              minimumSize: const Size.fromHeight(56),
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
            ),
            child: _isLoading
                ? const SizedBox(width: 24, height: 24, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                : const Text('Submit Application', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w700)),
          ),
        ),
      ),
    );
  }
}

// Widget giả lập hộp upload ảnh
class _ImageUploadBox extends StatelessWidget {
  final String label;
  const _ImageUploadBox({required this.label});

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 100,
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: Colors.grey.withValues(alpha: 0.3), style: BorderStyle.solid),
      ),
      child: Column(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          const Icon(Icons.add_a_photo_outlined, color: Colors.grey),
          const SizedBox(height: 8),
          Text(label, style: const TextStyle(fontSize: 12, color: Colors.grey, fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }
}