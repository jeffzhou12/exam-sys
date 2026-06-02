import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../api/api.dart';
import '../../../api/models/message_models.dart';
import '../../../theme/app_theme.dart';

final _inboxProvider = FutureProvider<List<Message>>((ref) async {
  final result = await messagesApi.getInbox(pageSize: 50);
  return result.items;
});

class MessagesScreen extends ConsumerWidget {
  const MessagesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(_inboxProvider);

    return Scaffold(
      backgroundColor: AppColors.bgPage,
      appBar: AppBar(
        title: const Text('站内消息'),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh),
            onPressed: () => ref.invalidate(_inboxProvider),
          ),
        ],
      ),
      body: async.when(
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
              ElevatedButton(onPressed: () => ref.invalidate(_inboxProvider), child: const Text('重新加载')),
            ],
          ),
        ),
        data: (messages) => RefreshIndicator(
          onRefresh: () async => ref.invalidate(_inboxProvider),
          child: messages.isEmpty
              ? const Center(
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(Icons.inbox_outlined, size: 48, color: AppColors.textWeak),
                      SizedBox(height: 16),
                      Text('暂无消息', style: TextStyle(color: AppColors.textSecondary)),
                    ],
                  ),
                )
              : ListView.separated(
                  padding: const EdgeInsets.all(16),
                  itemCount: messages.length,
                  separatorBuilder: (_, __) => const SizedBox(height: 8),
                  itemBuilder: (context, index) => _MessageTile(
                    message: messages[index],
                    onTap: () async {
                      if (!messages[index].isRead) {
                        await messagesApi.markAsRead(messages[index].id);
                        ref.invalidate(_inboxProvider);
                      }
                    },
                  ),
                ),
        ),
      ),
    );
  }
}

class _MessageTile extends StatelessWidget {
  final Message message;
  final VoidCallback onTap;
  const _MessageTile({required this.message, required this.onTap});

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: AppColors.bgCard,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(
            color: message.isRead ? AppColors.borderWeak : AppColors.primary.withAlpha(80),
          ),
        ),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              width: 40, height: 40,
              decoration: BoxDecoration(
                color: message.isRead ? AppColors.bgPage : AppColors.primary.withAlpha(26),
                shape: BoxShape.circle,
              ),
              child: Icon(
                message.isRead ? Icons.mail_outline : Icons.mail,
                color: message.isRead ? AppColors.textWeak : AppColors.primary,
                size: 20,
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Row(
                    children: [
                      Expanded(
                        child: Text(
                          message.subject,
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: message.isRead ? FontWeight.normal : FontWeight.w600,
                            color: AppColors.textMain,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                      ),
                      const SizedBox(width: 8),
                      Text(_formatDate(message.createdAt),
                          style: const TextStyle(fontSize: 12, color: AppColors.textWeak)),
                    ],
                  ),
                  const SizedBox(height: 4),
                  Text(
                    message.body,
                    maxLines: 2,
                    overflow: TextOverflow.ellipsis,
                    style: const TextStyle(fontSize: 13, color: AppColors.textSecondary),
                  ),
                  if (message.senderName != null) ...[
                    const SizedBox(height: 4),
                    Text('来自：${message.senderName}',
                        style: const TextStyle(fontSize: 12, color: AppColors.textWeak)),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  String _formatDate(DateTime dt) {
    final now = DateTime.now();
    if (dt.year == now.year && dt.month == now.month && dt.day == now.day) {
      return '${dt.hour.toString().padLeft(2, '0')}:${dt.minute.toString().padLeft(2, '0')}';
    }
    return '${dt.month}/${dt.day}';
  }
}

class MessagesScreen extends ConsumerWidget {
  const MessagesScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(_messagesProvider);

    return Scaffold(
      appBar: AppBar(title: const Text('站内消息')),
      body: async.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('加载失败：$e')),
        data: (messages) => RefreshIndicator(
          onRefresh: () => ref.refresh(_messagesProvider.future),
          child: messages.isEmpty
              ? const Center(child: Text('暂无消息'))
              : ListView.builder(
                  itemCount: messages.length,
                  itemBuilder: (context, index) =>
                      _MessageTile(message: messages[index]),
                ),
        ),
      ),
    );
  }
}

class _MessageTile extends StatelessWidget {
  final Message message;
  const _MessageTile({required this.message});

  @override
  Widget build(BuildContext context) {
    return ListTile(
      leading: Icon(
        message.isRead ? Icons.mail_outline : Icons.mail,
        color: message.isRead ? null : Theme.of(context).colorScheme.primary,
      ),
      title: Text(message.title,
          style: TextStyle(
              fontWeight: message.isRead ? FontWeight.normal : FontWeight.bold)),
      subtitle: Text(
        message.content,
        maxLines: 1,
        overflow: TextOverflow.ellipsis,
      ),
      trailing: Text(
        _formatDate(message.createdAt),
        style: const TextStyle(fontSize: 12, color: Colors.grey),
      ),
    );
  }

  String _formatDate(DateTime dt) {
    final now = DateTime.now();
    if (dt.year == now.year && dt.month == now.month && dt.day == now.day) {
      return '${dt.hour.toString().padLeft(2, '0')}:${dt.minute.toString().padLeft(2, '0')}';
    }
    return '${dt.month}/${dt.day}';
  }
}
