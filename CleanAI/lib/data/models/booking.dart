import 'worker.dart';

class Booking {
  final String id;
  final String serviceName;
  final String date;
  final String time;
  final double price;
  final String status;
  final Worker? worker;

  const Booking({
    required this.id,
    required this.serviceName,
    required this.date,
    required this.time,
    required this.price,
    required this.status,
    this.worker,
  });

  Booking copyWith({
    String? id,
    String? serviceName,
    String? date,
    String? time,
    double? price,
    String? status,
    Worker? worker,
  }) {
    return Booking(
      id: id ?? this.id,
      serviceName: serviceName ?? this.serviceName,
      date: date ?? this.date,
      time: time ?? this.time,
      price: price ?? this.price,
      status: status ?? this.status,
      worker: worker ?? this.worker,
    );
  }

  factory Booking.fromJson(Map<String, dynamic> json) {
    return Booking(
      id: json['id'] as String,
      serviceName: json['serviceName'] as String,
      date: json['date'] as String,
      time: json['time'] as String,
      price: (json['price'] as num).toDouble(),
      status: json['status'] as String,
      worker: json['worker'] != null
          ? Worker.fromJson(json['worker'] as Map<String, dynamic>)
          : null,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'serviceName': serviceName,
        'date': date,
        'time': time,
        'price': price,
        'status': status,
        'worker': worker?.toJson(),
      };
}
