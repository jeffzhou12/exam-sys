import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class FavoritesScreen extends StatefulWidget {
  const FavoritesScreen({super.key});

  @override
  State<FavoritesScreen> createState() => _FavoritesScreenState();
}

class _FavoritesScreenState extends State<FavoritesScreen>
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
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.pop(),
        ),
        title: const Text('我的收藏', style: TextStyle(fontWeight: FontWeight.bold)),
        actions: [
          IconButton(icon: const Icon(Icons.search), onPressed: () {}),
          IconButton(icon: const Icon(Icons.more_vert), onPressed: () {}),
        ],
        bottom: TabBar(
          controller: _tabController,
          labelColor: cs.primary,
          unselectedLabelColor: cs.onSurfaceVariant,
          indicatorColor: cs.primary,
          tabs: const [
            Tab(text: '书籍'),
            Tab(text: '题目'),
            Tab(text: '考试'),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [
          _BooksTab(cs: cs, tt: tt),
          _QuestionsTab(cs: cs, tt: tt),
          _ExamsTab(cs: cs, tt: tt),
        ],
      ),
    );
  }
}

// ── 收藏书籍 Tab ──────────────────────────────────────────────────────────────
class _BooksTab extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _BooksTab({required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(16),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text('3 本已收藏书籍',
                  style: tt.bodyMedium?.copyWith(color: cs.onSurfaceVariant)),
              OutlinedButton.icon(
                onPressed: () {},
                icon: const Icon(Icons.sort, size: 16),
                label: const Text('筛选'),
                style: OutlinedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                  textStyle: const TextStyle(fontSize: 12),
                ),
              ),
            ],
          ),
          const SizedBox(height: 12),

          // 大卡片（专业版）
          _FeaturedBookCard(cs: cs, tt: tt),
          const SizedBox(height: 12),

          // 小书单
          _SmallBookCard(
            title: '有机化学 II',
            meta: '128MB · PDF',
            cs: cs, tt: tt,
          ),
          const SizedBox(height: 8),
          _SmallBookCard(
            title: '技术伦理学',
            meta: '45MB · EPUB',
            cs: cs, tt: tt,
          ),
        ],
      ),
    );
  }
}

class _FeaturedBookCard extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _FeaturedBookCard({required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () => context.push('/books/detail/1'),
      child: Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: cs.surface,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: cs.outlineVariant),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // 专业版 badge
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
              decoration: BoxDecoration(
                color: cs.primary, borderRadius: BorderRadius.circular(4)),
              child: const Text('专业版',
                  style: TextStyle(color: Colors.white, fontSize: 11, fontWeight: FontWeight.bold)),
            ),
            const SizedBox(height: 12),
            Row(children: [
              Container(
                width: 70, height: 95,
                decoration: BoxDecoration(
                  gradient: LinearGradient(
                    colors: [cs.primary, cs.primaryContainer],
                    begin: Alignment.topLeft, end: Alignment.bottomRight),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Icon(Icons.auto_stories, color: cs.onPrimary, size: 30),
              ),
              const SizedBox(width: 14),
              Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text('高等结构力学',
                    style: tt.titleSmall?.copyWith(fontWeight: FontWeight.bold)),
                const SizedBox(height: 4),
                Text('埃莉诺·里格比 博士',
                    style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
                const SizedBox(height: 8),
                Row(children: [
                  Icon(Icons.star, size: 14, color: const Color(0xFFF59E0B)),
                  const SizedBox(width: 4),
                  Text('4.9',
                      style: tt.bodySmall?.copyWith(fontWeight: FontWeight.bold)),
                ]),
              ])),
              Icon(Icons.bookmark, color: cs.primary, size: 22),
            ]),
          ],
        ),
      ),
    );
  }
}

class _SmallBookCard extends StatelessWidget {
  final String title;
  final String meta;
  final ColorScheme cs;
  final TextTheme tt;
  const _SmallBookCard({required this.title, required this.meta, required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: cs.surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: cs.outlineVariant),
      ),
      child: Row(children: [
        Container(
          width: 44, height: 58,
          decoration: BoxDecoration(
            color: cs.primaryContainer, borderRadius: BorderRadius.circular(6)),
          child: Icon(Icons.book, color: cs.primary, size: 20),
        ),
        const SizedBox(width: 12),
        Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(title, style: tt.bodyMedium?.copyWith(fontWeight: FontWeight.w600)),
          Text(meta, style: tt.bodySmall?.copyWith(color: cs.onSurfaceVariant)),
        ])),
        Icon(Icons.bookmark, color: cs.primary, size: 18),
      ]),
    );
  }
}

// ── 收藏题目 Tab ──────────────────────────────────────────────────────────────
class _QuestionsTab extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _QuestionsTab({required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(mainAxisSize: MainAxisSize.min, children: [
        Icon(Icons.quiz_outlined, size: 56, color: cs.outlineVariant),
        const SizedBox(height: 12),
        Text('暂无收藏的题目', style: tt.bodyLarge?.copyWith(color: cs.onSurfaceVariant)),
        const SizedBox(height: 8),
        FilledButton(
          onPressed: () => context.push('/practice'),
          child: const Text('去做题'),
        ),
      ]),
    );
  }
}

// ── 收藏考试 Tab ──────────────────────────────────────────────────────────────
class _ExamsTab extends StatelessWidget {
  final ColorScheme cs;
  final TextTheme tt;
  const _ExamsTab({required this.cs, required this.tt});

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Column(mainAxisSize: MainAxisSize.min, children: [
        Icon(Icons.timer_outlined, size: 56, color: cs.outlineVariant),
        const SizedBox(height: 12),
        Text('暂无收藏的考试', style: tt.bodyLarge?.copyWith(color: cs.onSurfaceVariant)),
        const SizedBox(height: 8),
        FilledButton(
          onPressed: () => context.go('/exams'),
          child: const Text('浏览考试'),
        ),
      ]),
    );
  }
}
