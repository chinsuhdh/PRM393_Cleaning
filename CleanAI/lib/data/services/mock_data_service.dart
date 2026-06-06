import '../models/service_category.dart';
import '../models/worker.dart';
import '../models/booking.dart';
import '../models/notification_item.dart';

class MockDataService {
  MockDataService._();

  static const List<ServiceCategory> categories = [
    ServiceCategory(id: 1, name: 'House Cleaning', iconName: 'home'),
    ServiceCategory(id: 2, name: 'Deep Cleaning', iconName: 'cleaning_services'),
    ServiceCategory(id: 3, name: 'Sofa Cleaning', iconName: 'chair'),
    ServiceCategory(id: 4, name: 'Carpet Cleaning', iconName: 'layers'),
    ServiceCategory(id: 5, name: 'Office Cleaning', iconName: 'business'),
    ServiceCategory(id: 6, name: 'AC Cleaning', iconName: 'ac_unit'),
  ];

  static const List<Worker> workers = [
    Worker(
      id: 'w1',
      name: 'Sarah Connor',
      rating: 4.9,
      distance: '1.2 km',
      experience: '3 years',
      matchPercentage: 98,
      reviews: 420,
    ),
    Worker(
      id: 'w2',
      name: 'John Smith',
      rating: 4.6,
      distance: '2.5 km',
      experience: '1 year',
      matchPercentage: 85,
      reviews: 112,
    ),
    Worker(
      id: 'w3',
      name: 'Maria Garcia',
      rating: 4.8,
      distance: '3.0 km',
      experience: '5 years',
      matchPercentage: 95,
      reviews: 850,
    ),
  ];

  static List<Worker> get recommendedWorkers {
    final sorted = [...workers]
      ..sort((a, b) => b.matchPercentage.compareTo(a.matchPercentage));
    return sorted.take(2).toList();
  }

  static List<Booking> get bookings => [
        Booking(
          id: 'b1',
          serviceName: 'Deep Cleaning',
          date: 'Oct 15, 2026',
          time: '09:00 AM',
          price: 80.0,
          status: 'Upcoming',
          worker: workers[0],
        ),
        Booking(
          id: 'b2',
          serviceName: 'House Cleaning',
          date: 'Oct 10, 2026',
          time: '02:00 PM',
          price: 50.0,
          status: 'Completed',
          worker: workers[1],
        ),
        Booking(
          id: 'b3',
          serviceName: 'Sofa Cleaning',
          date: 'Sep 28, 2026',
          time: '10:00 AM',
          price: 35.0,
          status: 'Cancelled',
        ),
      ];

  static List<NotificationItem> get notifications => [
        const NotificationItem(
          id: 'n1',
          title: 'Booking Confirmed',
          message: 'Your House Cleaning service is booked for Oct 15.',
          isUnread: true,
        ),
        const NotificationItem(
          id: 'n2',
          title: 'Special Offer',
          message: 'Get 20% off on your next deep cleaning! Valid until Friday.',
          isUnread: false,
        ),
        const NotificationItem(
          id: 'n3',
          title: 'Worker Matched',
          message: 'Sarah Connor has been assigned to your service.',
          isUnread: false,
        ),
      ];
}
