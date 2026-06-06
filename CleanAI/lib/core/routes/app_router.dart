import 'package:go_router/go_router.dart';
import 'package:flutter/material.dart';
import '../../ui/auth/splash_screen.dart';
import '../../ui/auth/onboarding_screen.dart';
import '../../ui/auth/login_screen.dart';
import '../../ui/auth/register_screen.dart';
import '../../ui/home/client_shell.dart';
import '../../ui/home/home_screen.dart';
import '../../ui/booking/bookings_screen.dart';
import '../../ui/booking/create_booking_screen.dart';
import '../../ui/booking/booking_detail_screen.dart';
import '../../ui/chat/chat_screen.dart';
import '../../ui/notification/notifications_screen.dart';
import '../../ui/profile/profile_screen.dart';
import '../../ui/profile/address_management_screen.dart';
import '../../ui/service/service_detail_screen.dart';
import '../../ui/worker/worker_dashboard_screen.dart';
import '../../ui/worker/worker_jobs_screen.dart';
import '../../ui/worker/worker_wallet_screen.dart';
import '../../ui/admin/admin_dashboard_screen.dart';

// Route name constants
class AppRoutes {
  static const splash = '/';
  static const onboarding = '/onboarding';
  static const login = '/login';
  static const register = '/register';

  // Client
  static const clientShell = '/home';
  static const home = '/home/dashboard';
  static const bookings = '/home/bookings';
  static const chat = '/home/chat';
  static const notifications = '/home/notifications';
  static const profile = '/home/profile';

  // Detail screens (pushed on top of shell)
  static const serviceDetail = '/service/:id';
  static const createBooking = '/booking/create';
  static const bookingDetail = '/booking/:id';
  static const addressManagement = '/address';

  // Worker
  static const workerShell = '/worker';
  static const workerDashboard = '/worker/dashboard';
  static const workerJobs = '/worker/jobs';
  static const workerWallet = '/worker/wallet';

  // Admin
  static const admin = '/admin';
}

final _rootNavigatorKey = GlobalKey<NavigatorState>(debugLabel: 'root');
final _shellNavigatorHomeKey =
    GlobalKey<NavigatorState>(debugLabel: 'shellHome');
final _shellNavigatorBookingsKey =
    GlobalKey<NavigatorState>(debugLabel: 'shellBookings');
final _shellNavigatorChatKey =
    GlobalKey<NavigatorState>(debugLabel: 'shellChat');
final _shellNavigatorNotificationsKey =
    GlobalKey<NavigatorState>(debugLabel: 'shellNotifications');
final _shellNavigatorProfileKey =
    GlobalKey<NavigatorState>(debugLabel: 'shellProfile');

final _workerShellHomeKey =
    GlobalKey<NavigatorState>(debugLabel: 'workerShellHome');
final _workerShellJobsKey =
    GlobalKey<NavigatorState>(debugLabel: 'workerShellJobs');
final _workerShellWalletKey =
    GlobalKey<NavigatorState>(debugLabel: 'workerShellWallet');



final GoRouter appRouter = GoRouter(
  navigatorKey: _rootNavigatorKey,
  initialLocation: AppRoutes.splash,
  routes: [
    // ── Auth ──────────────────────────────────────────────────────────────────
    GoRoute(
      path: AppRoutes.splash,
      builder: (context, state) => const SplashScreen(),
    ),
    GoRoute(
      path: AppRoutes.onboarding,
      builder: (context, state) => const OnboardingScreen(),
    ),
    GoRoute(
      path: AppRoutes.login,
      builder: (context, state) => const LoginScreen(),
    ),
    GoRoute(
      path: AppRoutes.register,
      builder: (context, state) => const RegisterScreen(),
    ),

    // ── Service Detail (root navigator) ───────────────────────────────────────
    GoRoute(
      path: '/service/:id',
      builder: (context, state) {
        final id = state.pathParameters['id'] ?? '';
        return ServiceDetailScreen(serviceId: id);
      },
    ),
    GoRoute(
      path: AppRoutes.createBooking,
      builder: (context, state) => const CreateBookingScreen(),
    ),
    GoRoute(
      path: '/booking/:id',
      builder: (context, state) {
        final id = state.pathParameters['id'] ?? '';
        return BookingDetailScreen(bookingId: id);
      },
    ),
    GoRoute(
      path: AppRoutes.addressManagement,
      builder: (context, state) => const AddressManagementScreen(),
    ),

    // ── Client Shell (StatefulShellRoute) ─────────────────────────────────────
    StatefulShellRoute.indexedStack(
      builder: (context, state, navigationShell) =>
          ClientShell(navigationShell: navigationShell),
      branches: [
        StatefulShellBranch(
          navigatorKey: _shellNavigatorHomeKey,
          routes: [
            GoRoute(
              path: AppRoutes.clientShell,
              builder: (context, state) => const HomeScreen(),
            ),
          ],
        ),
        StatefulShellBranch(
          navigatorKey: _shellNavigatorBookingsKey,
          routes: [
            GoRoute(
              path: '/bookings',
              builder: (context, state) => const BookingsScreen(),
            ),
          ],
        ),
        StatefulShellBranch(
          navigatorKey: _shellNavigatorChatKey,
          routes: [
            GoRoute(
              path: '/chat',
              builder: (context, state) => const ChatScreen(),
            ),
          ],
        ),
        StatefulShellBranch(
          navigatorKey: _shellNavigatorNotificationsKey,
          routes: [
            GoRoute(
              path: '/notifications',
              builder: (context, state) => const NotificationsScreen(),
            ),
          ],
        ),
        StatefulShellBranch(
          navigatorKey: _shellNavigatorProfileKey,
          routes: [
            GoRoute(
              path: '/profile',
              builder: (context, state) => const ProfileScreen(),
            ),
          ],
        ),
      ],
    ),

    // ── Worker Shell ──────────────────────────────────────────────────────────
    StatefulShellRoute.indexedStack(
      builder: (context, state, navigationShell) =>
          WorkerShell(navigationShell: navigationShell),
      branches: [
        StatefulShellBranch(
          navigatorKey: _workerShellHomeKey,
          routes: [
            GoRoute(
              path: AppRoutes.workerShell,
              builder: (context, state) => const WorkerDashboardScreen(),
            ),
          ],
        ),
        StatefulShellBranch(
          navigatorKey: _workerShellJobsKey,
          routes: [
            GoRoute(
              path: '/worker/jobs',
              builder: (context, state) => const WorkerJobsScreen(),
            ),
          ],
        ),
        StatefulShellBranch(
          navigatorKey: _workerShellWalletKey,
          routes: [
            GoRoute(
              path: '/worker/wallet',
              builder: (context, state) => const WorkerWalletScreen(),
            ),
          ],
        ),
      ],
    ),

    // ── Admin ─────────────────────────────────────────────────────────────────
    GoRoute(
      path: AppRoutes.admin,
      builder: (context, state) => const AdminDashboardScreen(),
    ),
  ],
);
