import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../features/auth/screens/login_screen.dart';
import '../features/auth/screens/register_screen.dart';
import '../features/auth/screens/forgot_password_screen.dart';
import '../features/home/screens/home_screen.dart';
import '../features/exam/screens/exam_list_screen.dart';
import '../features/exam/screens/exam_detail_screen.dart';
import '../features/exam/screens/exam_room_screen.dart';
import '../features/practice/screens/practice_screen.dart';
import '../features/messages/screens/messages_screen.dart';
import '../features/profile/screens/profile_screen.dart';
import '../features/profile/screens/edit_profile_screen.dart';
import '../features/library/screens/library_screen.dart';
import '../features/library/screens/book_detail_screen.dart';
import '../features/library/screens/book_reader_screen.dart';
import '../features/wrong_book/screens/wrong_book_screen.dart';
import '../features/ai_analysis/screens/ai_analysis_screen.dart';
import '../features/favorites/screens/favorites_screen.dart';
import '../shared/shell/main_shell.dart';
import '../stores/auth_store.dart';

final appRouterProvider = Provider<GoRouter>((ref) {
  final authState = ref.watch(authStoreProvider);

  return GoRouter(
    initialLocation: '/home',
    redirect: (context, state) {
      final isLoggedIn = authState.token != null;
      final loc = state.matchedLocation;

      // 已登录用户访问登录/注册/忘记密码页直接跳首页
      if (isLoggedIn && (loc == '/login' || loc == '/register' || loc == '/forgot-password')) {
        return '/home';
      }
      // 需要强制登录的页面
      const protected = ['/messages', '/profile', '/favorites', '/wrong-book', '/ai-analysis'];
      if (!isLoggedIn && protected.any((p) => loc.startsWith(p))) {
        return '/login?redirect=${Uri.encodeComponent(loc)}';
      }
      return null;
    },
    routes: [
      // ── 认证页面 ────────────────────────────────────────────────────
      GoRoute(
        path: '/login',
        builder: (context, state) => const LoginScreen(),
      ),
      GoRoute(
        path: '/register',
        builder: (context, state) => const RegisterScreen(),
      ),
      GoRoute(
        path: '/forgot-password',
        builder: (context, state) => const ForgotPasswordScreen(),
      ),

      // ── 主 Shell（底部导航） ─────────────────────────────────────────
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
            path: '/library',
            builder: (context, state) => const LibraryScreen(),
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

      // ── 考试相关（全屏） ─────────────────────────────────────────────
      GoRoute(
        path: '/exams/:id/detail',
        builder: (context, state) => ExamDetailScreen(
          examId: state.pathParameters['id']!,
        ),
      ),
      GoRoute(
        path: '/exams/:id/room',
        builder: (context, state) => ExamRoomScreen(
          examId: state.pathParameters['id']!,
        ),
      ),

      // ── 图书相关 ────────────────────────────────────────────────────
      GoRoute(
        path: '/books/detail/:id',
        builder: (context, state) => BookDetailScreen(
          bookId: state.pathParameters['id']!,
        ),
      ),
      GoRoute(
        path: '/books/:id/read',
        builder: (context, state) => BookReaderScreen(
          bookId: state.pathParameters['id']!,
        ),
      ),

      // ── 其他功能页 ──────────────────────────────────────────────────
      GoRoute(
        path: '/wrong-book',
        builder: (context, state) => const WrongBookScreen(),
      ),
      GoRoute(
        path: '/ai-analysis',
        builder: (context, state) => const AIAnalysisScreen(),
      ),
      GoRoute(
        path: '/favorites',
        builder: (context, state) => const FavoritesScreen(),
      ),
      GoRoute(
        path: '/profile/edit',
        builder: (context, state) => const EditProfileScreen(),
      ),
    ],
  );
});
