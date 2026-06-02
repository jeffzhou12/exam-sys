import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../api/api.dart';
import '../../../api/models/exam_models.dart';
import '../../../stores/auth_store.dart';
import '../../../theme/app_theme.dart';

final _recentExamsProvider = FutureProvider<List<ExamPaper>>((ref) async {
  final result = await examsApi.getExams(pageSize: 5);
  return result.items;
});

class HomeScreen extends ConsumerWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final auth = ref.watch(authStoreProvider);
    final cs   = Theme.of(context).colorScheme;
    final tt   = Theme.of(context).textTheme;

    return Scaffold(
      backgroundColor: cs.surfaceContainerLowest,
      body: CustomScrollView(
        slivers: [
          // ── 顶部 AppBar ────────────────────────────────────────────
          SliverAppBar(
            floating: true,
            pinned: false,
            backgroundColor: cs.surface,
            surfaceTintColor: Colors.transparent,
            title: Row(
              children: [
                CircleAvatar(
                  radius: 16,
                  backgroundColor: cs.primaryContainer,
                  child: Icon(Icons.person, size: 18, color: cs.primary),
                ),
                const SizedBox(width: 10),
                Text('EduFlow 智学',
                    style: tt.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
              ],
            ),
            actions: [
              IconButton(
                icon: const Icon(Icons.notifications_outlined),
                onPressed: () => context.go('/messages'),
              ),
              if (!auth.isLoggedIn)
                Padding(
                  padding: const EdgeInsets.only(right: 8),
                  child: TextButton(
                    onPressed: () => context.go('/login'),
                    child: const Text('登录'),
                  ),
                ),
            ],
          ),

          // ── 继续学习卡片 ───────────────────────────────────────────
          if (auth.isLoggedIn)
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
              sliver: SliverToBoxAdapter(
                child: _ContinueLearningCard(cs: cs, tt: tt),
              ),
            ),

          // ── 本周学习统计 ───────────────────────────────────────────
          if (auth.isLoggedIn)
            SliverPadding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
              sliver: SliverToBoxAdapter(
                child: _WeeklyStatsCard(cs: cs, tt: tt),
              ),
            ),

          // ── 未登录提示 ────────────────────────────────────────────
          if (!auth.isLoggedIn)
            SliverPadding(
              padding: const EdgeInsets.all(16),
              sliver: SliverToBoxAdapter(
                child: _LoginPromptCard(cs: cs),
              ),
            ),

          // ── AI 智能推荐 ────────────────────────────────────────────
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
            sliver: SliverToBoxAdapter(
              child: _AiRecommendCard(cs: cs, tt: tt),
            ),
          ),

          // ── 近期考试 ───────────────────────────────────────────────
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
            sliver: SliverToBoxAdapter(
              child: const _UpcomingExamsCard(),
            ),
          ),

          // ── 快速练习入口 ────────────────────────────────────────────
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
            sliver: SliverToBoxAdapter(
              child: _QuickPracticeCard(cs: cs, tt: tt),
            ),
          ),

          // ── 近期阅读 ───────────────────────────────────────────────
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 24),
            sliver: SliverToBoxAdapter(
              child: _RecentBooksCard(cs: cs, tt: tt),
            ),
          ),
        ],
      ),
    );
  }
}

