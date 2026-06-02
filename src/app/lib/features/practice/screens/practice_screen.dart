import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../api/api.dart';
import '../../../api/models/question_models.dart';
import '../../../stores/auth_store.dart';
import '../../../theme/app_theme.dart';

enum _Phase { setup, practice, result }

class PracticeScreen extends ConsumerStatefulWidget {
  const PracticeScreen({super.key});

  @override
  ConsumerState<PracticeScreen> createState() => _PracticeScreenState();
}

class _PracticeScreenState extends ConsumerState<PracticeScreen> {
  _Phase _phase = _Phase.setup;

  // Setup options
  int _count = 10;
  int _type = 0; // 0=all, 1=single, 2=multi, 3=truefalse, 4=short
  int _difficulty = 0; // 0=any
  final _knowledgeCtrl = TextEditingController();

  // Practice state
  List<PracticeQuestion> _questions = [];
  int _currentIndex = 0;
  final Map<String, dynamic> _answers = {};
  bool _loading = false;
  String? _loadError;

  // Result state
  PracticeResult? _result;
  bool _submitting = false;
  String? _submitError;

  @override
  void dispose() {
    _knowledgeCtrl.dispose();
    super.dispose();
  }

  Future<void> _startPractice() async {
    if (!ref.read(authStoreProvider).isLoggedIn) {
      context.go('/login?redirect=${Uri.encodeComponent('/practice')}');
      return;
    }
    setState(() { _loading = true; _loadError = null; });
    try {
      final qs = await practiceApi.getQuestions(
        count: _count,
        type: _type == 0 ? null : _type,
        difficulty: _difficulty == 0 ? null : _difficulty,
        knowledgePoint: _knowledgeCtrl.text.trim().isEmpty ? null : _knowledgeCtrl.text.trim(),
      );
      if (mounted) setState(() {
        _questions = qs;
        _currentIndex = 0;
        _answers.clear();
        _phase = _Phase.practice;
        _loading = false;
      });
    } catch (e) {
      if (mounted) setState(() {
        _loadError = e.toString().replaceAll('Exception: ', '');
        _loading = false;
      });
    }
  }

  Future<void> _submitAnswers() async {
    setState(() { _submitting = true; _submitError = null; });
    try {
      final answers = _questions.map((q) {
        dynamic raw = _answers[q.id];
        String ans;
        if (q.type == 2) {
          // multi-choice: List<String>
          final selected = (raw as List<String>? ?? [])..sort();
          ans = selected.join(',');
        } else if (q.type == 3) {
          // true/false
          ans = (raw as bool? ?? false) ? 'true' : 'false';
        } else {
          ans = (raw as String?) ?? '';
        }
        return {'questionId': q.id, 'answer': ans};
      }).toList();

      final result = await practiceApi.submitAnswers(answers);
      // save session async
      practiceApi.saveSession(
        count: _questions.length,
        correctCount: result.correctCount,
        totalScore: result.totalScore,
        maxScore: result.maxScore,
        duration: 0,
      ).catchError((_) {});
      if (mounted) setState(() {
        _result = result;
        _phase = _Phase.result;
        _submitting = false;
      });
    } catch (e) {
      if (mounted) setState(() {
        _submitError = e.toString().replaceAll('Exception: ', '');
        _submitting = false;
      });
    }
  }

  void _toggleMultiAnswer(String questionId, String label) {
    final current = (_answers[questionId] as List<String>?) ?? [];
    final updated = List<String>.from(current);
    if (updated.contains(label)) {
      updated.remove(label);
    } else {
      updated.add(label);
    }
    setState(() => _answers[questionId] = updated);
  }

  @override
  Widget build(BuildContext context) {
    return switch (_phase) {
      _Phase.setup => _buildSetup(),
      _Phase.practice => _buildPractice(),
      _Phase.result => _buildResult(),
    };
  }

