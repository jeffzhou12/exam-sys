import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../stores/auth_store.dart';

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
