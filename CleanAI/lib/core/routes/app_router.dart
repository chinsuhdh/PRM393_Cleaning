import 'package:go_router/go_router.dart';
import 'package:flutter/material.dart';

import '../../ui/auth/splash_screen.dart';
import '../../ui/auth/onboarding_screen.dart';
import '../../ui/auth/login_screen.dart';
import '../../ui/auth/register_screen.dart';
import '../../ui/auth/forgot_password_screen.dart'; // [ĐÃ THÊM] Import màn hình Quên mật khẩu
import '../../ui/auth/verify_otp_screen.dart';      // [ĐÃ THÊM] Import màn hình Nhập OTP

import '../../ui/home/client_shell.dart';
import '../../ui/home/home_screen.dart';
import '../../ui/booking/bookings_screen.dart';
import '../../ui/booking/create_booking_screen.dart';
import '../../ui/booking/booking_detail_screen.dart';
import '../../ui/booking/finding_worker_screen.dart';
import '../../ui/chat/chat_screen.dart';
import '../../ui/notification/notifications_screen.dart';
import '../../ui/profile/profile_screen.dart';
import '../../ui/profile/address_management_screen.dart';
import '../../ui/service/service_detail_screen.dart';
import '../../ui/worker/worker_dashboard_screen.dart';
import '../../ui/worker/worker_jobs_screen.dart';
import '../../ui/worker/worker_wallet_screen.dart';
import '../../ui/admin/admin_dashboard_screen.dart';

class AppRoutes {
  static const splash = '/';
  static const onboarding = '/onboarding';
  static const login = '/login';
  static const register = '/register';
  static const verifyOtp = '/verify-otp';
  static const forgotPassword = '/forgot-password';

  static const clientShell = '/home';
  static const home = '/home/dashboard';
  static const bookings = '/home/bookings';
  static const chat = '/home/chat';
  static const notifications = '/home/notifications';
  static const profile = '/home/profile';

  // ĐÃ SỬA: Đổi từ /service/:id sang /category/:id
  static const serviceDetail = '/category/:id';
  static const createBooking = '/booking/create/:serviceId';
  static const bookingDetail = '/booking/:id';
  static const findingWorker = '/finding-worker/:bookingId';
  static const addressManagement = '/address';
  static const editProfile = '/profile/edit';
  static const payment = '/payment/:bookingId';
  static const review = '/review/:bookingId';

  static const workerShell = '/worker';
  static const workerDashboard = '/worker/dashboard';
  static const workerJobs = '/worker/jobs';
  static const workerWallet = '/worker/wallet';

  static const admin = '/admin';
}

final _rootNavigatorKey = GlobalKey<NavigatorState>(debugLabel: 'root');
final _shellNavigatorHomeKey = GlobalKey<NavigatorState>(debugLabel: 'shellHome');
final _shellNavigatorBookingsKey = GlobalKey<NavigatorState>(debugLabel: 'shellBookings');
final _shellNavigatorChatKey = GlobalKey<NavigatorState>(debugLabel: 'shellChat');
final _shellNavigatorNotificationsKey = GlobalKey<NavigatorState>(debugLabel: 'shellNotifications');
final _shellNavigatorProfileKey = GlobalKey<NavigatorState>(debugLabel: 'shellProfile');

final _workerShellHomeKey = GlobalKey<NavigatorState>(debugLabel: 'workerShellHome');
final _workerShellJobsKey = GlobalKey<NavigatorState>(debugLabel: 'workerShellJobs');
final _workerShellWalletKey = GlobalKey<NavigatorState>(debugLabel: 'workerShellWallet');

