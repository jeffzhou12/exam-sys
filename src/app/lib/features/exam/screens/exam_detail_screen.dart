import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class ExamDetailScreen extends StatefulWidget {
  final String examId;
  const ExamDetailScreen({super.key, required this.examId});

  @override
  State<ExamDetailScreen> createState() => _ExamDetailScreenState();
}

class _ExamDetailScreenState extends State<ExamDetailScreen> {
  bool _agreedToRules = false;

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
        title: const Text('考试详情'),
        actions: [
          IconButton(icon: const Icon(Icons.notifications_outlined), onPressed: () {}),
          Padding(
            padding: const EdgeInsets.only(right: 8),
            child: CircleAvatar(
              radius: 14,
              backgroundColor: cs.primaryContainer,
              child: Icon(Icons.person, size: 16, color: cs.primary),
            ),
          ),
        ],
      ),
      body: Column(
        children: [
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // ── 考试信息卡 ─────────────────────────────────────
                  Container(
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
                          Icon(Icons.science, color: cs.primary, size: 18),
                          const SizedBox(width: 6),
                          Text('物理学院 · 专业核心课',
                              style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
                        ]),
                        const SizedBox(height: 8),
                        Text('高等量子力学期末考试',
                            style: tt.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                        const SizedBox(height: 10),
                        _InfoRow(icon: Icons.calendar_today, text: '2024年10月24日 09:00 AM', cs: cs, tt: tt),
                        const SizedBox(height: 4),
                        _InfoRow(icon: Icons.person, text: '陈明远 教授', cs: cs, tt: tt),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // ── 考试规格 ────────────────────────────────────────
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: cs.surface,
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(color: cs.outlineVariant),
                    ),
                    child: Row(
                      children: [
                        _SpecItem(icon: Icons.grade, label: '总分', value: '100分', cs: cs, tt: tt),
                        Container(width: 1, height: 40, color: cs.outlineVariant),
                        _SpecItem(icon: Icons.verified, label: '及格分', value: '60分', cs: cs, tt: tt),
                        Container(width: 1, height: 40, color: cs.outlineVariant),
                        _SpecItem(icon: Icons.timer, label: '时长', value: '120分钟', cs: cs, tt: tt),
                        Container(width: 1, height: 40, color: cs.outlineVariant),
                        _SpecItem(icon: Icons.description, label: '难度', value: '困难', cs: cs, tt: tt),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // ── 题目分布 ────────────────────────────────────────
                  Container(
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
                          Icon(Icons.pie_chart_outline, size: 16, color: cs.primary),
                          const SizedBox(width: 6),
                          Text('题目分布', style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                        ]),
                        const SizedBox(height: 12),
                        _QuestionTypeRow(type: '单选题', count: 10, score: 20, cs: cs, tt: tt),
                        const SizedBox(height: 8),
                        _QuestionTypeRow(type: '多选题', count: 5, score: 20, cs: cs, tt: tt),
                        const SizedBox(height: 8),
                        _QuestionTypeRow(type: '简答题', count: 4, score: 60, cs: cs, tt: tt),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // ── 考试说明 ────────────────────────────────────────
                  Container(
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
                          Icon(Icons.info_outline, size: 16, color: cs.primary),
                          const SizedBox(width: 6),
                          Text('考试说明', style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                        ]),
                        const SizedBox(height: 10),
                        _RuleItem(index: 1, text: '本次考试全程开启防作弊监控，请确保环境安静、光线充足。', tt: tt, cs: cs),
                        const SizedBox(height: 8),
                        _RuleItem(index: 2, text: '请在规定时间内完成所有题目，超时后系统将自动提交答案。', tt: tt, cs: cs),
                        const SizedBox(height: 8),
                        _RuleItem(index: 3, text: '考试过程中不得使用任何未经授权的辅助工具或资料。', tt: tt, cs: cs),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // ── 同意条款 ────────────────────────────────────────
                  GestureDetector(
                    onTap: () => setState(() => _agreedToRules = !_agreedToRules),
                    child: Row(
                      children: [
                        Checkbox(
                          value: _agreedToRules,
                          onChanged: (v) => setState(() => _agreedToRules = v ?? false),
                          activeColor: cs.primary,
                        ),
                        Expanded(
                          child: Text('我已阅读并知晓考前须知的所有规定',
                              style: tt.bodySmall),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),

          // ── 底部开始按钮 ─────────────────────────────────────────────
          Container(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
            color: cs.surface,
            child: SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: _agreedToRules ? () => context.go('/exams/${widget.examId}/room') : null,
                icon: const Icon(Icons.play_circle_outline, size: 20),
                label: const Text('开始考试'),
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

class _InfoRow extends StatelessWidget {
  final IconData icon;
  final String text;
  final ColorScheme cs;
  final TextTheme tt;
  const _InfoRow({required this.icon, required this.text, required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Row(children: [
      Icon(icon, size: 14, color: cs.onSurfaceVariant),
      const SizedBox(width: 6),
      Text(text, style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
    ]);
  }
}

class _SpecItem extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final ColorScheme cs;
  final TextTheme tt;
  const _SpecItem({required this.icon, required this.label, required this.value, required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Column(children: [
        Icon(icon, size: 20, color: cs.primary),
        const SizedBox(height: 4),
        Text(value, style: tt.bodySmall?.copyWith(fontWeight: FontWeight.bold)),
        Text(label, style: TextStyle(fontSize: 10, color: cs.onSurfaceVariant)),
      ]),
    );
  }
}

class _QuestionTypeRow extends StatelessWidget {
  final String type;
  final int count;
  final int score;
  final ColorScheme cs;
  final TextTheme tt;
  const _QuestionTypeRow({required this.type, required this.count, required this.score, required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Row(children: [
      Container(
        width: 8, height: 8,
        decoration: BoxDecoration(shape: BoxShape.circle, color: cs.primary),
      ),
      const SizedBox(width: 8),
      Expanded(child: Text(type, style: tt.bodySmall)),
      Text('$count 题', style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
      const SizedBox(width: 16),
      Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
        decoration: BoxDecoration(
          color: cs.primaryContainer, borderRadius: BorderRadius.circular(4)),
        child: Text('$score 分', style: TextStyle(fontSize: 11, color: cs.primary)),
      ),
    ]);
  }
}

class _RuleItem extends StatelessWidget {
  final int index;
  final String text;
  final TextTheme tt;
  final ColorScheme cs;
  const _RuleItem({required this.index, required this.text, required this.tt, required this.cs});

  @override
  Widget build(BuildContext context) {
    return Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Container(
        width: 20, height: 20,
        decoration: BoxDecoration(
          color: cs.primaryContainer, shape: BoxShape.circle),
        child: Center(
          child: Text('$index', style: TextStyle(fontSize: 11, color: cs.primary, fontWeight: FontWeight.bold)),
        ),
      ),
      const SizedBox(width: 10),
      Expanded(child: Text(text, style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant))),
    ]);
  }
}
