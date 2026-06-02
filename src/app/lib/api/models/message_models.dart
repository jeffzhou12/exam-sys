class Message {
  final String id;
  final String senderName;
  final String recipientName;
  final String subject;
  final String body;
  final bool isRead;
  final DateTime createdAt;
  final int replyCount;
  final DateTime? latestReplyAt;

  const Message({
    required this.id,
    required this.senderName,
    required this.recipientName,
    required this.subject,
    required this.body,
    required this.isRead,
    required this.createdAt,
    this.replyCount = 0,
    this.latestReplyAt,
  });

  factory Message.fromJson(Map<String, dynamic> json) => Message(
        id: json['id'].toString(),
        senderName: json['senderName'] as String? ?? '系统',
        recipientName: json['recipientName'] as String? ?? '',
        subject: json['subject'] as String? ?? json['title'] as String? ?? '(无主题)',
        body: json['body'] as String? ?? json['content'] as String? ?? '',
        isRead: json['isRead'] as bool? ?? false,
        createdAt: json['createdAt'] != null
            ? DateTime.tryParse(json['createdAt'] as String) ?? DateTime.now()
            : DateTime.now(),
        replyCount: json['replyCount'] as int? ?? 0,
        latestReplyAt: json['latestReplyAt'] != null
            ? DateTime.tryParse(json['latestReplyAt'] as String)
            : null,
      );
}
