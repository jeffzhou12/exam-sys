import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../stores/auth_store.dart';

class ExamListScreen extends ConsumerWidget {
  const ExamListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final cs = Theme.of(context).colorScheme;
    final tt = Theme.of(context).textTheme;

    void requireLogin(VoidCallback action) {
      if (ref.read(authStoreProvider).isLoggedIn) {
        action();
      } else {
        context.go('/login?redirect=${Uri.encodeComponent('/exams')}');
      }
    }

    return Scaffold(
      backgroundColor: cs.surfaceContainerLowest,
      body: CustomScrollView(
        slivers: [
          // ── AppBar ─────────────────────────────────────────────────
          SliverAppBar(
            floating: true,
            backgroundColor: cs.surface,
            surfaceTintColor: Colors.transparent,
            title: Row(
              children: [
                CircleAvatar(
                  radius: 14,
                  backgroundColor: cs.primaryContainer,
                  child: Icon(Icons.person, size: 16, color: cs.primary),
                ),
                const SizedBox(width: 8),
                Text('考试中心',
                    style: tt.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
              ],
            ),
            actions: [
              IconButton(
                icon: const Icon(Icons.notifications_outlined),
                onPressed: () => context.go('/messages'),
              ),
            ],
          ),

          // ── 可用考试 ────────────────────────────────────────────────
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
            sliver: SliverToBoxAdapter(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('可用考试', style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                  const SizedBox(height: 4),
                  Text('找到符合你学习阶段的考试', style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
                ],
              ),
            ),
          ),

          // ── 重点考试卡片 ────────────────────────────────────────────
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            sliver: SliverToBoxAdapter(
              child: _FeaturedExamCard(cs: cs, tt: tt, requireLogin: requireLogin),
            ),
          ),

          // ── 进行中考试 ──────────────────────────────────────────────
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            sliver: SliverToBoxAdapter(
              child: _ExamCard(
                icon: Icons.history_edu,
                badge: '进行中 35%',
                badgeColor: cs.primary,
                title: '现代社会学基础',
                tags: const ['60 分钟', '30 题'],
                actionLabel: '继续',
                actionIcon: Icons.play_arrow,
                onAction: () => requireLogin(() => context.go('/exams/1/room')),
                onCardTap: () => context.go('/exams/1/detail'),
                cs: cs,
                tt: tt,
              ),
            ),
          ),

          // ── 未开始考试 ──────────────────────────────────────────────
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            sliver: SliverToBoxAdapter(
              child: _ExamCard(
                icon: Icons.calculate,
                badge: '未开始',
                badgeColor: cs.onSurfaceVariant,
                title: '应用线性代数',
                tags: const ['90 分钟', '25 题'],
                actionLabel: '开始',
                actionIcon: Icons.start,
                onAction: () => requireLogin(() => context.go('/exams/2/detail')),
                onCardTap: () => context.go('/exams/2/detail'),
                cs: cs,
                tt: tt,
              ),
            ),
          ),

          // ── AI 成绩分析 ─────────────────────────────────────────────
          SliverPadding(
            padding: const EdgeInsets.fromLTRB(16, 20, 16, 0),
            sliver: SliverToBoxAdapter(
              child: _AiAnalysisCard(cs: cs, tt: tt),
            ),
          ),

          const SliverPadding(padding: EdgeInsets.only(bottom: 24)),
        ],
      ),
    );
  }
}

// ── 重点/直播考试卡片 ─────────────────────────────────────────────────────────
class _FeaturedExamCard extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  final void Function(VoidCallback) requireLogin;
  const _FeaturedExamCard({required this.cs, required this.tt, required this.requireLogin});

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
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: const Color(0xFFDC2626),
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Container(
                      width: 6, height: 6,
                      decoration: const BoxDecoration(
                        shape: BoxShape.circle, color: Colors.white),
                    ),
                    const SizedBox(width: 4),
                    const Text('正在直播',
                        style: TextStyle(color: Colors.white, fontSize: 11, fontWeight: FontWeight.bold)),
                  ],
                ),
              ),
              const Spacer(),
              Icon(Icons.science, color: cs.onPrimary.withOpacity(0.7), size: 20),
            ],
          ),
          const SizedBox(height: 12),
          Text('高等量子力学期末考试',
              style: tt.titleMedium?.copyWith(color: cs.onPrimary, fontWeight: FontWeight.bold)),
          const SizedBox(height: 8),
          Wrap(
            spacing: 8,
            children: [
              _WhiteTag(label: '120 分钟'),
              _WhiteTag(label: '45 道题'),
              _WhiteTag(label: '高级'),
            ],
          ),
          const SizedBox(height: 16),
          SizedBox(
            width: double.infinity,
            child: FilledButton.icon(
              onPressed: () => requireLogin(() => context.go('/exams/1/detail')),
              style: FilledButton.styleFrom(
                backgroundColor: cs.onPrimary,
                foregroundColor: cs.primary,
              ),
              icon: const Icon(Icons.arrow_forward, size: 16),
              label: const Text('进入考场'),
            ),
          ),
        ],
      ),
    );
  }
}