  Widget _buildSetup() {
    return Scaffold(
      backgroundColor: AppColors.bgPage,
      appBar: AppBar(title: const Text('刷题练习')),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            _sectionTitle('题目数量'),
            const SizedBox(height: 10),
            Wrap(
              spacing: 10,
              children: [5, 10, 20, 30, 50].map((n) => _ChoiceChip(
                label: '$n 题',
                selected: _count == n,
                onTap: () => setState(() => _count = n),
              )).toList(),
            ),
            const SizedBox(height: 20),
            _sectionTitle('题目类型'),
            const SizedBox(height: 10),
            Wrap(
              spacing: 10,
              runSpacing: 8,
              children: [
                _ChoiceChip(label: '全部', selected: _type == 0, onTap: () => setState(() => _type = 0)),
                _ChoiceChip(label: '单选题', selected: _type == 1, onTap: () => setState(() => _type = 1)),
                _ChoiceChip(label: '多选题', selected: _type == 2, onTap: () => setState(() => _type = 2)),
                _ChoiceChip(label: '判断题', selected: _type == 3, onTap: () => setState(() => _type = 3)),
                _ChoiceChip(label: '简答题', selected: _type == 4, onTap: () => setState(() => _type = 4)),
              ],
            ),
            const SizedBox(height: 20),
            _sectionTitle('难度'),
            const SizedBox(height: 10),
            Wrap(
              spacing: 10,
              children: [
                _ChoiceChip(label: '不限', selected: _difficulty == 0, onTap: () => setState(() => _difficulty = 0)),
                _ChoiceChip(label: '★☆☆☆☆', selected: _difficulty == 1, onTap: () => setState(() => _difficulty = 1)),
                _ChoiceChip(label: '★★☆☆☆', selected: _difficulty == 2, onTap: () => setState(() => _difficulty = 2)),
                _ChoiceChip(label: '★★★☆☆', selected: _difficulty == 3, onTap: () => setState(() => _difficulty = 3)),
                _ChoiceChip(label: '★★★★☆', selected: _difficulty == 4, onTap: () => setState(() => _difficulty = 4)),
                _ChoiceChip(label: '★★★★★', selected: _difficulty == 5, onTap: () => setState(() => _difficulty = 5)),
              ],
            ),
            const SizedBox(height: 20),
            _sectionTitle('知识点（可选）'),
            const SizedBox(height: 10),
            TextField(
              controller: _knowledgeCtrl,
              decoration: const InputDecoration(
                hintText: '输入知识点关键词，如：高等数学、物理光学',
                prefixIcon: Icon(Icons.search),
              ),
            ),
            const SizedBox(height: 32),
            if (_loadError != null) ...[
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(
                  color: AppColors.error.withAlpha(20),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(_loadError!, style: const TextStyle(color: AppColors.error)),
              ),
              const SizedBox(height: 16),
            ],
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: _loading ? null : _startPractice,
                style: ElevatedButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 14)),
                child: _loading
                    ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                    : Text('开始练习（$_count 题）', style: const TextStyle(fontSize: 16)),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _sectionTitle(String title) => Text(
    title,
    style: const TextStyle(fontSize: 15, fontWeight: FontWeight.w600, color: AppColors.textMain),
  );

