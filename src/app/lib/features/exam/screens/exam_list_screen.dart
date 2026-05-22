import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../api/api.dart';
import '../../../api/models/exam_models.dart';

final _examsProvider = FutureProvider<List<ExamPaper>>((ref) async {
  return examsApi.getMyExams();
});

class ExamListScreen extends ConsumerWidget {
  const ExamListScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final examsAsync = ref.watch(_examsProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('我的考试')),
      body: examsAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('加载失败：$e')),
        data: (exams) => RefreshIndicator(
          onRefresh: () => ref.refresh(_examsProvider.future),
          child: exams.isEmpty
              ? const Center(child: Text('暂无考试'))
              : ListView.builder(
                  padding: const EdgeInsets.all(16),
                  itemCount: exams.length,
                  itemBuilder: (context, index) =>
                      _ExamCard(exam: exams[index]),
                ),
        ),
      ),
    );
  }
}

class _ExamCard extends StatelessWidget {
  final ExamPaper exam;
  const _ExamCard({required this.exam});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final statusColor = _statusColor(exam.status, cs);

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      child: ListTile(
        contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        title: Text(exam.title,
            style: const TextStyle(fontWeight: FontWeight.w600)),
        subtitle: Text('${exam.duration} 分钟  ·  ${exam.totalScore} 分'),
        trailing: Chip(
          label: Text(_statusText(exam.status),
              style: TextStyle(color: statusColor, fontSize: 12)),
          backgroundColor: statusColor.withValues(alpha: 0.1),
        ),
        onTap: exam.status == 'ongoing'
            ? () => context.push('/exams/${exam.id}/room')
            : null,
      ),
    );
  }

  String _statusText(String status) => switch (status) {
        'pending' => '未开始',
        'ongoing' => '进行中',
        'submitted' => '已提交',
        'graded' => '已批改',
        _ => status,
      };

  Color _statusColor(String status, ColorScheme cs) => switch (status) {
        'ongoing' => const Color(0xFF059669),
        'submitted' => const Color(0xFFD97706),
        'graded' => cs.primary,
        _ => cs.outline,
      };
}