// ── 继续学习卡片 ──────────────────────────────────────────────────────────────
class _ContinueLearningCard extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _ContinueLearningCard({required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [cs.primary, cs.primaryContainer],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.auto_stories, color: cs.onPrimary, size: 18),
              const SizedBox(width: 6),
              Text('继续学习',
                  style: tt.labelMedium?.copyWith(color: cs.onPrimary.withOpacity(0.8))),
            ],
          ),
          const SizedBox(height: 8),
          Text('高级微观经济学',
              style: tt.titleMedium
                  ?.copyWith(color: cs.onPrimary, fontWeight: FontWeight.bold)),
          const SizedBox(height: 4),
          Text('从上次离开的地方开始：第四章 市场均衡与价格弹性',
              style: tt.bodySmall
                  ?.copyWith(color: cs.onPrimary.withOpacity(0.8)),
              maxLines: 1,
              overflow: TextOverflow.ellipsis),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Text('进度',
                            style: tt.bodySmall
                                ?.copyWith(color: cs.onPrimary.withOpacity(0.7))),
                        Text('65%',
                            style: tt.bodySmall?.copyWith(
                                color: cs.onPrimary, fontWeight: FontWeight.bold)),
                      ],
                    ),
                    const SizedBox(height: 4),
                    ClipRRect(
                      borderRadius: BorderRadius.circular(4),
                      child: LinearProgressIndicator(
                        value: 0.65,
                        backgroundColor: cs.onPrimary.withOpacity(0.2),
                        valueColor: AlwaysStoppedAnimation<Color>(cs.onPrimary),
                        minHeight: 6,
                      ),
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 12),
              FilledButton.icon(
                onPressed: () => context.go('/library'),
                style: FilledButton.styleFrom(
                  backgroundColor: cs.onPrimary,
                  foregroundColor: cs.primary,
                  padding:
                      const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
                ),
                icon: const Icon(Icons.play_arrow, size: 18),
                label: const Text('继续'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

// ── 本周学习统计 ──────────────────────────────────────────────────────────────
class _WeeklyStatsCard extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _WeeklyStatsCard({required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cs.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: cs.outlineVariant),
      ),
      child: Column(
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('本周学习',
                  style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
              Icon(Icons.query_stats, size: 18, color: cs.primary),
            ],
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                  child: _StatItem(
                      icon: Icons.schedule,
                      label: '学习时长',
                      value: '12.5 小时',
                      cs: cs,
                      tt: tt)),
              Container(width: 1, height: 40, color: cs.outlineVariant),
              Expanded(
                  child: _StatItem(
                      icon: Icons.bolt,
                      label: '连续学习',
                      value: '8 天',
                      cs: cs,
                      tt: tt)),
            ],
          ),
          const SizedBox(height: 8),
          Align(
            alignment: Alignment.centerRight,
            child: GestureDetector(
              onTap: () => context.go('/profile'),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text('查看分析',
                      style: tt.labelSmall?.copyWith(color: cs.primary)),
                  Icon(Icons.chevron_right, size: 16, color: cs.primary),
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
  final IconData icon;
  final String label;
  final String value;
  final ColorScheme cs;
  final TextTheme tt;
  const _StatItem(
      {required this.icon,
      required this.label,
      required this.value,
      required this.cs,
      required this.tt});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Icon(icon, size: 20, color: cs.primary),
        const SizedBox(height: 4),
        Text(value,
            style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
        Text(label,
            style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
      ],
    );
  }
}

// ── 未登录提示 ────────────────────────────────────────────────────────────────
class _LoginPromptCard extends StatelessWidget {
  final ColorScheme cs;
  const _LoginPromptCard({required this.cs});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [cs.primary, cs.primaryContainer],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('在线学习平台',
              style: TextStyle(
                  color: Colors.white,
                  fontSize: 22,
                  fontWeight: FontWeight.bold)),
          const SizedBox(height: 4),
          Text('随时随地参加考试，智能分析学习进度',
              style: TextStyle(
                  color: Colors.white.withOpacity(0.85), fontSize: 14)),
          const SizedBox(height: 16),
          Row(
            children: [
              FilledButton(
                onPressed: () => context.go('/login'),
                style: FilledButton.styleFrom(
                  backgroundColor: Colors.white,
                  foregroundColor: cs.primary,
                ),
                child: const Text('立即登录'),
              ),
              const SizedBox(width: 12),
              OutlinedButton(
                onPressed: () => context.go('/register'),
                style: OutlinedButton.styleFrom(
                  foregroundColor: Colors.white,
                  side: const BorderSide(color: Colors.white),
                ),
                child: const Text('免费注册'),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

// ── AI 智能推荐 ───────────────────────────────────────────────────────────────
class _AiRecommendCard extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _AiRecommendCard({required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cs.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: cs.outlineVariant),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.auto_awesome, size: 18, color: cs.primary),
              const SizedBox(width: 6),
              Text('AI 智能推荐',
                  style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
            ],
          ),
          const SizedBox(height: 12),
          _AiRecommendItem(
            icon: Icons.psychology,
            title: '重点关注：贝叶斯推断',
            desc: '根据你上次的测验，后验概率计算是你的薄弱环节。',
            tag: '数学',
            priority: '高优先级',
            cs: cs,
            tt: tt,
            onTap: () => context.go('/practice'),
          ),
          const SizedBox(height: 8),
          _AiRecommendItem(
            icon: Icons.menu_book,
            title: '复习薄弱点：细胞有丝分裂',
            desc: '你对"后期"阶段的记忆正在减退，为你准备了可视化总结。',
            tag: '生物',
            priority: null,
            cs: cs,
            tt: tt,
            onTap: () => context.go('/library'),
          ),
        ],
      ),
    );
  }
}

