import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../api/api.dart';
import '../../../api/models/exam_models.dart';
import '../../../stores/auth_store.dart';
import '../../../theme/app_theme.dart';

class ExamDetailScreen extends ConsumerStatefulWidget {
  final String examId;
  const ExamDetailScreen({super.key, required this.examId});

  @override
  ConsumerState<ExamDetailScreen> createState() => _ExamDetailScreenState();
}

class _ExamDetailScreenState extends ConsumerState<ExamDetailScreen> {
  bool _agreedToRules = false;
  ExamPaperDetail? _detail;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      setState(() { _loading = true; _error = null; });
      final detail = await examsApi.getExamDetail(widget.examId);
      if (mounted) setState(() { _detail = detail; _loading = false; });
    } catch (e) {
      if (mounted) setState(() { _error = e.toString().replaceAll('Exception: ', ''); _loading = false; });
    }
  }

  String _formatDateTime(DateTime? dt) {
    if (dt == null) return '-';
    return '${dt.year}骞?{dt.month}鏈?{dt.day}鏃?${dt.hour.toString().padLeft(2,'0')}:${dt.minute.toString().padLeft(2,'0')}';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.bgPage,
      appBar: AppBar(
        leading: IconButton(icon: const Icon(Icons.arrow_back), onPressed: () => context.pop()),
        title: const Text('鑰冭瘯璇︽儏'),
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      const Icon(Icons.error_outline, size: 48, color: AppColors.error),
                      const SizedBox(height: 12),
                      Text(_error!, textAlign: TextAlign.center,
                          style: const TextStyle(color: AppColors.textSecondary)),
                      const SizedBox(height: 16),
                      ElevatedButton(onPressed: _load, child: const Text('閲嶆柊鍔犺浇')),
                    ],
                  ),
                )
              : _buildContent(_detail!),
    );
  }

  Widget _buildContent(ExamPaperDetail d) {
    return Column(
      children: [
        Expanded(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Info card
                _Card(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(children: [
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                          decoration: BoxDecoration(
                            color: AppColors.info.withAlpha(26),
                            borderRadius: BorderRadius.circular(4),
                          ),
                          child: Text(d.statusLabel,
                              style: const TextStyle(color: AppColors.info, fontSize: 12)),
                        ),
                        const Spacer(),
                        Text('${d.totalScore} 鍒?,
                            style: const TextStyle(fontSize: 14, color: AppColors.textWeak)),
                      ]),
                      const SizedBox(height: 10),
                      Text(d.title,
                          style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold, color: AppColors.textMain)),
                      if (d.description != null && d.description!.isNotEmpty) ...[
                        const SizedBox(height: 8),
                        Text(d.description!,
                            style: const TextStyle(color: AppColors.textSecondary, fontSize: 14)),
                      ],
                      const SizedBox(height: 12),
                      _InfoRow(icon: Icons.timer_outlined, label: '鑰冭瘯鏃堕暱', value: '${d.durationMinutes} 鍒嗛挓'),
                      const SizedBox(height: 6),
                      _InfoRow(icon: Icons.quiz_outlined, label: '棰樼洰鏁伴噺', value: '${d.questionCount} 棰?),
                      const SizedBox(height: 6),
                      _InfoRow(icon: Icons.calendar_today_outlined, label: '寮€濮嬫椂闂?, value: _formatDateTime(d.startTime)),
                      const SizedBox(height: 6),
                      _InfoRow(icon: Icons.event_busy_outlined, label: '缁撴潫鏃堕棿', value: _formatDateTime(d.endTime)),
                    ],
                  ),
                ),
                const SizedBox(height: 16),
                // Rules card
                _Card(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Row(children: [
                        Icon(Icons.info_outline, size: 16, color: AppColors.primary),
                        SizedBox(width: 6),
                        Text('鑰冭瘯椤荤煡', style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold, color: AppColors.textMain)),
                      ]),
                      const SizedBox(height: 12),
                      _RuleItem(index: 1, text: '璇峰湪瑙勫畾鏃堕棿鍐呭畬鎴愭墍鏈夐鐩紝瓒呮椂鍚庣郴缁熷皢鑷姩鎻愪氦绛旀銆?),
                      const SizedBox(height: 8),
                      _RuleItem(index: 2, text: '鑰冭瘯杩囩▼涓笉寰椾娇鐢ㄤ换浣曟湭缁忔巿鏉冪殑杈呭姪宸ュ叿鎴栬祫鏂欍€?),
                      const SizedBox(height: 8),
                      _RuleItem(index: 3, text: '鎻愪氦鍚庝笉鍙慨鏀圭瓟妗堬紝璇蜂粩缁嗘鏌ュ悗鍐嶆彁浜ゃ€?),
                    ],
                  ),
                ),
                const SizedBox(height: 16),
                // Agree checkbox
                GestureDetector(
                  onTap: () => setState(() => _agreedToRules = !_agreedToRules),
                  child: Row(
                    children: [
                      Checkbox(
                        value: _agreedToRules,
                        onChanged: (v) => setState(() => _agreedToRules = v ?? false),
                        activeColor: AppColors.primary,
                      ),
                      const Expanded(
                        child: Text('鎴戝凡闃呰骞剁煡鏅撹€冨墠椤荤煡鐨勬墍鏈夎瀹?,
                            style: TextStyle(color: AppColors.textSecondary, fontSize: 14)),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
        Container(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
          color: AppColors.bgCard,
          child: SizedBox(
            width: double.infinity,
            child: ElevatedButton.icon(
              onPressed: _agreedToRules && (d.status == 1 || d.status == 2)
                  ? () {
                      if (ref.read(authStoreProvider).isLoggedIn) {
                        context.go('/exams/${widget.examId}/room');
                      } else {
                        context.go('/login?redirect=${Uri.encodeComponent('/exams/${widget.examId}/detail')}');
                      }
                    }
                  : null,
              icon: const Icon(Icons.play_circle_outline, size: 20),
              label: Text(d.status == 3 ? '鑰冭瘯宸茬粨鏉? : '寮€濮嬭€冭瘯'),
              style: ElevatedButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 14)),
            ),
          ),
        ),
      ],
    );
  }
}

class _Card extends StatelessWidget {
  final Widget child;
  const _Card({required this.child});

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: AppColors.bgCard,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: AppColors.borderWeak),
      ),
      child: child,
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
    return Row(children: [
      Icon(icon, size: 14, color: AppColors.textWeak),
      const SizedBox(width: 6),
      Text('$label锛?, style: const TextStyle(color: AppColors.textSecondary, fontSize: 13)),
      Text(value, style: const TextStyle(color: AppColors.textMain, fontSize: 13, fontWeight: FontWeight.w500)),
    ]);
  }
}

class _RuleItem extends StatelessWidget {
  final int index;
  final String text;
  const _RuleItem({required this.index, required this.text});

  @override
  Widget build(BuildContext context) {
    return Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
      Container(
        width: 20, height: 20,
        decoration: const BoxDecoration(color: AppColors.primary, shape: BoxShape.circle),
        child: Center(
          child: Text('$index', style: const TextStyle(fontSize: 11, color: Colors.white, fontWeight: FontWeight.bold)),
        ),
      ),
      const SizedBox(width: 10),
      Expanded(child: Text(text, style: const TextStyle(color: AppColors.textSecondary, fontSize: 13))),
    ]);
  }
}
