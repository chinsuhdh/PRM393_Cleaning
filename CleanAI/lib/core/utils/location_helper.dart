import 'package:geolocator/geolocator.dart';
import 'package:geocoding/geocoding.dart';

class LocationHelper {
  // Hàm này được gọi khi user bấm nút "Lấy vị trí hiện tại"
  static Future<Map<String, dynamic>?> getCurrentAddress() async {
    bool serviceEnabled;
    LocationPermission permission;

    // 1. Kiểm tra dịch vụ định vị có bật không
    serviceEnabled = await Geolocator.isLocationServiceEnabled();
    if (!serviceEnabled) {
      return Future.error('Dịch vụ định vị đang tắt. Vui lòng bật GPS.');
    }

    // 2. Xin quyền truy cập vị trí
    permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
      if (permission == LocationPermission.denied) {
        return Future.error('Quyền truy cập vị trí bị từ chối.');
      }
    }

    if (permission == LocationPermission.deniedForever) {
      return Future.error('Quyền vị trí bị từ chối vĩnh viễn, không thể yêu cầu lại.');
    }

    // 3. Lấy tọa độ GPS hiện tại
    Position position = await Geolocator.getCurrentPosition(
        desiredAccuracy: LocationAccuracy.high);

    // 4. Dịch tọa độ (Reverse Geocoding) sang chuỗi địa chỉ
    try {
      List<Placemark> placemarks = await placemarkFromCoordinates(
        position.latitude,
        position.longitude,
      );

      if (placemarks.isNotEmpty) {
        Placemark place = placemarks[0];
        // Ghép các thành phần địa chỉ (Số nhà, đường, phường, quận, thành phố)
        String addressText = '${place.street}, ${place.subAdministrativeArea}, ${place.administrativeArea}';

        return {
          'latitude': position.latitude,
          'longitude': position.longitude,
          'addressText': addressText,
        };
      }
    } catch (e) {
      return Future.error('Không thể dịch tọa độ sang địa chỉ.');
    }

    return null;
  }
}