class _AiRecommendItem extends StatelessWidget {
  final IconData icon;
  final String title;
  final String desc;
  final String tag;
  final String? priority;
  final ColorScheme cs;
  final TextTheme tt;
  final VoidCallback onTap;
  const _AiRecommendItem({
    required this.icon,
    required this.title,
    required this.desc,
    required this.tag,
    this.priority,
    required this.cs,
    required this.tt,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return InkWell(
      borderRadius: BorderRadius.circular(12),
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: cs.primaryContainer.withOpacity(0.3),
          borderRadius: BorderRadius.circular(12),
        ),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                color: cs.primary.withOpacity(0.1),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Icon(icon, size: 20, color: cs.primary),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(title,
                      style: tt.bodyMedium
                          ?.copyWith(fontWeight: FontWeight.w600),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis),
                  const SizedBox(height: 2),
                  Text(desc,
                      style: tt.bodySmall
                          ?.copyWith(color: cs.onSurfaceVariant),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis),
                  const SizedBox(height: 6),
                  Row(
                    children: [
                      _Tag(label: tag, cs: cs),
                      if (priority != null) ...[
                        const SizedBox(width: 6),
                        _Tag(label: priority!, cs: cs, isHighlight: true),
                      ],
                    ],
                  ),
                ],
              ),
            ),
            Icon(Icons.arrow_forward, size: 16, color: cs.onSurfaceVariant),
          ],
        ),
      ),
    );
  }
}

class _Tag extends StatelessWidget {
  final String label;
  final ColorScheme cs;
  final bool isHighlight;
  const _Tag({required this.label, required this.cs, this.isHighlight = false});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
      decoration: BoxDecoration(
        color: isHighlight
            ? const Color(0xFFDC2626).withOpacity(0.1)
            : cs.primaryContainer,
        borderRadius: BorderRadius.circular(4),
      ),
      child: Text(
        label,
        style: TextStyle(
          fontSize: 11,
          color: isHighlight ? const Color(0xFFDC2626) : cs.primary,
          fontWeight: FontWeight.w500,
        ),
      ),
    );
  }
}

// ── 近期考试 ──────────────────────────────────────────────────────────────────
class _UpcomingExamsCard extends ConsumerWidget {
  const _UpcomingExamsCard();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final cs = Theme.of(context).colorScheme;
    final tt = Theme.of(context).textTheme;
    final examsAsync = ref.watch(_recentExamsProvider);

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.bgCard,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.borderWeak),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('近期考试', style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
              Icon(Icons.calendar_month, size: 18, color: cs.primary),
            ],
          ),
          const SizedBox(height: 12),
          examsAsync.when(
            loading: () => const Center(child: Padding(
              padding: EdgeInsets.all(16),
              child: CircularProgressIndicator(strokeWidth: 2),
            )),
            error: (_, __) => const Text('加载失败', style: TextStyle(color: AppColors.textSecondary)),
            data: (exams) => exams.isEmpty
                ? const Text('暂无考试', style: TextStyle(color: AppColors.textSecondary))
                : Column(
                    children: exams.take(3).map((exam) {
                      final start = exam.startTime;
                      return Padding(
                        padding: const EdgeInsets.only(bottom: 10),
                        child: Row(
                          children: [
                            Container(
                              width: 44,
                              padding: const EdgeInsets.symmetric(vertical: 6),
                              decoration: BoxDecoration(
                                color: AppColors.primary.withAlpha(20),
                                borderRadius: BorderRadius.circular(8),
                              ),
                              child: Column(
                                children: [
                                  Text(start != null ? '${start.month}月' : '-',
                                      style: const TextStyle(fontSize: 11, color: AppColors.primary)),
                                  Text(start != null ? '${start.day}' : '-',
                                      style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppColors.primary)),
                                ],
                              ),
                            ),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(exam.title,
                                      style: tt.bodyMedium?.copyWith(fontWeight: FontWeight.w600),
                                      maxLines: 1,
                                      overflow: TextOverflow.ellipsis),
                                  Text(exam.statusLabel,
                                      style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
                                ],
                              ),
                            ),
                            const Icon(Icons.chevron_right, size: 16, color: AppColors.textWeak),
                          ],
                        ),
                      );
                    }).toList(),
                  ),
          ),
          const SizedBox(height: 8),
          SizedBox(
            width: double.infinity,
            child: OutlinedButton(
              onPressed: () => context.go('/exams'),
              child: const Text('查看全部考试'),
            ),
          ),
        ],
      ),
    );
  }
}

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cs.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: cs.outlineVariant),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('近期考试',
                  style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
              Icon(Icons.calendar_month, size: 18, color: cs.primary),
            ],
          ),
          const SizedBox(height: 12),
          _ExamItem(
              month: '10月',
              day: '24',
              title: '期末考：宏观经济学',
              subtitle: '09:00 AM · 主礼堂',
              cs: cs,
              tt: tt),
          const SizedBox(height: 8),
          _ExamItem(
              month: '10月',
              day: '28',
              title: '测验：有机化学',
              subtitle: '02:00 PM · 在线',
              cs: cs,
              tt: tt),
          const SizedBox(height: 12),
          SizedBox(
            width: double.infinity,
            child: OutlinedButton(
              onPressed: () => context.go('/exams'),
              child: const Text('查看全部考试'),
            ),
          ),
        ],
      ),
    );
  }
}