final GoRouter appRouter = GoRouter(
  navigatorKey: _rootNavigatorKey,
  initialLocation: AppRoutes.splash,
  routes: [
    GoRoute(path: AppRoutes.splash, builder: (context, state) => const SplashScreen()),
    GoRoute(path: AppRoutes.onboarding, builder: (context, state) => const OnboardingScreen()),
    GoRoute(path: AppRoutes.login, builder: (context, state) => const LoginScreen()),
    GoRoute(path: AppRoutes.register, builder: (context, state) => const RegisterScreen()),

    // [ĐÃ SỬA] Trỏ tới màn hình VerifyOtpScreen thay vì Scaffold Text
    GoRoute(
      path: AppRoutes.verifyOtp,
      builder: (context, state) => const VerifyOtpScreen(),
    ),

    // [ĐÃ SỬA] Trỏ tới màn hình ForgotPasswordScreen thay vì Scaffold Text
    GoRoute(
      path: AppRoutes.forgotPassword,
      builder: (context, state) => const ForgotPasswordScreen(),
    ),

    // =========================================================
    // CÁC TRANG FULL-SCREEN CẦN CÓ parentNavigatorKey
    // =========================================================
    GoRoute(
      // ĐÃ SỬA: Bắt đúng đường dẫn /category/:id thay vì /service/:id
      path: '/category/:id',
      parentNavigatorKey: _rootNavigatorKey,
      builder: (context, state) => ServiceDetailScreen(serviceId: state.pathParameters['id'] ?? ''),
    ),

    GoRoute(
      path: AppRoutes.createBooking,
      parentNavigatorKey: _rootNavigatorKey,
      builder: (context, state) {
        final serviceId = state.pathParameters['serviceId'] ?? '';
        return CreateBookingScreen(serviceId: serviceId);
      },
    ),

    GoRoute(
      path: '/booking/:id',
      parentNavigatorKey: _rootNavigatorKey,
      builder: (context, state) => BookingDetailScreen(bookingId: state.pathParameters['id'] ?? ''),
    ),

    GoRoute(
      path: AppRoutes.findingWorker,
      parentNavigatorKey: _rootNavigatorKey,
      builder: (context, state) {
        final bookingId = state.pathParameters['bookingId'] ?? '';
        return FindingWorkerScreen(bookingId: bookingId);
      },
    ),

    GoRoute(
        path: AppRoutes.addressManagement,
        parentNavigatorKey: _rootNavigatorKey,
        builder: (context, state) => const AddressManagementScreen()
    ),

    GoRoute(
      path: AppRoutes.editProfile,
      parentNavigatorKey: _rootNavigatorKey,
      builder: (context, state) => Scaffold(appBar: AppBar(title: const Text('Edit Profile')), body: const Center(child: Text('EditProfileScreen'))),
    ),

    GoRoute(
      path: '/payment/:bookingId',
      parentNavigatorKey: _rootNavigatorKey,
      builder: (context, state) => Scaffold(appBar: AppBar(title: const Text('Payment')), body: const Center(child: Text('PaymentScreen'))),
    ),

    GoRoute(
      path: '/review/:bookingId',
      parentNavigatorKey: _rootNavigatorKey,
      builder: (context, state) => Scaffold(appBar: AppBar(title: const Text('Write a Review')), body: const Center(child: Text('ReviewScreen'))),
    ),

    // =========================================================
    // SHELL ROUTES (CÁC TRANG CÓ BOTTOM NAV BAR)
    // =========================================================
    StatefulShellRoute.indexedStack(
      builder: (context, state, navigationShell) => ClientShell(navigationShell: navigationShell),
      branches: [
        StatefulShellBranch(navigatorKey: _shellNavigatorHomeKey, routes: [GoRoute(path: AppRoutes.clientShell, builder: (context, state) => const HomeScreen())]),
        StatefulShellBranch(navigatorKey: _shellNavigatorBookingsKey, routes: [GoRoute(path: '/bookings', builder: (context, state) => const BookingsScreen())]),
        StatefulShellBranch(navigatorKey: _shellNavigatorChatKey, routes: [GoRoute(path: '/chat', builder: (context, state) => const ChatScreen())]),
        StatefulShellBranch(navigatorKey: _shellNavigatorNotificationsKey, routes: [GoRoute(path: '/notifications', builder: (context, state) => const NotificationsScreen())]),
        StatefulShellBranch(navigatorKey: _shellNavigatorProfileKey, routes: [GoRoute(path: '/profile', builder: (context, state) => const ProfileScreen())]),
      ],
    ),

    StatefulShellRoute.indexedStack(
      builder: (context, state, navigationShell) => WorkerShell(navigationShell: navigationShell),
      branches: [
        StatefulShellBranch(navigatorKey: _workerShellHomeKey, routes: [GoRoute(path: AppRoutes.workerShell, builder: (context, state) => const WorkerDashboardScreen())]),
        StatefulShellBranch(navigatorKey: _workerShellJobsKey, routes: [GoRoute(path: '/worker/jobs', builder: (context, state) => const WorkerJobsScreen())]),
        StatefulShellBranch(navigatorKey: _workerShellWalletKey, routes: [GoRoute(path: '/worker/wallet', builder: (context, state) => const WorkerWalletScreen())]),
      ],
    ),

    GoRoute(path: AppRoutes.admin, builder: (context, state) => const AdminDashboardScreen()),
  ],
);