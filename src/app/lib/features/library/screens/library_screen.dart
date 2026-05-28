import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

class LibraryScreen extends ConsumerStatefulWidget {
  const LibraryScreen({super.key});

  @override
  ConsumerState<LibraryScreen> createState() => _LibraryScreenState();
}

class _LibraryScreenState extends ConsumerState<LibraryScreen>
    with SingleTickerProviderStateMixin {
  late TabController _tabController;

  @override
  void initState() {
    super.initState();
    _tabController = TabController(length: 3, vsync: this);
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
        title: Row(
          children: [
            CircleAvatar(
              radius: 14,
              backgroundColor: cs.primaryContainer,
              child: Icon(Icons.person, size: 16, color: cs.primary),
            ),
            const SizedBox(width: 8),
            Text('EduFlow 智学', style: tt.titleMedium?.copyWith(fontWeight: FontWeight.bold)),
          ],
        ),
        actions: [
          IconButton(icon: const Icon(Icons.search), onPressed: () {}),
          IconButton(icon: const Icon(Icons.notifications_outlined), onPressed: () {}),
        ],
        bottom: TabBar(
          controller: _tabController,
          labelColor: cs.primary,
          unselectedLabelColor: cs.onSurfaceVariant,
          indicatorColor: cs.primary,
          tabs: const [
            Tab(text: '正在学习'),
            Tab(text: '已完成'),
            Tab(text: '收藏'),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [
          _LearningTab(cs: cs, tt: tt),
          _CompletedTab(cs: cs, tt: tt),
          _FavoritesTab(cs: cs, tt: tt),
        ],
      ),
    );
  }
}

// ── 正在学习 Tab ──────────────────────────────────────────────────────────────
class _LearningTab extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _LearningTab({required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // 主推荐书
          _FeaturedBookCard(cs: cs, tt: tt),
          const SizedBox(height: 16),

          // 小卡片网格
          ...(_learningBooks.map((book) => Padding(
            padding: const EdgeInsets.only(bottom: 12),
            child: _BookListItem(book: book, cs: cs, tt: tt),
          ))),

          const SizedBox(height: 16),
          Text('推荐资料', style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
          const SizedBox(height: 12),
          SizedBox(
            height: 130,
            child: ListView.separated(
              scrollDirection: Axis.horizontal,
              itemCount: _recommendedBooks.length,
              separatorBuilder: (_, __) => const SizedBox(width: 12),
              itemBuilder: (context, index) {
                final book = _recommendedBooks[index];
                return _RecommendedBookCard(title: book, cs: cs, tt: tt);
              },
            ),
          ),
        ],
      ),
    );
  }
}

final _learningBooks = [
  _BookItem('数据结构与算法(Java版)', '计算机科学 · 必修课', 0.45),
  _BookItem('全球史：从史前到21世纪', '通识教育 · L.S. 斯塔夫里阿诺斯', 0.15),
  _BookItem('线性代数精讲 (下)', '数学基础 · 正在进行', 0.88),
];

final _recommendedBooks = ['量子力学导引', '现代设计史', '高级口译教程', '人工智能导论'];

class _BookItem {
  final String title;
  final String subtitle;
  final double progress;
  _BookItem(this.title, this.subtitle, this.progress);
}