// ── 快速练习入口 ───────────────────────────────────────────────────────────────
class _QuickPracticeCard extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _QuickPracticeCard({required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cs.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: cs.outlineVariant),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('快速练习',
                  style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
              GestureDetector(
                onTap: () => context.go('/practice'),
                child: Text('浏览全部',
                    style: tt.labelSmall?.copyWith(color: cs.primary)),
              ),
            ],
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              _QuickItem(
                  icon: Icons.functions,
                  label: '记忆卡片',
                  desc: '120 个术语',
                  cs: cs,
                  tt: tt),
              const SizedBox(width: 8),
              _QuickItem(
                  icon: Icons.quiz,
                  label: '模拟测试',
                  desc: '45 道题',
                  cs: cs,
                  tt: tt),
              const SizedBox(width: 8),
              _QuickItem(
                  icon: Icons.draw,
                  label: '主动回忆',
                  desc: '15 个概念',
                  cs: cs,
                  tt: tt),
            ],
          ),
        ],
      ),
    );
  }
}

class _QuickItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final String desc;
  final ColorScheme cs;
  final TextTheme tt;
  const _QuickItem(
      {required this.icon,
      required this.label,
      required this.desc,
      required this.cs,
      required this.tt});

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: GestureDetector(
        onTap: () => context.go('/practice'),
        child: Container(
          padding: const EdgeInsets.all(12),
          decoration: BoxDecoration(
            color: cs.primaryContainer.withOpacity(0.3),
            borderRadius: BorderRadius.circular(12),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(icon, size: 20, color: cs.primary),
              const SizedBox(height: 6),
              Text(label,
                  style: tt.bodySmall?.copyWith(fontWeight: FontWeight.w600),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis),
              Text(desc,
                  style: tt.bodySmall
                      ?.copyWith(color: cs.onSurfaceVariant, fontSize: 11),
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis),
            ],
          ),
        ),
      ),
    );
  }
}

// ── 近期阅读 ──────────────────────────────────────────────────────────────────
class _RecentBooksCard extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _RecentBooksCard({required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    final books = [
      ('量子力学基础', '昨天阅读'),
      ('2024 全球贸易政策', '3 天前阅读'),
      ('神经解剖学 101', '上周阅读'),
    ];

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cs.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: cs.outlineVariant),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('近期阅读',
                  style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
              GestureDetector(
                onTap: () => context.go('/library'),
                child: Text('查看全部',
                    style: tt.labelSmall?.copyWith(color: cs.primary)),
              ),
            ],
          ),
          const SizedBox(height: 12),
          ...books.map((book) => Padding(
                padding: const EdgeInsets.only(bottom: 8),
                child: InkWell(
                  borderRadius: BorderRadius.circular(8),
                  onTap: () => context.go('/library'),
                  child: Row(
                    children: [
                      Container(
                        width: 40,
                        height: 52,
                        decoration: BoxDecoration(
                          color: cs.primaryContainer,
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child:
                            Icon(Icons.auto_stories, color: cs.primary, size: 20),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(book.$1,
                                style: tt.bodyMedium
                                    ?.copyWith(fontWeight: FontWeight.w600),
                                maxLines: 1,
                                overflow: TextOverflow.ellipsis),
                            Text(book.$2,
                                style: tt.bodySmall
                                    ?.copyWith(color: cs.onSurfaceVariant)),
                          ],
                        ),
                      ),
                      Icon(Icons.bookmark_outline,
                          size: 18, color: cs.onSurfaceVariant),
                    ],
                  ),
                ),
              )),
        ],
      ),
    );
  }
}
