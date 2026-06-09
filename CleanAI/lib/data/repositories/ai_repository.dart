import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/network/dio_client.dart';

class AiRepository {
  Future<String> chatWithBot(String sessionId, String message) async {
    try {
      final response = await DioClient.instance.post(
        '/Ai/chat',
        data: {
          "sessionId": sessionId,
          "message": message,
        },
      );
      // Backend trả về: { "reply": "...", "latencyMs": ... }
      return response.data['reply']?.toString() ?? "AI không có phản hồi.";
    } on DioException catch (e) {
      throw Exception(e.response?.data['error'] ?? 'Lỗi kết nối đến AI Server.');
    }
  }
}

final aiRepositoryProvider = Provider<AiRepository>((ref) => AiRepository());