import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/models/worker.dart';
import '../../data/services/mock_data_service.dart';

abstract class WorkerRepository {
  Future<List<Worker>> getWorkers();
  Future<List<Worker>> getRecommendedWorkers();
}

class MockWorkerRepository implements WorkerRepository {
  @override
  Future<List<Worker>> getWorkers() async {
    await Future.delayed(const Duration(milliseconds: 400));
    return MockDataService.workers;
  }

  @override
  Future<List<Worker>> getRecommendedWorkers() async {
    await Future.delayed(const Duration(milliseconds: 400));
    return MockDataService.recommendedWorkers;
  }
}

final workerRepositoryProvider = Provider<WorkerRepository>((ref) {
  return MockWorkerRepository();
});

final recommendedWorkersProvider = FutureProvider<List<Worker>>((ref) async {
  final repo = ref.read(workerRepositoryProvider);
  return repo.getRecommendedWorkers();
});
