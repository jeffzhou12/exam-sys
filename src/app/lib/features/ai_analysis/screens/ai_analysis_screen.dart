import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class AIAnalysisScreen extends StatelessWidget {
  const AIAnalysisScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final tt = Theme.of(context).textTheme;

    return Scaffold(
      backgroundColor: cs.surfaceContainerLowest,
      appBar: AppBar(
        backgroundColor: cs.surface,
        surfaceTintColor: Colors.transparent,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
        title: Row(children: [
          CircleAvatar(radius: 14, backgroundColor: cs.primaryContainer,
              child: Icon(Icons.person, size: 16, color: cs.primary)),
          const SizedBox(width: 8),
          const Text('AI 学习分析', style: TextStyle(fontWeight: FontWeight.bold)),
        ]),
        actions: [
          IconButton(icon: const Icon(Icons.notifications_outlined), onPressed: () {}),
        ],
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── 表现卡片 ────────────────────────────────────────────
            _PerformanceCard(cs: cs, tt: tt),
            const SizedBox(height: 16),

            // ── AI 学习洞察 ─────────────────────────────────────────
            _InsightCard(cs: cs, tt: tt),
            const SizedBox(height: 16),

            // ── 各主题熟练度 ────────────────────────────────────────
            _ProficiencyCard(cs: cs, tt: tt),
            const SizedBox(height: 16),

            // ── 推荐操作 ────────────────────────────────────────────
            _RecommendCard(cs: cs, tt: tt),
            const SizedBox(height: 16),

            // 查看完整答案按钮
            SizedBox(
              width: double.infinity,
              child: FilledButton(
                onPressed: () {},
                child: const Text('查看完整答案'),
              ),
            ),
            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }
}

// ── 表现卡片 ──────────────────────────────────────────────────────────────────
class _PerformanceCard extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _PerformanceCard({required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        gradient: LinearGradient(
          colors: [cs.primary, cs.primaryContainer],
          begin: Alignment.topLeft, end: Alignment.bottomRight,
        ),
        borderRadius: BorderRadius.circular(16),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('表现卓越！',
              style: tt.headlineSmall?.copyWith(color: cs.onPrimary, fontWeight: FontWeight.bold)),
          const SizedBox(height: 4),
          Text('击败了 88% 的参与者',
              style: tt.bodySmall?.copyWith(color: cs.onPrimary.withOpacity(0.85))),
          const SizedBox(height: 20),
          Row(
            children: [
              // 环形进度
              SizedBox(
                width: 90, height: 90,
                child: Stack(children: [
                  CircularProgressIndicator(
                    value: 0.85,
                    strokeWidth: 8,
                    backgroundColor: cs.onPrimary.withOpacity(0.2),
                    valueColor: AlwaysStoppedAnimation<Color>(cs.onPrimary),
                    strokeCap: StrokeCap.round,
                  ),
                  Center(child: Text('85%',
                      style: tt.titleMedium?.copyWith(color: cs.onPrimary, fontWeight: FontWeight.bold))),
                ]),
              ),
              const SizedBox(width: 20),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _MetaRow(icon: Icons.star_outline, label: '前 10%',
                        sub: '较上周 +12%', cs: cs),
                    const SizedBox(height: 10),
                    _MetaRow(icon: Icons.bolt, label: '效率：42 分钟',
                        sub: '比平均快 15 分钟', cs: cs),
                    const SizedBox(height: 10),
                    _MetaRow(icon: Icons.check_circle_outline, label: '准确率：92%',
                        sub: '上升趋势', cs: cs),
                  ],
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _MetaRow extends StatelessWidget {
  final IconData icon;
  final String label;
  final String sub;
  final ColorScheme cs;
  const _MetaRow({required this.icon, required this.label, required this.sub, required this.cs});

  @override
  Widget build(BuildContext context) {
    return Row(children: [
      Icon(icon, size: 15, color: cs.onPrimary),
      const SizedBox(width: 6),
      Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(label, style: TextStyle(fontSize: 12, color: cs.onPrimary, fontWeight: FontWeight.w500)),
        Text(sub, style: TextStyle(fontSize: 10, color: cs.onPrimary.withOpacity(0.75))),
      ]),
    ]);
  }
}

// ── AI 学习洞察 ───────────────────────────────────────────────────────────────
class _InsightCard extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _InsightCard({required this.cs, required this.tt});

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
          Row(children: [
            Icon(Icons.auto_awesome, size: 18, color: cs.primary),
            const SizedBox(width: 6),
            Text('AI 学习洞察', style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
          ]),
          const SizedBox(height: 14),

          // 知识点掌握
          Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Icon(Icons.psychology_outlined, size: 18, color: cs.primary),
            const SizedBox(width: 8),
            Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text('知识点掌握', style: tt.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
              const SizedBox(height: 4),
              Text('薄弱项：递归算法、图论遍历。建议本周集中练习 3 小时。',
                  style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant, height: 1.5)),
            ])),
          ]),
          const SizedBox(height: 14),

          // AI 专家提示
          Container(
            padding: const EdgeInsets.all(12),
            decoration: BoxDecoration(
              color: cs.primaryContainer.withOpacity(0.3),
              borderRadius: BorderRadius.circular(10),
              border: Border.all(color: cs.primaryContainer),
            ),
            child: Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Container(
                width: 3, height: 50,
                decoration: BoxDecoration(color: cs.primary, borderRadius: BorderRadius.circular(2)),
              ),
              const SizedBox(width: 10),
              Expanded(child: Text(
                '"您在处理复杂问题时，分步思考的能力很强。建议将这一方式应用到薄弱环节，系统性地突破瓶颈。"',
                style: tt.bodySmall?.copyWith(fontStyle: FontStyle.italic, height: 1.6),
              )),
            ]),
          ),
        ],
      ),
    );
  }
}