  Widget _buildPractice() {
    final q = _questions[_currentIndex];
    final isLast = _currentIndex == _questions.length - 1;
    final options = q.options;

    return Scaffold(
      backgroundColor: AppColors.bgPage,
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.close),
          onPressed: () => setState(() => _phase = _Phase.setup),
        ),
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('第 ${_currentIndex + 1} / ${_questions.length} 题',
                style: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold)),
            Text(q.typeLabel, style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
          ],
        ),
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(4),
          child: LinearProgressIndicator(
            value: (_currentIndex + 1) / _questions.length,
            backgroundColor: AppColors.borderStrong,
            valueColor: const AlwaysStoppedAnimation<Color>(AppColors.primary),
            minHeight: 4,
          ),
        ),
      ),
      body: Column(
        children: [
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(q.content, style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w600, height: 1.6, color: AppColors.textMain)),
                  const SizedBox(height: 20),
                  if (q.type == 1) // single choice
                    ...options.asMap().entries.map((e) {
                      final label = String.fromCharCode(65 + e.key);
                      final selected = _answers[q.id] == label;
                      return _OptionTile(
                        label: label,
                        text: e.value,
                        selected: selected,
                        onTap: () => setState(() => _answers[q.id] = label),
                      );
                    })
                  else if (q.type == 2) // multi choice
                    ...options.asMap().entries.map((e) {
                      final label = String.fromCharCode(65 + e.key);
                      final selected = ((_answers[q.id] as List<String>?) ?? []).contains(label);
                      return _OptionTile(
                        label: label,
                        text: e.value,
                        selected: selected,
                        isCheckbox: true,
                        onTap: () => _toggleMultiAnswer(q.id, label),
                      );
                    })
                  else if (q.type == 3) // true/false
                    Row(
                      children: [
                        Expanded(child: _TfButton(
                          label: '正确',
                          selected: _answers[q.id] == true,
                          onTap: () => setState(() => _answers[q.id] = true),
                        )),
                        const SizedBox(width: 12),
                        Expanded(child: _TfButton(
                          label: '错误',
                          selected: _answers[q.id] == false && _answers.containsKey(q.id),
                          onTap: () => setState(() => _answers[q.id] = false),
                        )),
                      ],
                    )
                  else // short answer
                    TextField(
                      maxLines: 5,
                      decoration: const InputDecoration(
                        hintText: '请输入你的答案...',
                        alignLabelWithHint: true,
                      ),
                      onChanged: (v) => setState(() => _answers[q.id] = v),
                    ),
                ],
              ),
            ),
          ),
          if (_submitError != null)
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: Text(_submitError!, style: const TextStyle(color: AppColors.error, fontSize: 13)),
            ),
          Container(
            padding: const EdgeInsets.fromLTRB(16, 10, 16, 24),
            color: AppColors.bgCard,
            child: Row(
              children: [
                if (_currentIndex > 0)
                  OutlinedButton(
                    onPressed: () => setState(() => _currentIndex--),
                    child: const Text('上一题'),
                  ),
                if (_currentIndex > 0) const SizedBox(width: 12),
                Expanded(
                  child: ElevatedButton(
                    onPressed: _submitting ? null : () {
                      if (isLast) {
                        _submitAnswers();
                      } else {
                        setState(() => _currentIndex++);
                      }
                    },
                    style: ElevatedButton.styleFrom(padding: const EdgeInsets.symmetric(vertical: 12)),
                    child: _submitting && isLast
                        ? const SizedBox(height: 20, width: 20, child: CircularProgressIndicator(strokeWidth: 2))
                        : Text(isLast ? '提交答案' : '下一题'),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildResult() {
    final r = _result!;
    final pct = r.maxScore > 0 ? (r.totalScore / r.maxScore * 100).round() : 0;
    return Scaffold(
      backgroundColor: AppColors.bgPage,
      appBar: AppBar(
        title: const Text('练习结果'),
        leading: IconButton(
          icon: const Icon(Icons.close),
          onPressed: () => setState(() { _phase = _Phase.setup; _result = null; }),
        ),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Column(
          children: [
            Container(
              padding: const EdgeInsets.all(24),
              decoration: BoxDecoration(
                color: AppColors.bgCard,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: AppColors.borderWeak),
              ),
              child: Column(
                children: [
                  Text('$pct%',
                      style: TextStyle(
                        fontSize: 48,
                        fontWeight: FontWeight.bold,
                        color: pct >= 60 ? AppColors.success : AppColors.error,
                      )),
                  const SizedBox(height: 8),
                  Text('得分 ${r.totalScore} / ${r.maxScore}',
                      style: const TextStyle(color: AppColors.textSecondary, fontSize: 16)),
                  const SizedBox(height: 16),
                  Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      _StatBox(label: '总题数', value: '${r.items.length}'),
                      const SizedBox(width: 24),
                      _StatBox(label: '答对', value: '${r.correctCount}', color: AppColors.success),
                      const SizedBox(width: 24),
                      _StatBox(label: '答错', value: '${r.items.length - r.correctCount}', color: AppColors.error),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            ...r.items.asMap().entries.map((e) => Padding(
              padding: const EdgeInsets.only(bottom: 12),
              child: _ResultItem(index: e.key, item: e.value),
            )),
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: ElevatedButton(
                onPressed: () => setState(() { _phase = _Phase.setup; _result = null; }),
                child: const Text('再来一轮'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ChoiceChip extends StatelessWidget {
  final String label;
  final bool selected;
  final VoidCallback onTap;
  const _ChoiceChip({required this.label, required this.selected, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
        margin: const EdgeInsets.only(bottom: 4),
        decoration: BoxDecoration(
          color: selected ? AppColors.primary : AppColors.bgCard,
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: selected ? AppColors.primary : AppColors.borderStrong),
        ),
        child: Text(label,
            style: TextStyle(
              color: selected ? Colors.white : AppColors.textSecondary,
              fontSize: 13,
              fontWeight: selected ? FontWeight.w600 : FontWeight.normal,
            )),
      ),
    );
  }
}

class _OptionTile extends StatelessWidget {
  final String label;
  final String text;
  final bool selected;
  final bool isCheckbox;
  final VoidCallback onTap;
  const _OptionTile({required this.label, required this.text, required this.selected,
      required this.onTap, this.isCheckbox = false});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        margin: const EdgeInsets.only(bottom: 10),
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: selected ? AppColors.primary.withAlpha(20) : AppColors.bgCard,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: selected ? AppColors.primary : AppColors.borderStrong,
            width: selected ? 1.5 : 1,
          ),
        ),
        child: Row(
          children: [
            Container(
              width: 28, height: 28,
              decoration: BoxDecoration(
                color: selected ? AppColors.primary : AppColors.bgPage,
                shape: isCheckbox ? BoxShape.rectangle : BoxShape.circle,
                borderRadius: isCheckbox ? BorderRadius.circular(4) : null,
                border: Border.all(color: selected ? AppColors.primary : AppColors.borderStrong),
              ),
              child: Center(
                child: selected
                    ? Icon(Icons.check, size: 16, color: Colors.white)
                    : Text(label, style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold, color: AppColors.textSecondary)),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Text(text, style: TextStyle(
                color: selected ? AppColors.primary : AppColors.textMain,
                fontWeight: selected ? FontWeight.w500 : FontWeight.normal,
              )),
            ),
          ],
        ),
      ),
    );
  }
}

class _TfButton extends StatelessWidget {
  final String label;
  final bool selected;
  final VoidCallback onTap;
  const _TfButton({required this.label, required this.selected, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(vertical: 16),
        decoration: BoxDecoration(
          color: selected ? AppColors.primary : AppColors.bgCard,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: selected ? AppColors.primary : AppColors.borderStrong),
        ),
        child: Center(
          child: Text(label, style: TextStyle(
            color: selected ? Colors.white : AppColors.textMain,
            fontSize: 16,
            fontWeight: FontWeight.w600,
          )),
        ),
      ),
    );
  }
}

class _StatBox extends StatelessWidget {
  final String label;
  final String value;
  final Color? color;
  const _StatBox({required this.label, required this.value, this.color});

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        Text(value, style: TextStyle(fontSize: 24, fontWeight: FontWeight.bold, color: color ?? AppColors.textMain)),
        Text(label, style: const TextStyle(fontSize: 12, color: AppColors.textSecondary)),
      ],
    );
  }
}

class _ResultItem extends StatefulWidget {
  final int index;
  final PracticeResultItem item;
  const _ResultItem({required this.index, required this.item});

  @override
  State<_ResultItem> createState() => _ResultItemState();
}

class _ResultItemState extends State<_ResultItem> {
  bool _expanded = false;

  @override
  Widget build(BuildContext context) {
    final item = widget.item;
    return Container(
      decoration: BoxDecoration(
        color: AppColors.bgCard,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
          color: item.isCorrect ? AppColors.success.withAlpha(80) : AppColors.error.withAlpha(80),
        ),
      ),
      child: Column(
        children: [
          ListTile(
            leading: Container(
              width: 32, height: 32,
              decoration: BoxDecoration(
                color: item.isCorrect ? AppColors.success.withAlpha(26) : AppColors.error.withAlpha(26),
                shape: BoxShape.circle,
              ),
              child: Icon(
                item.isCorrect ? Icons.check : Icons.close,
                color: item.isCorrect ? AppColors.success : AppColors.error,
                size: 18,
              ),
            ),
            title: Text('第 ${widget.index + 1} 题',
                style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w500, color: AppColors.textMain)),
            subtitle: Text(item.isCorrect ? '回答正确' : '回答错误',
                style: TextStyle(fontSize: 12, color: item.isCorrect ? AppColors.success : AppColors.error)),
            trailing: IconButton(
              icon: Icon(_expanded ? Icons.expand_less : Icons.expand_more),
              onPressed: () => setState(() => _expanded = !_expanded),
            ),
          ),
          if (_expanded)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Divider(height: 1),
                  const SizedBox(height: 12),
                  _DetailRow(label: '你的答案', value: item.studentAnswer ?? '-',
                      color: item.isCorrect ? AppColors.success : AppColors.error),
                  const SizedBox(height: 6),
                  _DetailRow(label: '正确答案', value: item.correctAnswer ?? '-', color: AppColors.success),
                  if (item.explanation != null && item.explanation!.isNotEmpty) ...[
                    const SizedBox(height: 10),
                    Text('解析', style: const TextStyle(fontWeight: FontWeight.bold, color: AppColors.textMain, fontSize: 13)),
                    const SizedBox(height: 4),
                    Text(item.explanation!, style: const TextStyle(color: AppColors.textSecondary, fontSize: 13)),
                  ],
                ],
              ),
            ),
        ],
      ),
    );
  }
}

