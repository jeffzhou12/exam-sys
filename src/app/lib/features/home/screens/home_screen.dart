import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../stores/auth_store.dart';

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authStoreProvider);
    final cs   = Theme.of(context).colorScheme;
    final tt   = Theme.of(context).textTheme;

    return Scaffold(
      backgroundColor: cs.background,
      body: CustomScrollView(
        slivers: [
          // ── 顶部 Banner ────────────────────────────────────────────
          SliverAppBar(
            expandedHeight: 180,
            floating: false,
            pinned: true,
            backgroundColor: cs.primary,
            flexibleSpace: FlexibleSpaceBar(
              titlePadding:
                  const EdgeInsets.only(left: 20, bottom: 16, right: 20),
              title: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Icon(Icons.school_rounded, color: cs.onPrimary, size: 20),
                  const SizedBox(width: 8),
                  Text('考试系统',
                      style: TextStyle(
                          color: cs.onPrimary, fontWeight: FontWeight.bold)),
                ],
              ),
              background: Container(
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                    colors: [cs.primary, cs.primaryContainer],
                  ),
                ),
                child: Padding(
                  padding:
                      const EdgeInsets.only(left: 20, right: 20, top: 60),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      if (auth.isLoggedIn) ...[
                        Text(
                          '你好，${auth.username ?? '同学'} 👋',
                          style: tt.titleLarge?.copyWith(
                              color: cs.onPrimary,
                              fontWeight: FontWeight.bold),
                        ),
                        const SizedBox(height: 4),
                        Text(
                          auth.role == 'Teacher' ? '教师端 · 管理课程与考试' : '学生端 · 参加考试与练习',
                          style: tt.bodySmall
                              ?.copyWith(color: cs.onPrimary.withOpacity(0.8)),
                        ),
                      ] else ...[
                        Text('在线考试平台',
                            style: tt.titleLarge?.copyWith(
                                color: cs.onPrimary,
                                fontWeight: FontWeight.bold)),
                        const SizedBox(height: 4),
                        Text('随时随地参加考试，轻松管理学习进度',
                            style: tt.bodySmall?.copyWith(
                                color: cs.onPrimary.withOpacity(0.8))),
                      ],
                    ],
                  ),
                ),
              ),
            ),
            actions: [
              if (!auth.isLoggedIn)
                Padding(
                  padding: const EdgeInsets.only(right: 8),
                  child: TextButton(
                    onPressed: () => context.go('/login'),
                    style: TextButton.styleFrom(
                        foregroundColor: cs.onPrimary,
                        backgroundColor: Colors.white.withOpacity(0.15)),
                    child: const Text('登录 / 注册'),
                  ),
                ),
            ],
          ),

          // ── 未登录提示条 ──────────────────────────────────────────
          if (!auth.isLoggedIn)
            SliverToBoxAdapter(
              child: Container(
                margin: const EdgeInsets.all(16),
                padding:
                    const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
                decoration: BoxDecoration(
                  color: cs.primaryContainer,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Row(children: [
                  Icon(Icons.info_outline, color: cs.primary, size: 20),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      '登录后可参加考试、查看成绩、访问更多功能',
                      style: TextStyle(
                          color: cs.onPrimaryContainer, fontSize: 13),
                    ),
                  ),
                  TextButton(
                    onPressed: () => context.go('/login'),
                    style: TextButton.styleFrom(
                        foregroundColor: cs.primary,
                        padding: EdgeInsets.zero,
                        minimumSize: const Size(48, 32)),
                    child: const Text('去登录'),
                  ),
                ]),
              ),
            ),

          // ── 功能入口 ──────────────────────────────────────────────
          SliverPadding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            sliver: SliverToBoxAdapter(
              child: Text('功能入口',
                  style: tt.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
            ),
          ),
          const SliverToBoxAdapter(child: SizedBox(height: 12)),
          SliverPadding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            sliver: SliverGrid.count(
              crossAxisCount: 2,
              crossAxisSpacing: 12,
              mainAxisSpacing: 12,
              childAspectRatio: 1.25,
              children: [
                _FeatureCard(
                  icon: Icons.assignment_rounded,
                  label: '参加考试',
                  desc: '查看可用考试',
                  color: const Color(0xFF1D4ED8),
                  bgColor: const Color(0xFFEFF6FF),
                  onTap: () => _navigateProtected(context, '/exams', auth),
                ),
                _FeatureCard(
                  icon: Icons.edit_note_rounded,
                  label: '练习刷题',
                  desc: '专项练习提升',
                  color: const Color(0xFF059669),
                  bgColor: const Color(0xFFECFDF5),
                  onTap: () => _navigateProtected(context, '/practice', auth),
                ),
                _FeatureCard(
                  icon: Icons.bar_chart_rounded,
                  label: '我的成绩',
                  desc: '查看考试成绩',
                  color: const Color(0xFFD97706),
                  bgColor: const Color(0xFFFFFBEB),
                  onTap: () => _navigateProtected(context, '/profile', auth),
                ),
                _FeatureCard(
                  icon: Icons.mail_rounded,
                  label: '站内消息',
                  desc: '通知与公告',
                  color: const Color(0xFF7C3AED),
                  bgColor: const Color(0xFFF5F3FF),
                  onTap: () => _navigateProtected(context, '/messages', auth),
                ),
              ],
            ),
          ),
          const SliverToBoxAdapter(child: SizedBox(height: 24)),
        ],
      ),
    );
  }

  void _navigateProtected(BuildContext context, String path, AuthState auth) {
    if (!auth.isLoggedIn) {
      showDialog(
        context: context,
        builder: (ctx) => AlertDialog(
          title: const Text('需要登录'),
          content: const Text('该功能需要登录后才能使用。'),
          actions: [
            TextButton(
                onPressed: () => Navigator.pop(ctx),
                child: const Text('取消')),
            FilledButton(
                onPressed: () {
                  Navigator.pop(ctx);
                  context.go('/login?redirect=${Uri.encodeComponent(path)}');
                },
                child: const Text('去登录')),
          ],
        ),
      );
    } else {
      context.go(path);
    }
  }
}

