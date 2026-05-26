import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../stores/auth_store.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authStoreProvider);
    final cs   = Theme.of(context).colorScheme;
    final tt   = Theme.of(context).textTheme;

    if (!auth.isLoggedIn) {
      return Scaffold(
        body: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(Icons.person_outline, size: 72, color: cs.outline),
              const SizedBox(height: 16),
              Text('请先登录', style: tt.titleMedium),
              const SizedBox(height: 24),
              FilledButton.icon(
                onPressed: () => context.go('/login'),
                icon: const Icon(Icons.login),
                label: const Text('去登录'),
              ),
            ],
          ),
        ),
      );
    }

    final isTeacher = auth.role == 'Teacher';

    return Scaffold(
      backgroundColor: cs.background,
      body: CustomScrollView(
        slivers: [
          SliverAppBar(
            expandedHeight: 220,
            pinned: true,
            backgroundColor: cs.primary,
            flexibleSpace: FlexibleSpaceBar(
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
                      const EdgeInsets.only(top: 80, left: 24, right: 24),
                  child: Column(
                    children: [
                      CircleAvatar(
                        radius: 40,
                        backgroundColor: Colors.white.withOpacity(0.3),
                        child: Text(
                          (auth.username ?? '?')[0].toUpperCase(),
                          style: TextStyle(
                              fontSize: 32,
                              fontWeight: FontWeight.bold,
                              color: cs.onPrimary),
                        ),
                      ),
                      const SizedBox(height: 10),
                      Text(auth.username ?? '',
                          style: tt.titleLarge?.copyWith(
                              color: cs.onPrimary,
                              fontWeight: FontWeight.bold)),
                      const SizedBox(height: 4),
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 12, vertical: 4),
                        decoration: BoxDecoration(
                          color: Colors.white.withOpacity(0.2),
                          borderRadius: BorderRadius.circular(20),
                        ),
                        child: Row(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Text(isTeacher ? '📚' : '🎓',
                                style: const TextStyle(fontSize: 14)),
                            const SizedBox(width: 4),
                            Text(isTeacher ? '教师' : '学生',
                                style: TextStyle(
                                    color: cs.onPrimary, fontSize: 13)),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            actions: [
              IconButton(
                icon: Icon(Icons.edit_outlined, color: cs.onPrimary),
                onPressed: () {/* TODO: 编辑个人资料 */},
              ),
            ],
          ),

          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // ── 账号信息 ──────────────────────────────────────
                  Text('账号信息',
                      style: tt.titleSmall?.copyWith(
                          color: cs.onSurfaceVariant,
                          fontWeight: FontWeight.w600)),
                  const SizedBox(height: 8),
                  Card(
                    elevation: 0,
                    color: cs.surfaceVariant.withOpacity(0.3),
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(16)),
                    child: Column(children: [
                      _InfoRow(
                          icon: Icons.person_outline,
                          label: '用户名',
                          value: auth.username ?? '-'),
                      _Divider(),
                      _InfoRow(
                          icon: Icons.badge_outlined,
                          label: '身份',
                          value: isTeacher ? '教师' : '学生'),
                      _Divider(),
                      _InfoRow(
                          icon: Icons.business_outlined,
                          label: '所属机构',
                          value: auth.tenantId != null
                              ? '已绑定'
                              : '未绑定'),
                    ]),
                  ),
                  const SizedBox(height: 20),

                  // ── 学习统计（占位） ───────────────────────────────
                  Text('学习概览',
                      style: tt.titleSmall?.copyWith(
                          color: cs.onSurfaceVariant,
                          fontWeight: FontWeight.w600)),
                  const SizedBox(height: 8),
                  Row(children: [
                    _StatCard(
                        label: '参加考试', value: '-', icon: Icons.quiz_outlined),
                    const SizedBox(width: 12),
                    _StatCard(
                        label: '练习题数', value: '-', icon: Icons.edit_note_rounded),
                    const SizedBox(width: 12),
                    _StatCard(
                        label: '平均分', value: '-', icon: Icons.star_outline),
                  ]),
                  const SizedBox(height: 32),

                  // ── 退出登录 ──────────────────────────────────────
                  SizedBox(
                    width: double.infinity,
                    height: 48,
                    child: OutlinedButton.icon(
                      onPressed: () async {
                        await ref.read(authStoreProvider.notifier).logout();
                        if (context.mounted) context.go('/home');
                      },
                      icon: const Icon(Icons.logout),
                      label: const Text('退出登录'),
                      style: OutlinedButton.styleFrom(
                        foregroundColor: cs.error,
                        side: BorderSide(color: cs.error.withOpacity(0.5)),
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12)),
                      ),
                    ),
                  ),
                  const SizedBox(height: 32),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;

  const _InfoRow({required this.icon, required this.label, required this.value});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      child: Row(children: [
        Icon(icon, size: 18, color: cs.onSurfaceVariant),
        const SizedBox(width: 12),
        Text(label, style: TextStyle(color: cs.onSurfaceVariant, fontSize: 14)),
        const Spacer(),
        Text(value,
            style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w500)),
      ]),
    );
  }
}

class _Divider extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Divider(height: 1, indent: 46, endIndent: 0, color: cs.outline.withOpacity(0.2));
  }
}

class _StatCard extends StatelessWidget {
  final String label;
  final String value;
  final IconData icon;

  const _StatCard({required this.label, required this.value, required this.icon});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Expanded(
      child: Card(
        elevation: 0,
        color: cs.primaryContainer.withOpacity(0.4),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        child: Padding(
          padding: const EdgeInsets.symmetric(vertical: 16),
          child: Column(
            children: [
              Icon(icon, size: 20, color: cs.primary),
              const SizedBox(height: 6),
              Text(value,
                  style: TextStyle(
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                      color: cs.primary)),
              const SizedBox(height: 2),
              Text(label,
                  style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant)),
            ],
          ),
        ),
      ),
    );
  }
}


class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(authStoreProvider);
    final cs = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(title: const Text('个人中心')),
      body: ListView(
        children: [
          const SizedBox(height: 24),
          Center(
            child: CircleAvatar(
              radius: 40,
              backgroundColor: cs.primaryContainer,
              child: Text(
                state.username?.substring(0, 1).toUpperCase() ?? '?',
                style: TextStyle(fontSize: 32, color: cs.primary),
              ),
            ),
          ),
          const SizedBox(height: 12),
          Center(
            child: Text(
              state.username ?? '未登录',
              style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w600),
            ),
          ),
          const SizedBox(height: 32),
          const Divider(),
          ListTile(
            leading: const Icon(Icons.logout, color: Colors.red),
            title: const Text('退出登录', style: TextStyle(color: Colors.red)),
            onTap: () async {
              final ok = await showDialog<bool>(
                context: context,
                builder: (ctx) => AlertDialog(
                  title: const Text('退出登录'),
                  content: const Text('确认退出当前账号吗？'),
                  actions: [
                    TextButton(
                        onPressed: () => Navigator.pop(ctx, false),
                        child: const Text('取消')),
                    ElevatedButton(
                        onPressed: () => Navigator.pop(ctx, true),
                        child: const Text('确认退出')),
                  ],
                ),
              );
              if (ok == true) {
                await ref.read(authStoreProvider.notifier).logout();
                if (context.mounted) context.go('/login');
              }
            },
          ),
        ],
      ),
    );
  }
}
