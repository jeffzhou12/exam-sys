import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import '../../../api/api.dart';
import '../../../api/models/message_models.dart';

final _messagesProvider = FutureProvider<List<Message>>((ref) async {
  return messagesApi.getMessages();
});

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