class _FeatureCard extends StatelessWidget {
  final IconData icon;
  final String label;
  final String desc;
  final Color color;
  final Color bgColor;
  final VoidCallback onTap;

  const _FeatureCard({
    required this.icon,
    required this.label,
    required this.desc,
    required this.color,
    required this.bgColor,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 0,
      color: bgColor,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                padding: const EdgeInsets.all(8),
                decoration: BoxDecoration(
                  color: color.withOpacity(0.12),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Icon(icon, color: color, size: 24),
              ),
              const Spacer(),
              Text(label,
                  style: TextStyle(
                      fontSize: 15,
                      fontWeight: FontWeight.w600,
                      color: color)),
              const SizedBox(height: 2),
              Text(desc,
                  style: TextStyle(
                      fontSize: 12,
                      color: color.withOpacity(0.7))),
            ],
          ),
        ),
      ),
    );
  }
}


  static const _entries = [
    _Entry(icon: Icons.assignment, label: '参加考试', path: '/exams', color: Color(0xFF1D4ED8)),
    _Entry(icon: Icons.edit_note,  label: '练习刷题', path: '/practice', color: Color(0xFF059669)),
    _Entry(icon: Icons.mail,       label: '站内消息', path: '/messages', color: Color(0xFFD97706)),
    _Entry(icon: Icons.person,     label: '个人中心', path: '/profile', color: Color(0xFF7C3AED)),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('考试系统')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: GridView.count(
          crossAxisCount: 2,
          crossAxisSpacing: 16,
          mainAxisSpacing: 16,
          children: _entries.map((e) => _EntryCard(entry: e)).toList(),
        ),
      ),
    );
  }
}

class _EntryCard extends StatelessWidget {
  final _Entry entry;
  const _EntryCard({required this.entry});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => context.go(entry.path),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(entry.icon, size: 48, color: entry.color),
            const SizedBox(height: 12),
            Text(entry.label,
                style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w500)),
          ],
        ),
      ),
    );
  }
}

class _Entry {
  final IconData icon;
  final String label;
  final String path;
  final Color color;
  const _Entry({required this.icon, required this.label, required this.path, required this.color});
}