class _DetailRow extends StatelessWidget {
  final String label;
  final String value;
  final Color? color;
  const _DetailRow({required this.label, required this.value, this.color});

  @override
  Widget build(BuildContext context) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text('$label：', style: const TextStyle(color: AppColors.textSecondary, fontSize: 13)),
        Expanded(child: Text(value, style: TextStyle(color: color ?? AppColors.textMain, fontSize: 13, fontWeight: FontWeight.w500))),
      ],
    );
  }
}

class PracticeScreen extends ConsumerStatefulWidget {
  const PracticeScreen({super.key});

  @override
  ConsumerState<PracticeScreen> createState() => _PracticeScreenState();
}

class _PracticeScreenState extends ConsumerState<PracticeScreen> {
  int _currentIndex = 11; // 第12题 (0-based → display as 12)
  final int _total = 20;
  int? _selectedOption;
  bool _bookmarked = false;
  int _secondsLeft = 18 * 60 + 15;
  Timer? _timer;

  final List<String> _options = [
    'mRNA 从 DNA 模板链读取信息，合成时方向为 3\'→5\'。',
    'RNA 聚合酶沿模板链 3\'→5\' 方向移动，合成新链方向为 5\'→3\'。',
    'mRNA 与 DNA 的编码链（非模板链）具有相同的碱基序列（U 替换 T）。',
    '转录终止时，RNA 聚合酶脱离 DNA，mRNA 被释放并加工。',
  ];

