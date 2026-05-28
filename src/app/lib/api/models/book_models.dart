class Book {
  final String id;
  final String title;
  final String? author;
  final String? coverUrl;
  final String? description;
  final int? pageCount;
  final String? category;
  final double? rating;
  final int? readCount;

  const Book({
    required this.id,
    required this.title,
    this.author,
    this.coverUrl,
    this.description,
    this.pageCount,
    this.category,
    this.rating,
    this.readCount,
  });

  factory Book.fromJson(Map<String, dynamic> json) => Book(
        id: json['id'].toString(),
        title: json['title'] as String,
        author: json['author'] as String?,
        coverUrl: json['coverUrl'] as String?,
        description: json['description'] as String?,
        pageCount: json['pageCount'] as int?,
        category: json['category'] as String?,
        rating: (json['rating'] as num?)?.toDouble(),
        readCount: json['readCount'] as int?,
      );
}

class WrongQuestion {
  final String id;
  final String questionId;
  final String subject;
  final String? examTitle;
  final String content;
  final String? imageUrl;
  final int wrongCount;
  final bool isHighPriority;
  final DateTime wrongAt;

  const WrongQuestion({
    required this.id,
    required this.questionId,
    required this.subject,
    this.examTitle,
    required this.content,
    this.imageUrl,
    required this.wrongCount,
    required this.isHighPriority,
    required this.wrongAt,
  });

  factory WrongQuestion.fromJson(Map<String, dynamic> json) => WrongQuestion(
        id: json['id'].toString(),
        questionId: json['questionId'].toString(),
        subject: json['subject'] as String? ?? '',
        examTitle: json['examTitle'] as String?,
        content: json['content'] as String? ?? '',
        imageUrl: json['imageUrl'] as String?,
        wrongCount: json['wrongCount'] as int? ?? 1,
        isHighPriority: json['isHighPriority'] as bool? ?? false,
        wrongAt: json['wrongAt'] != null
            ? DateTime.parse(json['wrongAt'] as String)
            : DateTime.now(),
      );
}

class FavoriteItem {
  final String favoriteId;
  final String targetId;
  final int targetType; // 1=题目 2=试卷 3=图书
  final String title;
  final String? subtitle;
  final DateTime createdAt;

  const FavoriteItem({
    required this.favoriteId,
    required this.targetId,
    required this.targetType,
    required this.title,
    this.subtitle,
    required this.createdAt,
  });

  factory FavoriteItem.fromJson(Map<String, dynamic> json) => FavoriteItem(
        favoriteId: json['favoriteId'].toString(),
        targetId: json['targetId'].toString(),
        targetType: json['targetType'] as int,
        title: json['title'] as String? ?? '',
        subtitle: json['subtitle'] as String?,
        createdAt: json['createdAt'] != null
            ? DateTime.parse(json['createdAt'] as String)
            : DateTime.now(),
      );
}

class PagedResult<T> {
  final List<T> items;
  final int total;
  final int page;
  final int pageSize;

  const PagedResult({
    required this.items,
    required this.total,
    required this.page,
    required this.pageSize,
  });
}
