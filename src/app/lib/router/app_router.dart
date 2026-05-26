import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../features/auth/screens/login_screen.dart';
import '../features/auth/screens/register_screen.dart';
import '../features/home/screens/home_screen.dart';
import '../features/exam/screens/exam_list_screen.dart';
import '../features/exam/screens/exam_room_screen.dart';
import '../features/practice/screens/practice_screen.dart';
import '../features/messages/screens/messages_screen.dart';
import '../features/profile/screens/profile_screen.dart';
import '../shared/shell/main_shell.dart';
import '../stores/auth_store.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  final authState = ref.watch(authStoreProvider);

  return GoRouter(
    initialLocation: '/home',
    redirect: (context, state) {
      final isLoggedIn = authState.token != null;
      final loc = state.matchedLocation;

      // 已登录用户访问登录/注册页直接跳首页
      if (isLoggedIn && (loc == '/login' || loc == '/register')) {
        return '/home';
      }
      // 需要强制登录的页面
      const protected = ['/messages', '/profile'];
      if (!isLoggedIn && protected.any((p) => loc.startsWith(p))) {
        return '/login?redirect=${Uri.encodeComponent(loc)}';
      }
      return null;
    },
    routes: [
      GoRoute(
        path: '/login',
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: '/register',
        builder: (context, state) => const RegisterScreen(),
      ),
      ShellRoute(
        builder: (context, state, child) => MainShell(child: child),
        routes: [
          GoRoute(
            path: '/home',
            builder: (context, state) => const HomeScreen(),
          ),
          GoRoute(
            path: '/exams',
            builder: (context, state) => const ExamListScreen(),
          ),
          GoRoute(
            path: '/practice',
            builder: (context, state) => const PracticeScreen(),
          ),
          GoRoute(
            path: '/messages',
            builder: (context, state) => const MessagesScreen(),
          ),
          GoRoute(
            path: '/profile',
            builder: (context, state) => const ProfileScreen(),
          ),
        ],
      ),
      GoRoute(
        path: '/exams/:id/room',
        builder: (context, state) => ExamRoomScreen(
          examId: int.parse(state.pathParameters['id']!),
        ),
      ),
    ],
  );
});
