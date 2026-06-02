import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../api/api.dart';
import '../../../stores/auth_store.dart';
import '../../../theme/app_theme.dart';

final _profileProvider = FutureProvider((ref) => profileApi.getProfile());

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authStoreProvider);
    final cs   = Theme.of(context).colorScheme;
    final tt   = Theme.of(context).textTheme;
    final profileAsync = auth.isLoggedIn ? ref.watch(_profileProvider) : null;

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

    final profile = profileAsync?.value;
    final username = profile?.nickname ?? auth.username ?? '用户';
    final isTeacher = auth.role == 'Teacher';

    return Scaffold(
      backgroundColor: AppColors.bgPage,
      body: CustomScrollView(
        slivers: [
          // ── AppBar ─────────────────────────────────────────────────
          SliverAppBar(
            floating: true,
            backgroundColor: cs.surface,
            surfaceTintColor: Colors.transparent,
            title: Row(children: [
              CircleAvatar(
                radius: 14,
                backgroundColor: cs.primaryContainer,
                child: Text(
                  username[0].toUpperCase(),
                  style: TextStyle(fontWeight: FontWeight.bold, color: cs.primary, fontSize: 12),
                ),
              ),
              const SizedBox(width: 8),
              Text('EduFlow 智学', style: tt.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
            ]),
            actions: [
              IconButton(icon: const Icon(Icons.notifications_outlined), onPressed: () {}),
            ],
          ),

          SliverToBoxAdapter(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // ── 个人信息卡 ─────────────────────────────────────
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppColors.bgCard,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: AppColors.borderWeak),
                    ),
                    child: Row(
                      children: [
                        CircleAvatar(
                          radius: 34,
                          backgroundColor: AppColors.primary.withAlpha(20),
                          child: Text(
                            username.isNotEmpty ? username[0].toUpperCase() : '?',
                            style: const TextStyle(
                                fontSize: 26, fontWeight: FontWeight.bold, color: AppColors.primary),
                          ),
                        ),
                        const SizedBox(width: 14),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(username,
                                  style: tt.titleMedium?.copyWith(fontWeight: FontWeight.bold, color: AppColors.textMain)),
                              const SizedBox(height: 4),
                              if (profile?.email != null && profile!.email!.isNotEmpty)
                                Text(profile.email!,
                                    style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
                              if (profile?.phoneNumber != null && profile!.phoneNumber!.isNotEmpty)
                                Text(profile!.phoneNumber!,
                                    style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
                              if (profile?.email == null && profile?.phoneNumber == null)
                                Text(isTeacher ? '教师账号' : '学生账号',
                                    style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
                            ],
                          ),
                        ),
                        const Icon(Icons.chevron_right, color: AppColors.textWeak),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // ── 学习统计 ───────────────────────────────────────
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppColors.bgCard,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: AppColors.borderWeak),
                    ),
                    child: Row(children: [
                      _StatItem(value: '-', label: '连续学习', unit: '天', cs: cs, tt: tt),
                      Container(width: 1, height: 40, color: AppColors.borderWeak),
                      _StatItem(value: '-', label: '完成练习', unit: '题', cs: cs, tt: tt),
                      Container(width: 1, height: 40, color: AppColors.borderWeak),
                      _StatItem(value: '-', label: '平均得分', unit: '分', cs: cs, tt: tt),
                    ]),
                  ),
                  const SizedBox(height: 16),

                  // ── 功能菜单 ───────────────────────────────────────
                  Container(
                    decoration: BoxDecoration(
                      color: AppColors.bgCard,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: AppColors.borderWeak),
                    ),
                    child: Column(children: [
                      _MenuItem(
                        icon: Icons.edit_outlined,
                        label: '编辑资料',
                        onTap: () => context.push('/profile/edit'),
                        cs: cs, tt: tt,
                        showDivider: true,
                      ),
                      _MenuItem(
                        icon: Icons.bookmark_outline,
                        label: '我的收藏',
                        badge: '12',
                        onTap: () => context.push('/favorites'),
                        cs: cs, tt: tt,
                        showDivider: true,
                      ),
                      _MenuItem(
                        icon: Icons.history_edu_outlined,
                        label: '考试记录',
                        onTap: () => context.push('/exams'),
                        cs: cs, tt: tt,
                        showDivider: true,
                      ),
                      _MenuItem(
                        icon: Icons.edit_document,
                        label: '错题本',
                        onTap: () => context.push('/wrong-book'),
                        cs: cs, tt: tt,
                        showDivider: true,
                      ),
                      _MenuItem(
                        icon: Icons.settings_outlined,
                        label: '系统设置',
                        onTap: () {},
                        cs: cs, tt: tt,
                        showDivider: true,
                      ),
                      _MenuItem(
                        icon: Icons.help_center_outlined,
                        label: '帮助中心',
                        onTap: () {},
                        cs: cs, tt: tt,
                        showDivider: false,
                      ),
                    ]),
                  ),
                  const SizedBox(height: 24),

                  // ── 退出登录 ───────────────────────────────────────
                  SizedBox(
                    width: double.infinity,
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
                        padding: const EdgeInsets.symmetric(vertical: 14),
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
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

class _StatItem extends StatelessWidget {
  final String value, label, unit;
  final ColorScheme cs;
  final TextTheme tt;
  const _StatItem({required this.value, required this.label, required this.unit,
      required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Column(children: [
        RichText(
          text: TextSpan(
            style: tt.titleMedium?.copyWith(fontWeight: FontWeight.bold, color: cs.onSurface),
            children: [
              TextSpan(text: value),
              TextSpan(text: unit,
                  style: TextStyle(fontSize: 12, color: cs.onSurfaceVariant, fontWeight: FontWeight.normal)),
            ],
          ),
        ),
        const SizedBox(height: 2),
        Text(label, style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant, fontSize: 11)),
      ]),
    );
  }
}

class _MenuItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final String? badge;
  final VoidCallback onTap;
  final ColorScheme cs;
  final TextTheme tt;
  final bool showDivider;
  const _MenuItem({required this.icon, required this.label, this.badge,
      required this.onTap, required this.cs, required this.tt, required this.showDivider});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        InkWell(
          borderRadius: showDivider
              ? BorderRadius.zero
              : const BorderRadius.vertical(bottom: Radius.circular(15)),
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
            child: Row(children: [
              Icon(icon, size: 20, color: cs.primary),
              const SizedBox(width: 14),
              Expanded(child: Text(label, style: tt.bodyMedium?.copyWith(fontWeight: FontWeight.w500))),
              if (badge != null)
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                  decoration: BoxDecoration(
                    color: cs.primaryContainer, borderRadius: BorderRadius.circular(10)),
                  child: Text(badge!, style: TextStyle(fontSize: 11, color: cs.primary, fontWeight: FontWeight.bold)),
                ),
              const SizedBox(width: 4),
              Icon(Icons.chevron_right, size: 18, color: cs.onSurfaceVariant),
            ]),
          ),
        ),
        if (showDivider)
          Divider(height: 1, indent: 50, color: cs.outlineVariant),
      ],
    );
  }
}
