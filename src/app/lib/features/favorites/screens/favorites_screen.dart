import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../api/api.dart';
import '../../../api/models/book_models.dart';
import '../../../theme/app_theme.dart';

// targetType: 1=题目, 2=试卷, 3=图书
final _favBooksProvider = FutureProvider<List<FavoriteItem>>((ref) async {
  final result = await favoritesApi.getList(targetType: 3, pageSize: 50);
  return result.items;
});

final _favQuestionsProvider = FutureProvider<List<FavoriteItem>>((ref) async {
  final result = await favoritesApi.getList(targetType: 1, pageSize: 50);
  return result.items;
});

final _favExamsProvider = FutureProvider<List<FavoriteItem>>((ref) async {
  final result = await favoritesApi.getList(targetType: 2, pageSize: 50);
  return result.items;
});

class FavoritesScreen extends ConsumerStatefulWidget {
  const FavoritesScreen({super.key});

  @override
  ConsumerState<FavoritesScreen> createState() => _FavoritesScreenState();
}

class _FavoritesScreenState extends ConsumerState<FavoritesScreen>
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
    return Scaffold(
      backgroundColor: AppColors.bgPage,
      appBar: AppBar(
        leading: IconButton(icon: const Icon(Icons.arrow_back), onPressed: () => context.pop()),
        title: const Text('我的收藏'),
        bottom: TabBar(
          controller: _tabController,
          labelColor: AppColors.primary,
          unselectedLabelColor: AppColors.textSecondary,
          indicatorColor: AppColors.primary,
          tabs: const [
            Tab(text: '图书'),
            Tab(text: '题目'),
            Tab(text: '试卷'),
          ],
        ),
      ),
      body: TabBarView(
        controller: _tabController,
        children: [
          _FavTab(provider: _favBooksProvider, emptyLabel: '暂无收藏图书',
              icon: Icons.library_books_outlined,
              onTap: (item) => context.push('/books/detail/${item.targetId}')),
          _FavTab(provider: _favQuestionsProvider, emptyLabel: '暂无收藏题目',
              icon: Icons.quiz_outlined,
              onTap: (_) => context.push('/practice')),
          _FavTab(provider: _favExamsProvider, emptyLabel: '暂无收藏试卷',
              icon: Icons.assignment_outlined,
              onTap: (item) => context.push('/exams/${item.targetId}/detail')),
        ],
      ),
    );
  }
}

class _FavTab extends ConsumerWidget {
  final ProviderBase<AsyncValue<List<FavoriteItem>>> provider;
  final String emptyLabel;
  final IconData icon;
  final void Function(FavoriteItem) onTap;
  const _FavTab({required this.provider, required this.emptyLabel, required this.icon, required this.onTap});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(provider);
    return async.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (e, _) => Center(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.error_outline, size: 48, color: AppColors.error),
            const SizedBox(height: 12),
            Text(e.toString().replaceAll('Exception: ', ''),
                textAlign: TextAlign.center,
                style: const TextStyle(color: AppColors.textSecondary)),
            const SizedBox(height: 16),
            ElevatedButton(onPressed: () => ref.invalidate(provider), child: const Text('重新加载')),
          ],
        ),
      ),
      data: (items) => items.isEmpty
          ? Center(
              child: Column(mainAxisSize: MainAxisSize.min, children: [
                Icon(icon, size: 48, color: AppColors.textWeak),
                const SizedBox(height: 12),
                Text(emptyLabel, style: const TextStyle(color: AppColors.textSecondary)),
              ]),
            )
          : ListView.separated(
              padding: const EdgeInsets.all(16),
              itemCount: items.length,
              separatorBuilder: (_, __) => const SizedBox(height: 8),
              itemBuilder: (context, i) => _FavTile(
                item: items[i],
                icon: icon,
                onTap: () => onTap(items[i]),
                onRemove: () async {
                  await favoritesApi.removeFavorite(items[i].id);
                  ref.invalidate(provider);
                },
              ),
            ),
    );
  }
}

class _FavTile extends StatelessWidget {
  final FavoriteItem item;
  final IconData icon;
  final VoidCallback onTap;
  final VoidCallback onRemove;
  const _FavTile({required this.item, required this.icon, required this.onTap, required this.onRemove});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: AppColors.bgCard,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: AppColors.borderWeak),
        ),
        child: Row(
          children: [
            Container(
              width: 44, height: 44,
              decoration: BoxDecoration(
                color: AppColors.primary.withAlpha(20),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Icon(icon, color: AppColors.primary, size: 22),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(item.targetTitle,
                      style: const TextStyle(fontSize: 14, fontWeight: FontWeight.w500, color: AppColors.textMain),
                      maxLines: 2,
                      overflow: TextOverflow.ellipsis),
                  const SizedBox(height: 4),
                  Text(_formatDate(item.createdAt),
                      style: const TextStyle(fontSize: 12, color: AppColors.textWeak)),
                ],
              ),
            ),
            IconButton(
              icon: const Icon(Icons.bookmark, color: AppColors.primary),
              onPressed: onRemove,
              tooltip: '取消收藏',
            ),
          ],
        ),
      ),
    );
  }

  String _formatDate(DateTime dt) {
    return '${dt.month}/${dt.day}';
  }
}