class _WhiteTag extends StatelessWidget {
  final String label;
  const _WhiteTag({required this.label});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: Colors.white.withOpacity(0.2),
        borderRadius: BorderRadius.circular(4),
      ),
      child: Text(label, style: const TextStyle(color: Colors.white, fontSize: 11)),
    );
  }
}

// ── 普通考试卡片 ──────────────────────────────────────────────────────────────
class _ExamCard extends StatelessWidget {
  final IconData icon;
  final String badge;
  final Color badgeColor;
  final String title;
  final List<String> tags;
  final String actionLabel;
  final IconData actionIcon;
  final VoidCallback onAction;
  final VoidCallback onCardTap;
  final ColorScheme cs;
  final TextTheme tt;

  const _ExamCard({
    required this.icon,
    required this.badge,
    required this.badgeColor,
    required this.title,
    required this.tags,
    required this.actionLabel,
    required this.actionIcon,
    required this.onAction,
    required this.onCardTap,
    required this.cs,
    required this.tt,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onCardTap,
      child: Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: cs.surface,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: cs.outlineVariant),
        ),
        child: Row(
          children: [
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: cs.primaryContainer,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Icon(icon, color: cs.primary, size: 24),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(badge,
                      style: TextStyle(fontSize: 12, color: badgeColor, fontWeight: FontWeight.w500)),
                  const SizedBox(height: 2),
                  Text(title,
                      style: tt.bodyMedium?.copyWith(fontWeight: FontWeight.w600),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis),
                  const SizedBox(height: 6),
                  Wrap(
                    spacing: 6,
                    children: tags.map((t) => Container(
                      padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                      decoration: BoxDecoration(
                        color: cs.primaryContainer,
                        borderRadius: BorderRadius.circular(4),
                      ),
                      child: Text(t, style: TextStyle(fontSize: 11, color: cs.primary)),
                    )).toList(),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 12),
            FilledButton.icon(
              onPressed: onAction,
              style: FilledButton.styleFrom(
                padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                textStyle: const TextStyle(fontSize: 13),
              ),
              icon: Icon(actionIcon, size: 16),
              label: Text(actionLabel),
            ),
          ],
        ),
      ),
    );
  }
}

// ── AI 成绩分析面板 ───────────────────────────────────────────────────────────
class _AiAnalysisCard extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _AiAnalysisCard({required this.cs, required this.tt});

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
              Text('AI 成绩分析',
                  style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
            ],
          ),
          const SizedBox(height: 16),

          // 三列统计
          Row(
            children: [
              _AnalyticItem(label: '平均分', value: '84%',
                  sub: '↑+12%', subColor: const Color(0xFF16A34A), cs: cs, tt: tt),
              Container(width: 1, height: 50, color: cs.outlineVariant),
              _AnalyticItem(label: '全球排名', value: '#142',
                  sub: '前 5%', subColor: cs.primary, cs: cs, tt: tt),
              Container(width: 1, height: 50, color: cs.outlineVariant),
              _AnalyticItem(label: '准确率', value: '92%',
                  sub: '上升趋势', subColor: const Color(0xFF16A34A), cs: cs, tt: tt),
            ],
          ),
          const SizedBox(height: 12),
          Divider(color: cs.outlineVariant),
          const SizedBox(height: 8),

          // 薄弱环节
          Row(
            children: [
              Icon(Icons.lightbulb_outline, size: 16, color: cs.primary),
              const SizedBox(width: 6),
              Expanded(
                child: Text('智能建议：您的准确率在45分钟后显著下降，建议提升答题效率。',
                    style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
              ),
            ],
          ),
          const SizedBox(height: 12),
          SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              onPressed: () => context.go('/ai-analysis'),
              icon: const Icon(Icons.insights, size: 16),
              label: const Text('查看完整分析'),
            ),
          ),
        ],
      ),
    );
  }
}

class _AnalyticItem extends StatelessWidget {
  final String label;
  final String value;
  final String sub;
  final Color subColor;
  final ColorScheme cs;
  final TextTheme tt;
  const _AnalyticItem({
    required this.label, required this.value,
    required this.sub, required this.subColor,
    required this.cs, required this.tt,
  });

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Column(
        children: [
          Text(value, style: tt.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
          Text(label, style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
          Text(sub, style: TextStyle(fontSize: 11, color: subColor, fontWeight: FontWeight.w500)),
        ],
      ),
    );
  }
}
