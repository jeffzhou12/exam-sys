import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../api/api.dart';
import '../../../api/models/exam_models.dart';
import '../../../stores/auth_store.dart';
import '../../../theme/app_theme.dart';

class ExamRoomScreen extends ConsumerStatefulWidget {
  final String examId;
  const ExamRoomScreen({super.key, required this.examId});

  @override
  ConsumerState<ExamRoomScreen> createState() => _ExamRoomScreenState();
}

class _ExamRoomScreenState extends ConsumerState<ExamRoomScreen> {
  ExamPaperDetail? _exam;
  bool _loading = true;
  String? _error;

  int _currentIndex = 0;
  final Map<String, dynamic> _answers = {}; // questionId -> answer (String or List<String>)
  Timer? _timer;
  int _remainingSeconds = 0;
  bool _submitting = false;
  bool _submitted = false;

  @override
  void initState() {
    super.initState();
    _loadExam();
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  Future<void> _loadExam() async {
    try {
      final exam = await examsApi.getExamDetail(widget.examId);
      if (mounted) {
        setState(() {
          _exam = exam;
          _loading = false;
          _remainingSeconds = exam.durationMinutes * 60;
        });
        _startTimer();
      }
    } catch (e) {
      if (mounted) {
        setState(() {
          _loading = false;
          _error = e.toString().replaceAll('Exception: ', '');
        });
      }
    }
  }

  void _startTimer() {
    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (_remainingSeconds <= 0) {
        _timer?.cancel();
        _submitAnswers(autoSubmit: true);
      } else {
        setState(() => _remainingSeconds--);
      }
    });
  }

  String get _timerText {
    final h = _remainingSeconds ~/ 3600;
    final m = (_remainingSeconds % 3600) ~/ 60;
    final s = _remainingSeconds % 60;
    if (h > 0) return '${h.toString().padLeft(2, '0')}:${m.toString().padLeft(2, '0')}:${s.toString().padLeft(2, '0')}';
    return '${m.toString().padLeft(2, '0')}:${s.toString().padLeft(2, '0')}';
  }

  Color get _timerColor {
    if (_remainingSeconds <= 300) return AppColors.error;
    if (_remainingSeconds <= 600) return AppColors.warning;
    return AppColors.success;
  }

  Future<void> _submitAnswers({bool autoSubmit = false}) async {
    if (_submitting || _submitted) return;
    if (!autoSubmit) {
      final ok = await showDialog<bool>(
        context: context,
        builder: (ctx) => AlertDialog(
          title: const Text('确认交卷'),
          content: Text('已作答 ${_answers.length} / ${_exam?.questions.length ?? 0} 题，交卷后不可修改。'),
          actions: [
            TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('继续答题')),
            ElevatedButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('确认交卷')),
          ],
        ),
      );
      if (ok != true) return;
    }

    _timer?.cancel();
    final authState = ref.read(authStoreProvider);
    final studentId = authState.userId;
    if (studentId == null) {
      _showError('无法获取用户信息，请重新登录');
      return;
    }

    setState(() => _submitting = true);
    try {
      final answerItems = _answers.entries.map((e) {
        String content;
        if (e.value is List) {
          content = (e.value as List).join(',');
        } else {
          content = e.value.toString();
        }
        return ExamAnswerItem(questionId: e.key, content: content);
      }).toList();

      await examsApi.submitAnswers(
        examId: widget.examId,
        studentId: studentId,
        answers: answerItems,
      );

      if (mounted) {
        setState(() { _submitted = true; _submitting = false; });
        _showSuccessAndPop();
      }
    } catch (e) {
      if (mounted) {
        setState(() => _submitting = false);
        _showError('交卷失败: ${e.toString().replaceAll('Exception: ', '')}');
      }
    }
  }

  void _showSuccessAndPop() {
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (ctx) => AlertDialog(
        title: const Text('交卷成功'),
        content: const Text('您的答案已提交，请等待批改结果。'),
        actions: [
          ElevatedButton(
            onPressed: () {
              Navigator.pop(ctx);
              Navigator.pop(context);
            },
            child: const Text('返回'),
          ),
        ],
      ),
    );
  }

  void _showError(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(content: Text(msg), backgroundColor: AppColors.error),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) {
      return const Scaffold(body: Center(child: CircularProgressIndicator()));
    }
    if (_error != null) {
      return Scaffold(
        appBar: AppBar(title: const Text('考试')),
        body: Center(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              const Icon(Icons.error_outline, size: 48, color: AppColors.error),
              const SizedBox(height: 16),
              Text(_error!, textAlign: TextAlign.center),
              const SizedBox(height: 16),
              ElevatedButton(onPressed: () => Navigator.pop(context), child: const Text('返回')),
            ],
          ),
        ),
      );
    }

    final exam = _exam!;
    if (exam.questions.isEmpty) {
      return Scaffold(
        appBar: AppBar(title: Text(exam.title)),
        body: const Center(child: Text('该考试暂无题目')),
      );
    }

    final q = exam.questions[_currentIndex];
    final answered = _answers.length;
    final total = exam.questions.length;

    return Scaffold(
      backgroundColor: AppColors.bgPage,
      appBar: AppBar(
        title: Text(exam.title, style: const TextStyle(fontSize: 16)),
        automaticallyImplyLeading: false,
        actions: [
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
            margin: const EdgeInsets.only(right: 8),
            decoration: BoxDecoration(
              color: _timerColor.withAlpha(26),
              borderRadius: BorderRadius.circular(20),
            ),
            child: Row(
              children: [
                Icon(Icons.timer_outlined, size: 16, color: _timerColor),
                const SizedBox(width: 4),
                Text(_timerText, style: TextStyle(color: _timerColor, fontWeight: FontWeight.bold)),
              ],
            ),
          ),
          TextButton(
            onPressed: _submitting ? null : () => _submitAnswers(),
            child: const Text('交卷'),
          ),
        ],
      ),
      body: Column(
        children: [
          // Progress bar
          Container(
            color: AppColors.bgCard,
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text('第 ${_currentIndex + 1} / $total 题',
                        style: const TextStyle(color: AppColors.textSecondary, fontSize: 13)),
                    Text('已答 $answered 题',
                        style: const TextStyle(color: AppColors.textSecondary, fontSize: 13)),
                  ],
                ),
                const SizedBox(height: 8),
                LinearProgressIndicator(
                  value: total > 0 ? (_currentIndex + 1) / total : 0,
                  backgroundColor: AppColors.borderWeak,
                  valueColor: const AlwaysStoppedAnimation(AppColors.primary),
                  borderRadius: BorderRadius.circular(4),
                  minHeight: 6,
                ),
              ],
            ),
          ),
          // Question
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Question header
                  Container(
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
                                color: AppColors.primary.withAlpha(26),
                                borderRadius: BorderRadius.circular(4),
                              ),
                              child: Text(
                                q.typeLabel,
                                style: const TextStyle(
                                  color: AppColors.primary,
                                  fontSize: 12,
                                  fontWeight: FontWeight.w500,
                                ),
                              ),
                            ),
                            const SizedBox(width: 8),
                            Text('${q.score} 分',
                                style: const TextStyle(
                                    color: AppColors.textWeak, fontSize: 12)),
                          ],
                        ),
                        const SizedBox(height: 12),
                        Text(
                          q.content,
                          style: const TextStyle(
                            fontSize: 15,
                            color: AppColors.textMain,
                            height: 1.6,
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                  // Answer options
                  _buildAnswerArea(q),
                ],
              ),
            ),
          ),
          // Navigation buttons
          Container(
            color: AppColors.bgCard,
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                if (_currentIndex > 0)
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: () => setState(() => _currentIndex--),
                      icon: const Icon(Icons.chevron_left),
                      label: const Text('上一题'),
                    ),
                  ),
                if (_currentIndex > 0) const SizedBox(width: 12),
                if (_currentIndex < total - 1)
                  Expanded(
                    child: ElevatedButton.icon(
                      onPressed: () => setState(() => _currentIndex++),
                      icon: const Icon(Icons.chevron_right),
                      label: const Text('下一题'),
                      iconAlignment: IconAlignment.end,
                    ),
                  ),
                if (_currentIndex == total - 1)
                  Expanded(
                    child: ElevatedButton(
                      onPressed: _submitting ? null : () => _submitAnswers(),
                      style: ElevatedButton.styleFrom(backgroundColor: AppColors.success),
                      child: _submitting
                          ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                          : const Text('提交答案'),
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildAnswerArea(ExamQuestion q) {
    switch (q.type) {
      case 1: // Single choice
        return _buildSingleChoice(q);
      case 2: // Multiple choice
        return _buildMultipleChoice(q);
      case 3: // True/False
        return _buildTrueFalse(q);
      case 4: // Short answer
        return _buildShortAnswer(q);
      default:
        return const SizedBox.shrink();
    }
  }

  Widget _buildSingleChoice(ExamQuestion q) {
    final selected = _answers[q.questionId] as String?;
    const labels = ['A', 'B', 'C', 'D', 'E', 'F'];
    return Column(
      children: (q.options ?? []).asMap().entries.map((entry) {
        final label = entry.key < labels.length ? labels[entry.key] : '${entry.key + 1}';
        final text = entry.value;
        final isSelected = selected == label;
        return _OptionTile(
          label: label,
          text: text,
          selected: isSelected,
          onTap: () => setState(() => _answers[q.questionId] = label),
        );
      }).toList(),
    );
  }

  Widget _buildMultipleChoice(ExamQuestion q) {
    final selected = (_answers[q.questionId] as List<String>?) ?? [];
    const labels = ['A', 'B', 'C', 'D', 'E', 'F'];
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        const Text('（多选题，请选择所有正确答案）',
            style: TextStyle(color: AppColors.textWeak, fontSize: 13)),
        const SizedBox(height: 8),
        ...(q.options ?? []).asMap().entries.map((entry) {
          final label = entry.key < labels.length ? labels[entry.key] : '${entry.key + 1}';
          final text = entry.value;
          final isSelected = selected.contains(label);
          return _OptionTile(
            label: label,
            text: text,
            selected: isSelected,
            multiSelect: true,
            onTap: () {
              setState(() {
                final list = List<String>.from(selected);
                if (isSelected) {
                  list.remove(label);
                } else {
                  list.add(label);
                }
                list.sort();
                _answers[q.questionId] = list;
              });
            },
          );
        }),
      ],
    );
  }

  Widget _buildTrueFalse(ExamQuestion q) {
    final selected = _answers[q.questionId] as String?;
    return Row(
      children: [
        Expanded(
          child: _OptionTile(
            label: '✓',
            text: '正确',
            selected: selected == 'true',
            onTap: () => setState(() => _answers[q.questionId] = 'true'),
          ),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: _OptionTile(
            label: '✗',
            text: '错误',
            selected: selected == 'false',
            onTap: () => setState(() => _answers[q.questionId] = 'false'),
          ),
        ),
      ],
    );
  }

  Widget _buildShortAnswer(ExamQuestion q) {
    return Container(
      decoration: BoxDecoration(
        color: AppColors.bgCard,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.borderStrong),
      ),
      child: TextField(
        maxLines: 6,
        decoration: const InputDecoration(
          hintText: '请输入您的答案...',
          border: InputBorder.none,
          contentPadding: EdgeInsets.all(16),
        ),
        onChanged: (v) => _answers[q.questionId] = v,
        controller: TextEditingController(text: _answers[q.questionId] as String? ?? ''),
      ),
    );
  }
}

