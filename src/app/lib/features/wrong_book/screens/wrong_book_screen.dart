import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class WrongBookScreen extends StatefulWidget {
  const WrongBookScreen({super.key});

  @override
  State<WrongBookScreen> createState() => _WrongBookScreenState();
}

class _WrongBookScreenState extends State<WrongBookScreen> {
  String _selectedSubject = '全部科目';
  final List<String> _subjects = ['全部科目', '数学', '物理', '化学', '生物'];

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
        title: const Text('错题本', style: TextStyle(fontWeight: FontWeight.bold)),
        actions: [
          IconButton(icon: const Icon(Icons.filter_list), onPressed: () {}),
        ],
      ),
      body: Column(
        children: [
          // ── 科目筛选 Chips ────────────────────────────────────────
          Container(
            color: cs.surface,
            padding: const EdgeInsets.symmetric(vertical: 10, horizontal: 16),
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: _subjects.map((s) {
                  final selected = s == _selectedSubject;
                  return Padding(
                    padding: const EdgeInsets.only(right: 8),
                    child: FilterChip(
                      label: Text(s),
                      selected: selected,
                      onSelected: (_) => setState(() => _selectedSubject = s),
                      selectedColor: cs.primaryContainer,
                      checkmarkColor: cs.primary,
                      labelStyle: TextStyle(
                        color: selected ? cs.primary : cs.onSurface,
                        fontWeight: selected ? FontWeight.w600 : FontWeight.normal,
                      ),
                    ),
                  );
                }).toList(),
              ),
            ),
          ),

          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // ── 统计行 ──────────────────────────────────────────
                  Row(children: [
                    Expanded(
                      child: _StatChip(
                        icon: Icons.error_outline,
                        label: '待攻克错题',
                        count: '24',
                        iconColor: const Color(0xFFDC2626),
                        cs: cs, tt: tt,
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: _StatChip(
                        icon: Icons.auto_stories,
                        label: '已复习掌握',
                        count: '12',
                        iconColor: const Color(0xFF16A34A),
                        cs: cs, tt: tt,
                      ),
                    ),
                  ]),
                  const SizedBox(height: 20),

                  Text('最近错误', style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                  const SizedBox(height: 12),

                  // 错题卡片列表
                  _WrongQuestionCard(
                    subject: '数学',
                    examTitle: '模拟考试 #4',
                    date: '2023-10-24',
                    question: '设随机变量 X~N(0,1)，则 P(X>1.96) 的近似值为多少？请给出详细推导步骤。',
                    wrongCount: 3,
                    isHighPriority: false,
                    cs: cs, tt: tt,
                  ),
                  const SizedBox(height: 12),
                  _WrongQuestionCard(
                    subject: '物理',
                    examTitle: '每日练习',
                    date: '2023-10-22',
                    question: '（见图）如图所示电路中，已知 R₁=10Ω，R₂=20Ω，电源电动势 ε=12V，内阻 r=2Ω，求总电流和路端电压。',
                    wrongCount: null,
                    isHighPriority: true,
                    cs: cs, tt: tt,
                  ),
                  const SizedBox(height: 12),
                  _WrongQuestionCard(
                    subject: '化学',
                    examTitle: '模拟考 B 卷',
                    date: '2023-10-20',
                    question: '下列有关有机物的说法中，正确的是：A. 乙醇与乙酸互为同分异构体 B. 苯不能发生加成反应 C. 葡萄糖的分子式为 C₆H₁₂O₆ D. 蛋白质水解的最终产物是氨基酸',
                    wrongCount: 1,
                    isHighPriority: false,
                    cs: cs, tt: tt,
                  ),
                ],
              ),
            ),
          ),

          // ── 底部再次练习按钮 ─────────────────────────────────────────
          Container(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
            color: cs.surface,
            child: SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: () => context.push('/practice'),
                icon: const Icon(Icons.play_circle_outline, size: 20),
                label: const Text('再次练习（24 道错题）'),
                style: FilledButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  textStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _StatChip extends StatelessWidget {
  final IconData icon;
  final String label;
  final String count;
  final Color iconColor;
  final ColorScheme cs;
  final TextTheme tt;
  const _StatChip({required this.icon, required this.label, required this.count,
      required this.iconColor, required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: cs.surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: cs.outlineVariant),
      ),
      child: Row(children: [
        Icon(icon, color: iconColor, size: 22),
        const SizedBox(width: 10),
        Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(count,
              style: tt.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
          Text(label,
              style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
        ]),
      ]),
    );
  }
}

class _WrongQuestionCard extends StatelessWidget {
  final String subject;
  final String examTitle;
  final String date;
  final String question;
  final int? wrongCount;
  final bool isHighPriority;
  final ColorScheme cs;
  final TextTheme tt;

  const _WrongQuestionCard({
    required this.subject, required this.examTitle, required this.date,
    required this.question, required this.wrongCount, required this.isHighPriority,
    required this.cs, required this.tt,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: cs.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: cs.outlineVariant),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // 顶部标签行
          Row(children: [
            _SubjectTag(label: subject, cs: cs),
            const SizedBox(width: 8),
            _SubjectTag(label: examTitle, cs: cs),
            const Spacer(),
            Text(date, style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant, fontSize: 11)),
          ]),
          const SizedBox(height: 10),

          // 题目内容
          Text(question,
              style: tt.bodySmall?.copyWith(height: 1.5),
              maxLines: 3,
              overflow: TextOverflow.ellipsis),
          const SizedBox(height: 10),

          // 底部
          Row(children: [
            if (isHighPriority) ...[
              Icon(Icons.priority_high, size: 14, color: const Color(0xFFDC2626)),
              const SizedBox(width: 4),
              Text('高优先级',
                  style: const TextStyle(fontSize: 11, color: Color(0xFFDC2626), fontWeight: FontWeight.w500)),
            ] else if (wrongCount != null) ...[
              Icon(Icons.history_edu, size: 14, color: cs.onSurfaceVariant),
              const SizedBox(width: 4),
              Text('错误 $wrongCount 次',
                  style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant, fontSize: 11)),
            ],
            const Spacer(),
            GestureDetector(
              onTap: () => context.push('/practice'),
              child: Row(mainAxisSize: MainAxisSize.min, children: [
                Text('重做此题', style: tt.labelSmall?.copyWith(color: cs.primary)),
                Icon(Icons.chevron_right, size: 16, color: cs.primary),
              ]),
            ),
          ]),
        ],
      ),
    );
  }
}

class _SubjectTag extends StatelessWidget {
  final String label;
  final ColorScheme cs;
  const _SubjectTag({required this.label, required this.cs});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 2),
      decoration: BoxDecoration(
        color: cs.primaryContainer, borderRadius: BorderRadius.circular(4)),
      child: Text(label, style: TextStyle(fontSize: 11, color: cs.primary, fontWeight: FontWeight.w500)),
    );
  }
}