class _FeaturedBookCard extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _FeaturedBookCard({required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: cs.surface,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: cs.outlineVariant),
      ),
      child: Row(
        children: [
          Container(
            width: 80,
            height: 110,
            decoration: BoxDecoration(
              gradient: LinearGradient(
                colors: [cs.primary, cs.primaryContainer],
                begin: Alignment.topLeft,
                end: Alignment.bottomRight,
              ),
              borderRadius: BorderRadius.circular(8),
            ),
            child: Icon(Icons.auto_stories, color: cs.onPrimary, size: 32),
          ),
          const SizedBox(width: 16),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                  decoration: BoxDecoration(
                    color: const Color(0xFFDC2626).withOpacity(0.1),
                    borderRadius: BorderRadius.circular(4),
                  ),
                  child: const Text('重点复习',
                      style: TextStyle(fontSize: 11, color: Color(0xFFDC2626), fontWeight: FontWeight.w500)),
                ),
                const SizedBox(height: 6),
                Text('高级宏观经济学分析',
                    style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                const SizedBox(height: 4),
                Text('王教授 · 经济学院',
                    style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
                const SizedBox(height: 10),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text('已学习 72%',
                        style: tt.bodySmall?.copyWith(color: cs.primary)),
                    Text('上次于 2小时前',
                        style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
                  ],
                ),
                const SizedBox(height: 4),
                ClipRRect(
                  borderRadius: BorderRadius.circular(4),
                  child: LinearProgressIndicator(
                    value: 0.72,
                    backgroundColor: cs.primaryContainer,
                    valueColor: AlwaysStoppedAnimation<Color>(cs.primary),
                    minHeight: 6,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _BookListItem extends StatelessWidget {
  final _BookItem book;
  final ColorScheme cs;
  final TextTheme tt;
  const _BookListItem({required this.book, required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () => context.push('/books/detail/1'),
      child: Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: cs.surface,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: cs.outlineVariant),
        ),
        child: Row(
          children: [
            Container(
              width: 48,
              height: 64,
              decoration: BoxDecoration(
                color: cs.primaryContainer,
                borderRadius: BorderRadius.circular(6),
              ),
              child: Icon(Icons.book, color: cs.primary, size: 22),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(book.title,
                      style: tt.bodyMedium?.copyWith(fontWeight: FontWeight.w600),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis),
                  const SizedBox(height: 2),
                  Text(book.subtitle,
                      style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Expanded(
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(4),
                          child: LinearProgressIndicator(
                            value: book.progress,
                            backgroundColor: cs.primaryContainer,
                            valueColor: AlwaysStoppedAnimation<Color>(cs.primary),
                            minHeight: 5,
                          ),
                        ),
                      ),
                      const SizedBox(width: 8),
                      Text('${(book.progress * 100).round()}%',
                          style: tt.bodySmall?.copyWith(color: cs.primary)),
                    ],
                  ),
                ],
              ),
            ),
            IconButton(
              icon: Icon(Icons.more_vert, size: 18, color: cs.onSurfaceVariant),
              onPressed: () {},
              padding: EdgeInsets.zero,
              constraints: const BoxConstraints(),
            ),
          ],
        ),
      ),
    );
  }
}

class _RecommendedBookCard extends StatelessWidget {
  final String title;
  final ColorScheme cs;
  final TextTheme tt;
  const _RecommendedBookCard({required this.title, required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () => context.push('/books/detail/1'),
      child: Container(
        width: 90,
        decoration: BoxDecoration(
          color: cs.surface,
          borderRadius: BorderRadius.circular(10),
          border: Border.all(color: cs.outlineVariant),
        ),
        child: Column(
          children: [
            Expanded(
              child: Container(
                decoration: BoxDecoration(
                  color: cs.primaryContainer,
                  borderRadius: const BorderRadius.vertical(top: Radius.circular(9)),
                ),
                child: Center(child: Icon(Icons.auto_stories, color: cs.primary, size: 28)),
              ),
            ),
            Padding(
              padding: const EdgeInsets.all(6),
              child: Text(title,
                  style: tt.bodySmall?.copyWith(fontWeight: FontWeight.w600),
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  textAlign: TextAlign.center),
            ),
          ],
        ),
      ),
    );
  }
}

// ── 已完成 Tab ────────────────────────────────────────────────────────────────
class _CompletedTab extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _CompletedTab({required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.check_circle_outline, size: 56, color: cs.outlineVariant),
          const SizedBox(height: 12),
          Text('暂无已完成的书籍', style: tt.bodyLarge?.copyWith(color: cs.onSurfaceVariant)),
          const SizedBox(height: 8),
          FilledButton(
            onPressed: () {},
            child: const Text('浏览书库'),
          ),
        ],
      ),
    );
  }
}

// ── 收藏 Tab ──────────────────────────────────────────────────────────────────
class _FavoritesTab extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _FavoritesTab({required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(Icons.bookmark_outline, size: 56, color: cs.outlineVariant),
          const SizedBox(height: 12),
          Text('还没有收藏任何书籍', style: tt.bodyLarge?.copyWith(color: cs.onSurfaceVariant)),
          const SizedBox(height: 8),
          OutlinedButton.icon(
            onPressed: () => context.push('/favorites'),
            icon: const Icon(Icons.star_outline),
            label: const Text('查看全部收藏'),
          ),
        ],
      ),
    );
  }
}
