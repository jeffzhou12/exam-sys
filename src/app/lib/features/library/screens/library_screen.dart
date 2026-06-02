import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import '../../../api/api.dart';
import '../../../api/models/book_models.dart';
import '../../../theme/app_theme.dart';

final _booksProvider = FutureProvider<PagedResult<Book>>((ref) async {
  return booksApi.getBooks(pageSize: 50);
});

class LibraryScreen extends ConsumerStatefulWidget {
  const LibraryScreen({super.key});

  @override
  ConsumerState<LibraryScreen> createState() => _LibraryScreenState();
}

class _LibraryScreenState extends ConsumerState<LibraryScreen> {
  final _searchCtrl = TextEditingController();
  String _search = '';

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final booksAsync = ref.watch(_booksProvider);

    return Scaffold(
      backgroundColor: AppColors.bgPage,
      appBar: AppBar(
        title: const Text('图书馆'),
        actions: [
          IconButton(icon: const Icon(Icons.refresh), onPressed: () => ref.invalidate(_booksProvider)),
        ],
      ),
      body: Column(
        children: [
          Container(
            color: AppColors.bgCard,
            padding: const EdgeInsets.fromLTRB(16, 8, 16, 12),
            child: TextField(
              controller: _searchCtrl,
              decoration: const InputDecoration(
                hintText: '搜索书名...',
                prefixIcon: Icon(Icons.search),
                isDense: true,
              ),
              onChanged: (v) => setState(() => _search = v.trim().toLowerCase()),
            ),
          ),
          Expanded(
            child: booksAsync.when(
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
                    ElevatedButton(onPressed: () => ref.invalidate(_booksProvider), child: const Text('重新加载')),
                  ],
                ),
              ),
              data: (result) {
                final books = _search.isEmpty
                    ? result.items
                    : result.items.where((b) => b.title.toLowerCase().contains(_search)).toList();
                if (books.isEmpty) {
                  return const Center(
                    child: Column(mainAxisSize: MainAxisSize.min, children: [
                      Icon(Icons.library_books_outlined, size: 48, color: AppColors.textWeak),
                      SizedBox(height: 12),
                      Text('暂无图书', style: TextStyle(color: AppColors.textSecondary)),
                    ]),
                  );
                }
                return RefreshIndicator(
                  onRefresh: () async => ref.invalidate(_booksProvider),
                  child: GridView.builder(
                    padding: const EdgeInsets.all(16),
                    gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: 2,
                      childAspectRatio: 0.7,
                      crossAxisSpacing: 12,
                      mainAxisSpacing: 12,
                    ),
                    itemCount: books.length,
                    itemBuilder: (context, i) => _BookCard(book: books[i]),
                  ),
                );
              },
            ),
          ),
        ],
      ),
    );
  }
}

class _BookCard extends StatelessWidget {
  final Book book;
  const _BookCard({required this.book});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () => context.push('/books/detail/${book.id}'),
      child: Container(
        decoration: BoxDecoration(
          color: AppColors.bgCard,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: AppColors.borderWeak),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            ClipRRect(
              borderRadius: const BorderRadius.vertical(top: Radius.circular(11)),
              child: book.coverUrl != null
                  ? Image.network(
                      book.coverUrl!,
                      height: 140,
                      width: double.infinity,
                      fit: BoxFit.cover,
                      errorBuilder: (_, __, ___) => _PlaceholderCover(title: book.title),
                    )
                  : _PlaceholderCover(title: book.title),
            ),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.all(8),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(book.title,
                        style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w600, color: AppColors.textMain),
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis),
                    if (book.author != null) ...[
                      const SizedBox(height: 2),
                      Text(book.author!,
                          style: const TextStyle(fontSize: 11, color: AppColors.textSecondary),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis),
                    ],
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _PlaceholderCover extends StatelessWidget {
  final String title;
  const _PlaceholderCover({required this.title});

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 140,
      width: double.infinity,
      color: AppColors.primary.withAlpha(40),
      child: Center(
        child: Padding(
          padding: const EdgeInsets.all(8),
          child: Text(title,
              style: const TextStyle(color: AppColors.primary, fontSize: 13, fontWeight: FontWeight.w500),
              textAlign: TextAlign.center,
              maxLines: 3,
              overflow: TextOverflow.ellipsis),
        ),
      ),
    );
  }
}