  @override
  void initState() {
    super.initState();
    _timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (_secondsLeft > 0) {
        setState(() => _secondsLeft--);
      } else {
        _timer?.cancel();
      }
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }

  String get _timerText {
    final m = _secondsLeft ~/ 60;
    final s = _secondsLeft % 60;
    return '${m.toString().padLeft(2, '0')}:${s.toString().padLeft(2, '0')}';
  }

  double get _progress => (_currentIndex + 1) / _total;

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
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('练习中', style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15)),
            Text('第 ${_currentIndex + 1} 题 共 $_total 题',
                style: TextStyle(fontSize: 12, color: cs.onSurfaceVariant)),
          ],
        ),
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(4),
          child: LinearProgressIndicator(
            value: _progress,
            backgroundColor: cs.primaryContainer,
            valueColor: AlwaysStoppedAnimation<Color>(cs.primary),
            minHeight: 4,
          ),
        ),
        actions: [
          Container(
            margin: const EdgeInsets.only(right: 8),
            padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
            decoration: BoxDecoration(
              color: _secondsLeft < 60 ? const Color(0xFFDC2626).withOpacity(0.1) : cs.primaryContainer,
              borderRadius: BorderRadius.circular(8),
            ),
            child: Row(children: [
              Icon(Icons.timer_outlined, size: 14,
                  color: _secondsLeft < 60 ? const Color(0xFFDC2626) : cs.primary),
              const SizedBox(width: 4),
              Text(_timerText,
                  style: TextStyle(
                    fontSize: 13, fontWeight: FontWeight.bold,
                    color: _secondsLeft < 60 ? const Color(0xFFDC2626) : cs.primary,
                  )),
            ]),
          ),
          CircleAvatar(
            radius: 14,
            backgroundColor: cs.primaryContainer,
            child: Icon(Icons.person, size: 16, color: cs.primary),
          ),
          const SizedBox(width: 8),
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
                  // 科目标签
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: cs.primaryContainer,
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Text('分子生物学',
                        style: TextStyle(fontSize: 12, color: cs.primary, fontWeight: FontWeight.w500)),
                  ),
                  const SizedBox(height: 16),

                  // 题目
                  Text(
                    '已完成 ${(_progress * 100).round()}%',
                    style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    '关于真核细胞中 DNA 转录过程，下列哪种说法是正确的？',
                    style: tt.bodyLarge?.copyWith(fontWeight: FontWeight.w600, height: 1.5),
                  ),
                  const SizedBox(height: 12),

                  // 图片区域
                  Container(
                    width: double.infinity,
                    height: 140,
                    decoration: BoxDecoration(
                      color: cs.primaryContainer.withOpacity(0.3),
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: cs.outlineVariant),
                    ),
                    child: Stack(
                      children: [
                        Center(child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Icon(Icons.image_outlined, size: 40, color: cs.onSurfaceVariant),
                            const SizedBox(height: 8),
                            Text('转录过程图',
                                style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
                          ],
                        )),
                        Positioned(
                          right: 8, bottom: 8,
                          child: Container(
                            padding: const EdgeInsets.all(4),
                            decoration: BoxDecoration(
                              color: cs.surface, borderRadius: BorderRadius.circular(6)),
                            child: Icon(Icons.zoom_in, size: 16, color: cs.primary),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 20),

                  // 选项
                  ..._options.asMap().entries.map((entry) {
                    final idx = entry.key;
                    final text = entry.value;
                    final label = String.fromCharCode(65 + idx); // A B C D
                    final isSelected = _selectedOption == idx;
                    return Padding(
                      padding: const EdgeInsets.only(bottom: 10),
                      child: GestureDetector(
                        onTap: () => setState(() => _selectedOption = idx),
                        child: AnimatedContainer(
                          duration: const Duration(milliseconds: 150),
                          padding: const EdgeInsets.all(14),
                          decoration: BoxDecoration(
                            color: isSelected ? cs.primaryContainer : cs.surface,
                            borderRadius: BorderRadius.circular(12),
                            border: Border.all(
                              color: isSelected ? cs.primary : cs.outlineVariant,
                              width: isSelected ? 1.5 : 1,
                            ),
                          ),
                          child: Row(
                            children: [
                              Container(
                                width: 28, height: 28,
                                decoration: BoxDecoration(
                                  color: isSelected ? cs.primary : cs.outlineVariant.withOpacity(0.3),
                                  shape: BoxShape.circle,
                                ),
                                child: Center(
                                  child: isSelected
                                      ? Icon(Icons.check_circle, size: 18, color: cs.onPrimary)
                                      : Text(label,
                                          style: TextStyle(
                                              fontWeight: FontWeight.bold,
                                              color: isSelected ? cs.onPrimary : cs.onSurface,
                                              fontSize: 12)),
                                ),
                              ),
                              const SizedBox(width: 12),
                              Expanded(
                                child: Text(text,
                                    style: tt.bodyMedium?.copyWith(
                                        color: isSelected ? cs.primary : cs.onSurface,
                                        fontWeight: isSelected ? FontWeight.w500 : FontWeight.normal)),
                              ),
                            ],
                          ),
                        ),
                      ),
                    );
                  }),
                ],
              ),
            ),
          ),

          // ── 底部工具栏 ──────────────────────────────────────────────
          Container(
            color: cs.surface,
            padding: const EdgeInsets.fromLTRB(16, 10, 16, 24),
            child: Row(
              children: [
                OutlinedButton.icon(
                  onPressed: () {},
                  icon: const Icon(Icons.flag_outlined, size: 16),
                  label: const Text('反馈'),
                  style: OutlinedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10)),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: FilledButton(
                    onPressed: _selectedOption != null
                        ? () {
                            if (ref.read(authStoreProvider).isLoggedIn) {
                              // 提交答案逻辑
                            } else {
                              context.go('/login?redirect=${Uri.encodeComponent('/practice')}');
                            }
                          }
                        : null,
                    child: const Text('检查答案'),
                    style: FilledButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 12)),
                  ),
                ),
                const SizedBox(width: 8),
                IconButton(
                  onPressed: () => setState(() => _bookmarked = !_bookmarked),
                  icon: Icon(
                    _bookmarked ? Icons.bookmark : Icons.bookmark_border,
                    color: _bookmarked ? cs.primary : cs.onSurfaceVariant,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () {
          if (ref.read(authStoreProvider).isLoggedIn) {
            context.push('/ai-analysis');
          } else {
            context.go('/login?redirect=${Uri.encodeComponent('/practice')}');
          }
        },
        backgroundColor: cs.primaryContainer,
        foregroundColor: cs.primary,
        icon: const Icon(Icons.smart_toy_outlined, size: 18),
        label: const Text('AI 分析', style: TextStyle(fontWeight: FontWeight.w600)),
        elevation: 2,
      ),
    );
  }
}