class _OptionTile extends StatelessWidget {
  final String label;
  final String text;
  final bool selected;
  final bool multiSelect;
  final VoidCallback onTap;

  const _OptionTile({
    required this.label,
    required this.text,
    required this.selected,
    this.multiSelect = false,
    required this.onTap,
  });

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        margin: const EdgeInsets.only(bottom: 10),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        decoration: BoxDecoration(
          color: selected ? AppColors.primary.withAlpha(13) : AppColors.bgCard,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: selected ? AppColors.primary : AppColors.borderStrong,
            width: selected ? 1.5 : 1,
          ),
        ),
        child: Row(
          children: [
            Container(
              width: 28,
              height: 28,
              decoration: BoxDecoration(
                color: selected ? AppColors.primary : AppColors.bgPage,
                shape: multiSelect ? BoxShape.rectangle : BoxShape.circle,
                borderRadius: multiSelect ? BorderRadius.circular(6) : null,
                border: Border.all(
                  color: selected ? AppColors.primary : AppColors.borderStrong,
                ),
              ),
              alignment: Alignment.center,
              child: Text(
                label,
                style: TextStyle(
                  color: selected ? Colors.white : AppColors.textSecondary,
                  fontSize: 12,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Text(
                text,
                style: TextStyle(
                  color: selected ? AppColors.primary : AppColors.textMain,
                  fontSize: 14,
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
