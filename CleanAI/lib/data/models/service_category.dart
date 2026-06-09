class ServiceCategory {
  final String id;
  final String name;
  final String? iconName;

  const ServiceCategory({
    required this.id,
    required this.name,
    this.iconName,
  });

  ServiceCategory copyWith({String? id, String? name, String? iconName}) {
    return ServiceCategory(
      id: id ?? this.id,
      name: name ?? this.name,
      iconName: iconName ?? this.iconName,
    );
  }

  factory ServiceCategory.fromJson(Map<String, dynamic> json) {
    return ServiceCategory(
      id: (json['id'] ?? '').toString(),
      name: (json['name'] ?? 'Unknown') as String,
      iconName: json['iconName'] as String?,
    );
  }

  Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'iconName': iconName,
      };
}
