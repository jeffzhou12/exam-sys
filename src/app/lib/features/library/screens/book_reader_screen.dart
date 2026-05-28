import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class BookReaderScreen extends StatefulWidget {
  final String bookId;
  const BookReaderScreen({super.key, required this.bookId});

  @override
  State<BookReaderScreen> createState() => _BookReaderScreenState();
}

class _BookReaderScreenState extends State<BookReaderScreen> {
  double _fontSize = 16.0;
  bool _bookmarked = false;

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final tt = Theme.of(context).textTheme;

    return Scaffold(
      backgroundColor: cs.surface,
      appBar: AppBar(
        backgroundColor: cs.surface,
        surfaceTintColor: Colors.transparent,
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
        title: const Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('算法导论 - EduFlow 智学',
                style: TextStyle(fontSize: 14, fontWeight: FontWeight.bold)),
            Text('第42页 共500页',
                style: TextStyle(fontSize: 11, color: Colors.grey)),
          ],
        ),
        actions: [
          IconButton(icon: const Icon(Icons.search), onPressed: () {}),
          IconButton(icon: const Icon(Icons.settings_outlined), onPressed: () {
            _showSettingsSheet(context, cs, tt);
          }),
        ],
      ),
      body: Column(
        children: [
          // ── 阅读内容区 ─────────────────────────────────────────────
          Expanded(
            child: SingleChildScrollView(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // 章节标题
                  Text('第4章', style: tt.labelMedium?.copyWith(color: cs.primary)),
                  const SizedBox(height: 4),
                  Text('分治策略',
                      style: tt.headlineSmall?.copyWith(fontWeight: FontWeight.bold)),
                  Text('第 42 页',
                      style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
                  const SizedBox(height: 20),

                  // 正文段落
                  Text(
                    '分治（Divide and Conquer）是一种重要的算法设计范式，通过将大问题拆分为若干子问题来解决。'
                    '其核心思想分为三个阶段：',
                    style: TextStyle(fontSize: _fontSize, height: 1.8),
                  ),
                  const SizedBox(height: 12),

                  // 关键词段落
                  RichText(
                    text: TextSpan(
                      style: TextStyle(
                          fontSize: _fontSize, height: 1.8, color: cs.onSurface),
                      children: const [
                        TextSpan(
                            text: '分解（Decompose）',
                            style: TextStyle(fontWeight: FontWeight.bold)),
                        TextSpan(text: '：将问题分割为规模更小、形式相同的子问题。\n'),
                        TextSpan(
                            text: '解决（Solve）',
                            style: TextStyle(fontWeight: FontWeight.bold)),
                        TextSpan(text: '：递归地求解各个子问题（若子问题足够小，直接求解）。\n'),
                        TextSpan(
                            text: '合并（Combine）',
                            style: TextStyle(fontWeight: FontWeight.bold)),
                        TextSpan(text: '：将子问题的解合并为原问题的解。'),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // 引用块
                  Container(
                    padding: const EdgeInsets.all(14),
                    decoration: BoxDecoration(
                      color: cs.primaryContainer.withOpacity(0.3),
                      borderRadius: BorderRadius.circular(10),
                      border: Border(left: BorderSide(color: cs.primary, width: 3)),
                    ),
                    child: Text(
                      '"分治法的关键在于子问题的独立性——各子问题之间不共享状态，这使得递归成为可能。"',
                      style: TextStyle(
                          fontSize: _fontSize - 1,
                          fontStyle: FontStyle.italic,
                          height: 1.6,
                          color: cs.onSurface),
                    ),
                  ),
                  const SizedBox(height: 16),

                  Text('典型应用：归并排序',
                      style: TextStyle(fontSize: _fontSize, fontWeight: FontWeight.bold)),
                  const SizedBox(height: 8),
                  Text(
                    '归并排序是分治思想的经典实现。将数组从中间分为左右两半，'
                    '分别排序后合并。时间复杂度为 O(n log n)，空间复杂度为 O(n)。',
                    style: TextStyle(fontSize: _fontSize, height: 1.8),
                  ),
                  const SizedBox(height: 16),

                  // 图片占位
                  Container(
                    width: double.infinity,
                    height: 160,
                    decoration: BoxDecoration(
                      color: cs.primaryContainer.withOpacity(0.2),
                      borderRadius: BorderRadius.circular(10),
                      border: Border.all(color: cs.outlineVariant),
                    ),
                    child: Column(mainAxisAlignment: MainAxisAlignment.center, children: [
                      Icon(Icons.image_outlined, size: 36, color: cs.onSurfaceVariant),
                      const SizedBox(height: 8),
                      Text('图 4.1：归并排序分治示意图',
                          style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
                    ]),
                  ),
                  const SizedBox(height: 20),

                  // 注释
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: const Color(0xFFFEF3C7),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Row(children: [
                      const CircleAvatar(
                        radius: 14,
                        backgroundColor: Color(0xFFF59E0B),
                        child: Text('博', style: TextStyle(color: Colors.white, fontSize: 11)),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: Text('主任提示：重点掌握合并步骤的实现，这是面试高频考点。',
                            style: tt.bodySmall?.copyWith(color: const Color(0xFF92400E))),
                      ),
                    ]),
                  ),
                ],
              ),
            ),
          ),

          // ── 阅读进度条 ─────────────────────────────────────────────
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Row(children: [
              Text('第 42 页', style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant)),
              Expanded(child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 10),
                child: LinearProgressIndicator(
                  value: 42 / 500,
                  backgroundColor: cs.primaryContainer,
                  valueColor: AlwaysStoppedAnimation<Color>(cs.primary),
                  minHeight: 4,
                ),
              )),
              Text('已完成 8%', style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant)),
            ]),
          ),
          const SizedBox(height: 8),

          // ── 底部工具栏 ─────────────────────────────────────────────
          Container(
            padding: const EdgeInsets.fromLTRB(8, 6, 8, 24),
            decoration: BoxDecoration(
              color: cs.surface,
              border: Border(top: BorderSide(color: cs.outlineVariant)),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceEvenly,
              children: [
                IconButton(
                  icon: Icon(Icons.menu_book_outlined, color: cs.onSurfaceVariant),
                  onPressed: () {},
                  tooltip: '目录',
                ),
                IconButton(
                  icon: Icon(Icons.remove, color: cs.onSurfaceVariant),
                  onPressed: () => setState(() => _fontSize = (_fontSize - 1).clamp(12.0, 24.0)),
                  tooltip: '缩小字体',
                ),
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
                  decoration: BoxDecoration(
                    border: Border.all(color: cs.outlineVariant),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: Text('${_fontSize.round()}',
                      style: TextStyle(fontWeight: FontWeight.bold, color: cs.onSurface)),
                ),
                IconButton(
                  icon: Icon(Icons.add, color: cs.onSurfaceVariant),
                  onPressed: () => setState(() => _fontSize = (_fontSize + 1).clamp(12.0, 24.0)),
                  tooltip: '放大字体',
                ),
                IconButton(
                  icon: Icon(
                    _bookmarked ? Icons.bookmark : Icons.bookmark_border,
                    color: _bookmarked ? cs.primary : cs.onSurfaceVariant,
                  ),
                  onPressed: () => setState(() => _bookmarked = !_bookmarked),
                  tooltip: '书签',
                ),
                IconButton(
                  icon: Icon(Icons.edit_note, color: cs.onSurfaceVariant),
                  onPressed: () {},
                  tooltip: '笔记',
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  void _showSettingsSheet(BuildContext context, ColorScheme cs, TextTheme tt) {
    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(16))),
      builder: (_) => Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('阅读设置', style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
            const SizedBox(height: 16),
            Text('字体大小', style: tt.bodySmall),
            Slider(
              value: _fontSize,
              min: 12, max: 24, divisions: 12,
              onChanged: (v) => setState(() => _fontSize = v),
            ),
          ],
        ),
      ),
    );
  }
}
