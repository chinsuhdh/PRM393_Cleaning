import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/network/dio_client.dart';
import '../models/service_category.dart';

class ServiceCatalogRepository {
  Future<List<ServiceCategory>> getCategories() async {
    try {
      final response =
          await DioClient.instance.get('/ServiceCatalog/categories');
      final raw = response.data;
      if (raw is List) {
        return raw
            .whereType<Map<String, dynamic>>()
            .map(ServiceCategory.fromJson)
            .toList();
      }
      return [];
    } catch (e) {
      return [];
    }
  }
}

final serviceCatalogRepositoryProvider =
    Provider<ServiceCatalogRepository>((ref) {
  return ServiceCatalogRepository();
});

/// FutureProvider that fetches categories from GET /api/ServiceCatalog/categories
final categoriesProvider = FutureProvider<List<ServiceCategory>>((ref) async {
  final repo = ref.read(serviceCatalogRepositoryProvider);
  return repo.getCategories();
});
