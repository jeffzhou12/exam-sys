import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../api/api.dart';
import '../../../api/models/question_models.dart';
import '../../../theme/app_theme.dart';

final _historyProvider = FutureProvider<List<PracticeSession>>((ref) async {
  return practiceApi.getHistory();
});

class WrongBookScreen extends ConsumerWidget {
  const WrongBookScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(_historyProvider);

    return Scaffold(
      backgroundColor: AppColors.bgPage,
      appBar: AppBar(
        leading: IconButton(icon: const Icon(Icons.arrow_back), onPressed: () => context.pop()),
        title: const Text('练习记录'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: () => ref.invalidate(_historyProvider)),
        ],
      ),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.error_outline, size: 48, color: AppColors.error),
              const SizedBox(height: 12),
              Text(e.toString().replaceAll('Exception: ', ''),
                  textAlign: TextAlign.center, style: const TextStyle(color: AppColors.textSecondary)),
              const SizedBox(height: 16),
              ElevatedButton(onPressed: () => ref.invalidate(_historyProvider), child: const Text('重新加载')),
            ],
          ),
        ),
        data: (sessions) => sessions.isEmpty
            ? Center(
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Icon(Icons.history_edu_outlined, size: 60, color: AppColors.textWeak),
                    const SizedBox(height: 16),
                    const Text('暂无练习记录', style: TextStyle(color: AppColors.textSecondary, fontSize: 16)),
                    const SizedBox(height: 24),
                    ElevatedButton.icon(
                      onPressed: () => context.go('/practice'),
                      icon: const Icon(Icons.play_arrow),
                      label: const Text('开始刷题'),
                    ),
                  ],
                ),
              )
            : RefreshIndicator(
                onRefresh: () async => ref.invalidate(_historyProvider),
                child: ListView.separated(
                  padding: const EdgeInsets.all(16),
                  itemCount: sessions.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 10),
                  itemBuilder: (context, i) => _SessionCard(session: sessions[i]),
                ),
              ),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/practice'),
        backgroundColor: AppColors.primary,
        foregroundColor: Colors.white,
        icon: const Icon(Icons.add),
        label: const Text('新练习'),
      ),
    );
  }
}

class _SessionCard extends StatelessWidget {
  final PracticeSession session;
  const _SessionCard({required this.session});

  @override
  Widget build(BuildContext context) {
    final pct = session.maxScore > 0
        ? (session.totalScore / session.maxScore * 100).round()
        : 0;
    final color = pct >= 80 ? AppColors.success : pct >= 60 ? AppColors.warning : AppColors.error;

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.bgCard,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.borderWeak),
      ),
      child: Row(
        children: [
          Container(
            width: 56, height: 56,
            decoration: BoxDecoration(
              color: color.withAlpha(26),
              shape: BoxShape.circle,
            ),
            child: Center(
              child: Text('$pct%',
                  style: TextStyle(color: color, fontSize: 14, fontWeight: FontWeight.bold)),
            ),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('练习 ${session.count} 题',
                    style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600, color: AppColors.textMain)),
                const SizedBox(height: 4),
                Text('得分 ${session.totalScore} / ${session.maxScore}  · 答对 ${session.correctCount} 题',
                    style: const TextStyle(color: AppColors.textSecondary, fontSize: 13)),
                const SizedBox(height: 4),
                Text(_formatDate(session.createdAt),
                    style: const TextStyle(color: AppColors.textWeak, fontSize: 12)),
              ],
            ),
          ),
          const Icon(Icons.chevron_right, color: AppColors.textWeak),
        ],
      ),
    );
  }

  String _formatDate(DateTime dt) {
    return '${dt.year}/${dt.month.toString().padLeft(2,'0')}/${dt.day.toString().padLeft(2,'0')} '
        '${dt.hour.toString().padLeft(2,'0')}:${dt.minute.toString().padLeft(2,'0')}';
  }
}