// ── 各主题熟练度 ──────────────────────────────────────────────────────────────
class _ProficiencyCard extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _ProficiencyCard({required this.cs, required this.tt});

  static const _items = [
    ('动态规划', 0.94),
    ('数组操作', 0.88),
    ('图论', 0.52),
    ('递归', 0.71),
  ];

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
          Text('各主题熟练度', style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 14),
          ..._items.map((item) => Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Row(mainAxisAlignment: MainAxisAlignment.spaceBetween, children: [
                Text(item.$1, style: tt.bodySmall?.copyWith(fontWeight: FontWeight.w500)),
                Text('${(item.$2 * 100).round()}%',
                    style: tt.bodySmall?.copyWith(
                      color: item.$2 < 0.6 ? const Color(0xFFDC2626) : cs.primary,
                      fontWeight: FontWeight.bold,
                    )),
              ]),
              const SizedBox(height: 4),
              ClipRRect(
                borderRadius: BorderRadius.circular(4),
                child: LinearProgressIndicator(
                  value: item.$2,
                  backgroundColor: cs.primaryContainer,
                  valueColor: AlwaysStoppedAnimation<Color>(
                    item.$2 < 0.6 ? const Color(0xFFDC2626) : cs.primary),
                  minHeight: 7,
                ),
              ),
            ]),
          )),
        ],
      ),
    );
  }
}

// ── 推荐操作 ──────────────────────────────────────────────────────────────────
class _RecommendCard extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _RecommendCard({required this.cs, required this.tt});

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
          Text('推荐操作', style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 12),
          _ActionItem(icon: Icons.menu_book, label: '复习第 3-4 章节',
              onTap: () => context.push('/library'), cs: cs, tt: tt),
          const Divider(height: 1),
          _ActionItem(icon: Icons.edit_note, label: '专项练习：递归 & 图论',
              onTap: () => context.push('/practice'), cs: cs, tt: tt),
        ],
      ),
    );
  }
}

class _ActionItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final VoidCallback onTap;
  final ColorScheme cs;
  final TextTheme tt;
  const _ActionItem({required this.icon, required this.label, required this.onTap,
      required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return InkWell(
      borderRadius: BorderRadius.circular(8),
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 12),
        child: Row(children: [
          Icon(icon, size: 18, color: cs.primary),
          const SizedBox(width: 12),
          Expanded(child: Text(label, style: tt.bodyMedium?.copyWith(fontWeight: FontWeight.w500))),
          Icon(Icons.chevron_right, size: 18, color: cs.onSurfaceVariant),
        ]),
      ),
    );
  }
}
