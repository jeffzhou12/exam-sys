import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../api/api.dart';
import '../../../api/models/exam_models.dart';
import '../../../stores/auth_store.dart';
import '../../../theme/app_theme.dart';

final _examsProvider = FutureProvider<PagedResult<ExamPaper>>((ref) async {
  return examsApi.getExams(pageSize: 20);
});

class ExamListScreen extends ConsumerStatefulWidget {
  const ExamListScreen({super.key});

  @override
  ConsumerState<ExamListScreen> createState() => _ExamListScreenState();
}

class _ExamListScreenState extends ConsumerState<ExamListScreen> {
  int _filterStatus = -1;

  @override
  Widget build(BuildContext context) {
    final examsAsync = ref.watch(_examsProvider);
    final isLoggedIn = ref.watch(authStoreProvider).isLoggedIn;

    return Scaffold(
      backgroundColor: AppColors.bgPage,
      appBar: AppBar(
        title: const Text('考试中心'),
        actions: [
          IconButton(
            icon: const Icon(Icons.notifications_outlined),
            onPressed: isLoggedIn ? () => context.go('/messages') : null,
          ),
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () => ref.invalidate(_examsProvider),
          ),
        ],
      ),
      body: Column(
        children: [
          Container(
            color: AppColors.bgCard,
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
              child: Row(
                children: [
                  _StatusChip(label: '全部', value: -1, selected: _filterStatus == -1,
                      onTap: () => setState(() => _filterStatus = -1)),
                  const SizedBox(width: 8),
                  _StatusChip(label: '报名中', value: 1, selected: _filterStatus == 1,
                      onTap: () => setState(() => _filterStatus = 1)),
                  const SizedBox(width: 8),
                  _StatusChip(label: '进行中', value: 2, selected: _filterStatus == 2,
                      onTap: () => setState(() => _filterStatus = 2)),
                  const SizedBox(width: 8),
                  _StatusChip(label: '已结束', value: 3, selected: _filterStatus == 3,
                      onTap: () => setState(() => _filterStatus = 3)),
                ],
              ),
            ),
          ),
          const Divider(height: 1),
          Expanded(
            child: examsAsync.when(
              loading: () => const Center(child: CircularProgressIndicator()),
              error: (e, _) => _ErrorWidget(
                message: e.toString().replaceAll('Exception: ', ''),
                onRetry: () => ref.invalidate(_examsProvider),
              ),
              data: (result) {
                final items = _filterStatus == -1
                    ? result.items
                    : result.items.where((e) => e.status == _filterStatus).toList();
                if (items.isEmpty) {
                  return const Center(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        Icon(Icons.inbox_outlined, size: 48, color: AppColors.textWeak),
                        SizedBox(height: 16),
                        Text('暂无考试', style: TextStyle(color: AppColors.textSecondary)),
                      ],
                    ),
                  );
                }
                return RefreshIndicator(
                  onRefresh: () async => ref.invalidate(_examsProvider),
                  child: ListView.separated(
                    padding: const EdgeInsets.all(16),
                    itemCount: items.length,
                    separatorBuilder: (_, __) => const SizedBox(height: 12),
                    itemBuilder: (context, idx) {
                      final exam = items[idx];
                      return _ExamCard(
                        exam: exam,
                        onTap: () => context.push('/exams/${exam.id}/detail'),
                        onEnter: () {
                          if (!isLoggedIn) {
                            context.go('/login?redirect=/exams/${exam.id}/detail');
                            return;
                          }
                          context.push('/exams/${exam.id}/detail');
                        },
                      );
                    },
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _StatusChip extends StatelessWidget {
  final String label;
  final int value;
  final bool selected;
  final VoidCallback onTap;
  const _StatusChip({required this.label, required this.value, required this.selected, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
        decoration: BoxDecoration(
          color: selected ? AppColors.primary : AppColors.bgPage,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: selected ? AppColors.primary : AppColors.borderStrong),
        ),
        child: Text(
          label,
          style: TextStyle(
            color: selected ? Colors.white : AppColors.textSecondary,
            fontSize: 13,
            fontWeight: selected ? FontWeight.w600 : FontWeight.normal,
          ),
        ),
      ),
    );
  }
}

class _ExamCard extends StatelessWidget {
  final ExamPaper exam;
  final VoidCallback onTap;
  final VoidCallback onEnter;
  const _ExamCard({required this.exam, required this.onTap, required this.onEnter});

  Color get _statusColor => switch (exam.status) {
        1 => AppColors.info,
        2 => AppColors.success,
        3 => AppColors.textWeak,
        4 => AppColors.error,
        _ => AppColors.textWeak,
      };

  bool get _canEnter => exam.status == 1 || exam.status == 2;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
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
              children: [
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: _statusColor.withAlpha(26),
                    borderRadius: BorderRadius.circular(4),
                  ),
                  child: Text(exam.statusLabel,
                      style: TextStyle(color: _statusColor, fontSize: 12, fontWeight: FontWeight.w500)),
                ),
                const Spacer(),
                Text('${exam.totalScore} 分',
                    style: const TextStyle(color: AppColors.textWeak, fontSize: 12)),
              ],
            ),
            const SizedBox(height: 10),
            Text(exam.title,
                style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600, color: AppColors.textMain),
                maxLines: 2,
                overflow: TextOverflow.ellipsis),
            if (exam.description != null && exam.description!.isNotEmpty) ...[
              const SizedBox(height: 6),
              Text(exam.description!,
                  style: const TextStyle(color: AppColors.textSecondary, fontSize: 13),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis),
            ],
            const SizedBox(height: 12),
            Wrap(
              spacing: 16,
              children: [
                _InfoTag(icon: Icons.timer_outlined, label: '${exam.durationMinutes} 分钟'),
                _InfoTag(icon: Icons.quiz_outlined, label: '${exam.questionCount} 题'),
                if (exam.startTime != null)
                  _InfoTag(icon: Icons.calendar_today_outlined,
                      label: '${exam.startTime!.month}/${exam.startTime!.day}'),
              ],
            ),
            if (_canEnter) ...[
              const SizedBox(height: 12),
              SizedBox(
                width: double.infinity,
                child: ElevatedButton(
                  onPressed: onEnter,
                  style: ElevatedButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 10)),
                  child: Text(exam.status == 2 ? '进入考场' : '查看详情'),
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _InfoTag extends StatelessWidget {
  final IconData icon;
  final String label;
  const _InfoTag({required this.icon, required this.label});

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 13, color: AppColors.textWeak),
        const SizedBox(width: 3),
        Text(label, style: const TextStyle(color: AppColors.textSecondary, fontSize: 12)),
      ],
    );
  }
}

class _ErrorWidget extends StatelessWidget {
  final String message;
  final VoidCallback onRetry;
  const _ErrorWidget({required this.message, required this.onRetry});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.error_outline, size: 48, color: AppColors.error),
            const SizedBox(height: 16),
            Text(message, textAlign: TextAlign.center,
                style: const TextStyle(color: AppColors.textSecondary)),
            const SizedBox(height: 16),
            ElevatedButton(onPressed: onRetry, child: const Text('重新加载')),
          ],
        ),
      ),
    );
  }
}