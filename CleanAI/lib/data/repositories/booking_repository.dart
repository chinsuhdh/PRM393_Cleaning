import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../core/network/dio_client.dart';
import '../models/booking.dart';

abstract class BookingRepository {
  Future<List<Booking>> getClientBookings();
  Future<List<Booking>> getWorkerBookings();
  Future<List<Booking>> getAvailableBookings();
  Future<Booking> createBooking(Map<String, dynamic> data);
  Future<void> cancelBooking(String bookingId);
  Future<void> acceptBooking(String bookingId);
  Future<void> updateBookingStatus(String bookingId, int newStatus);
}

class ApiBookingRepository implements BookingRepository {
  @override
  Future<List<Booking>> getClientBookings() async {
    try {
      final response = await DioClient.instance.get('/Bookings/client');
      final raw = response.data;
      if (raw is List) {
        return raw
            .whereType<Map<String, dynamic>>()
            .map((json) => Booking.fromJson(json))
            .toList();
      }
      return [];
    } catch (e) {
      return [];
    }
  }

  @override
  Future<List<Booking>> getWorkerBookings() async {
    try {
      final response = await DioClient.instance.get('/Bookings/worker');
      final raw = response.data;
      if (raw is List) {
        return raw
            .whereType<Map<String, dynamic>>()
            .map((json) => Booking.fromJson(json))
            .toList();
      }
      return [];
    } catch (e) {
      return [];
    }
  }

  @override
  Future<List<Booking>> getAvailableBookings() async {
    try {
      final response = await DioClient.instance.get('/Bookings/available');
      final raw = response.data;
      if (raw is List) {
        return raw
            .whereType<Map<String, dynamic>>()
            .map((json) => Booking.fromJson(json))
            .toList();
      }
      return [];
    } catch (e) {
      return [];
    }
  }

  @override
  Future<Booking> createBooking(Map<String, dynamic> data) async {
    try {
      final response = await DioClient.instance.post('/Bookings', data: data);
      return Booking.fromJson(response.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw Exception(e.response?.data['message'] ?? 'Lỗi khi tạo Booking');
    }
  }

  @override
  Future<void> cancelBooking(String bookingId) async {
    try {
      await updateBookingStatus(bookingId, 4); // 4 = Cancelled
    } on DioException catch (e) {
      throw Exception(e.response?.data['message'] ?? 'Lỗi khi hủy Booking');
    }
  }

  @override
  Future<void> acceptBooking(String bookingId) async {
    try {
      await DioClient.instance.patch('/Bookings/$bookingId/accept');
    } on DioException catch (e) {
      throw Exception(e.response?.data['message'] ?? 'Lỗi khi nhận đơn');
    }
  }

  @override
  Future<void> updateBookingStatus(String bookingId, int newStatus) async {
    try {
      await DioClient.instance.patch(
        '/Bookings/$bookingId/status',
        data: {'newStatus': newStatus},
      );
    } on DioException catch (e) {
      throw Exception(e.response?.data['message'] ?? 'Lỗi khi cập nhật trạng thái');
    }
  }
}

final bookingRepositoryProvider = Provider<BookingRepository>((ref) {
  return ApiBookingRepository();
});

// Cho Client
final bookingsProvider = FutureProvider.autoDispose<List<Booking>>((ref) async {
  return ref.read(bookingRepositoryProvider).getClientBookings();
});

// Cho Worker
final workerBookingsProvider = FutureProvider.autoDispose<List<Booking>>((ref) async {
  return ref.read(bookingRepositoryProvider).getWorkerBookings();
});

final availableBookingsProvider = FutureProvider.autoDispose<List<Booking>>((ref) async {
  return ref.read(bookingRepositoryProvider).getAvailableBookings();
});