import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class BookDetailScreen extends StatefulWidget {
  final String bookId;
  const BookDetailScreen({super.key, required this.bookId});

  @override
  State<BookDetailScreen> createState() => _BookDetailScreenState();
}

class _BookDetailScreenState extends State<BookDetailScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;
  bool _isInShelf = false;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 2, vsync: this);
  }

  @override
  void dispose() {
    _tabController.dispose();
    super.dispose();
  }

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
        title: const Text('书籍详情', style: TextStyle(fontWeight: FontWeight.bold)),
        actions: [
          IconButton(icon: const Icon(Icons.share_outlined), onPressed: () {}),
          IconButton(icon: const Icon(Icons.more_vert), onPressed: () {}),
        ],
      ),
      body: Column(
        children: [
          Expanded(
            child: SingleChildScrollView(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // ── 书籍基本信息 ─────────────────────────────────
                  Container(
                    color: cs.surface,
                    padding: const EdgeInsets.all(20),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Container(
                          width: 100, height: 136,
                          decoration: BoxDecoration(
                            gradient: LinearGradient(
                              colors: [cs.primary, cs.primaryContainer],
                              begin: Alignment.topLeft, end: Alignment.bottomRight,
                            ),
                            borderRadius: BorderRadius.circular(10),
                            boxShadow: [
                              BoxShadow(color: cs.primary.withOpacity(0.3),
                                  blurRadius: 12, offset: const Offset(0, 4)),
                            ],
                          ),
                          child: Icon(Icons.auto_stories, color: cs.onPrimary, size: 40),
                        ),
                        const SizedBox(width: 16),
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text('EduFlow: 现代学习的高效之道',
                                  style: tt.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
                              const SizedBox(height: 6),
                              Text('王小明 著',
                                  style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
                              const SizedBox(height: 14),
                              // 统计
                              Row(children: [
                                _StatBadge(value: '9.8', label: '评分', cs: cs, tt: tt),
                                const SizedBox(width: 16),
                                _StatBadge(value: '24w', label: '阅读量', cs: cs, tt: tt),
                                const SizedBox(width: 16),
                                _StatBadge(value: '182', label: '页数', cs: cs, tt: tt),
                              ]),
                              const SizedBox(height: 14),
                              // 出版信息
                              Text('机械工业出版社 · 2023年10月版 · 中文简体',
                                  style: tt.bodySmall?.copyWith(
                                      color: cs.onSurfaceVariant, fontSize: 11)),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),

                  // ── Tab ─────────────────────────────────────────
                  Container(
                    color: cs.surface,
                    child: TabBar(
                      controller: _tabController,
                      labelColor: cs.primary,
                      unselectedLabelColor: cs.onSurfaceVariant,
                      indicatorColor: cs.primary,
                      tabs: const [Tab(text: '书籍简介'), Tab(text: '章节目录')],
                    ),
                  ),

                  SizedBox(
                    height: 350,
                    child: TabBarView(
                      controller: _tabController,
                      children: [
                        _IntroTab(cs: cs, tt: tt),
                        _ContentsTab(cs: cs, tt: tt),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),

          // ── 底部按钮 ──────────────────────────────────────────────
          Container(
            color: cs.surface,
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 24),
            child: Row(children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: () => setState(() => _isInShelf = !_isInShelf),
                  icon: Icon(_isInShelf ? Icons.library_add_check : Icons.library_add, size: 16),
                  label: Text(_isInShelf ? '已加入书架' : '加入书架'),
                  style: OutlinedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 13)),
                ),
              ),
              const SizedBox(width: 12),
              Expanded(
                flex: 2,
                child: FilledButton.icon(
                  onPressed: () => context.push('/books/${widget.bookId}/read'),
                  icon: const Icon(Icons.play_arrow, size: 18),
                  label: const Text('开始阅读'),
                  style: FilledButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 13)),
                ),
              ),
            ]),
          ),
        ],
      ),
    );
  }
}

class _StatBadge extends StatelessWidget {
  final String value, label;
  final ColorScheme cs;
  final TextTheme tt;
  const _StatBadge({required this.value, required this.label, required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Column(children: [
      Text(value, style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold, color: cs.primary)),
      Text(label, style: const TextStyle(fontSize: 10, color: Colors.grey)),
    ]);
  }
}

class _IntroTab extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _IntroTab({required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text('本书由学习科学专家王小明博士撰写，系统介绍了现代学习的核心方法论。'
            '结合认知科学与行为心理学最新研究成果，为学生和职场人士提供一套完整的高效学习框架。',
            style: tt.bodyMedium?.copyWith(height: 1.7)),
        const SizedBox(height: 16),
        Text('核心亮点', style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
        const SizedBox(height: 10),
        _HighlightItem(icon: Icons.bolt, label: '高效闭环', desc: '构建完整学习-复习-巩固循环体系', cs: cs, tt: tt),
        const SizedBox(height: 8),
        _HighlightItem(icon: Icons.psychology, label: '科学脑科学', desc: '基于记忆曲线和认知负荷理论设计', cs: cs, tt: tt),
      ]),
    );
  }
}

class _HighlightItem extends StatelessWidget {
  final IconData icon;
  final String label, desc;
  final ColorScheme cs;
  final TextTheme tt;
  const _HighlightItem({required this.icon, required this.label, required this.desc,
      required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Row(children: [
      Container(
        padding: const EdgeInsets.all(8),
        decoration: BoxDecoration(color: cs.primaryContainer, borderRadius: BorderRadius.circular(8)),
        child: Icon(icon, size: 18, color: cs.primary),
      ),
      const SizedBox(width: 12),
      Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        Text(label, style: tt.bodySmall?.copyWith(fontWeight: FontWeight.bold)),
        Text(desc, style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
      ]),
    ]);
  }
}

class _ContentsTab extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _ContentsTab({required this.cs, required this.tt});

  static const _chapters = [
    ('第1章', '学习的本质与误区', '第 1-18 页'),
    ('第2章', '主动回忆：记忆最强手段', '第 19-42 页'),
    ('第3章', '间隔重复与遗忘曲线', '第 43-78 页'),
    ('第4章', '分治策略：拆解复杂问题', '第 79-112 页'),
    ('第5章', '费曼技巧：用输出促输入', '第 113-140 页'),
  ];

  @override
  Widget build(BuildContext context) {
    return ListView.separated(
      padding: const EdgeInsets.all(16),
      itemCount: _chapters.length,
      separatorBuilder: (_, __) => const Divider(height: 1),
      itemBuilder: (_, i) {
        final c = _chapters[i];
        return Padding(
          padding: const EdgeInsets.symmetric(vertical: 10),
          child: Row(children: [
            Text(c.$1, style: TextStyle(fontSize: 11, color: cs.onSurfaceVariant)),
            const SizedBox(width: 12),
            Expanded(child: Text(c.$2, style: tt.bodyMedium)),
            Text(c.$3, style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
          ]),
        );
      },
    );
  }
}
