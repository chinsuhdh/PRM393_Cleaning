import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../data/models/booking.dart';
import '../../data/services/mock_data_service.dart';

abstract class BookingRepository {
  Future<List<Booking>> getBookings();
  Future<Booking> createBooking(Booking booking);
  Future<void> cancelBooking(String bookingId);
}

class MockBookingRepository implements BookingRepository {
  @override
  Future<List<Booking>> getBookings() async {
    await Future.delayed(const Duration(milliseconds: 500));
    return MockDataService.bookings;
  }

  @override
  Future<Booking> createBooking(Booking booking) async {
    await Future.delayed(const Duration(milliseconds: 500));
    return booking;
  }

  @override
  Future<void> cancelBooking(String bookingId) async {
    await Future.delayed(const Duration(milliseconds: 300));
  }
}

final bookingRepositoryProvider = Provider<BookingRepository>((ref) {
  return MockBookingRepository();
});

final bookingsProvider = FutureProvider<List<Booking>>((ref) async {
  final repo = ref.read(bookingRepositoryProvider);
  return repo.getBookings();
});
