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

  factory PagedResult.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) fromItem,
  ) =>
      PagedResult(
        items: (json['items'] as List<dynamic>? ?? [])
            .map((e) => fromItem(e as Map<String, dynamic>))
            .toList(),
        total: (json['totalCount'] as int?) ?? (json['total'] as int?) ?? 0,
        page: json['page'] as int? ?? 1,
        pageSize: json['pageSize'] as int? ?? 10,
      );
}

class ExamPaper {
  final String id;
  final String title;
  final String? description;
  final int totalScore;
  final int durationMinutes;
  final int status; // 0=Draft,1=Published,2=InProgress,3=Ended,4=Cancelled
  final DateTime? startTime;
  final DateTime? endTime;
  final bool antiCheatingEnabled;
  final int questionCount;
  final DateTime createdAt;

  const ExamPaper({
    required this.id,
    required this.title,
    this.description,
    required this.totalScore,
    required this.durationMinutes,
    required this.status,
    this.startTime,
    this.endTime,
    required this.antiCheatingEnabled,
    required this.questionCount,
    required this.createdAt,
  });

  String get statusLabel => switch (status) {
        0 => '草稿',
        1 => '报名中',
        2 => '进行中',
        3 => '已结束',
        4 => '已取消',
        _ => '未知',
      };

  factory ExamPaper.fromJson(Map<String, dynamic> json) => ExamPaper(
        id: json['id'].toString(),
        title: json['title'] as String,
        description: json['description'] as String?,
        totalScore: json['totalScore'] as int? ?? 100,
        durationMinutes: json['durationMinutes'] as int? ?? 60,
        status: json['status'] as int? ?? 0,
        startTime: json['startTime'] != null
            ? DateTime.tryParse(json['startTime'] as String)
            : null,
        endTime: json['endTime'] != null
            ? DateTime.tryParse(json['endTime'] as String)
            : null,
        antiCheatingEnabled: json['antiCheatingEnabled'] as bool? ?? false,
        questionCount: json['questionCount'] as int? ?? 0,
        createdAt: json['createdAt'] != null
            ? DateTime.tryParse(json['createdAt'] as String) ?? DateTime.now()
            : DateTime.now(),
      );
}

class ExamPaperDetail {
  final String id;
  final String title;
  final String? description;
  final int totalScore;
  final int durationMinutes;
  final int status;
  final DateTime? startTime;
  final DateTime? endTime;
  final bool antiCheatingEnabled;
  final List<ExamQuestion> questions;

  const ExamPaperDetail({
    required this.id,
    required this.title,
    this.description,
    required this.totalScore,
    required this.durationMinutes,
    required this.status,
    this.startTime,
    this.endTime,
    required this.antiCheatingEnabled,
    required this.questions,
  });

  factory ExamPaperDetail.fromJson(Map<String, dynamic> json) => ExamPaperDetail(
        id: json['id'].toString(),
        title: json['title'] as String,
        description: json['description'] as String?,
        totalScore: json['totalScore'] as int? ?? 100,
        durationMinutes: json['durationMinutes'] as int? ?? 60,
        status: json['status'] as int? ?? 0,
        startTime: json['startTime'] != null
            ? DateTime.tryParse(json['startTime'] as String)
            : null,
        endTime: json['endTime'] != null
            ? DateTime.tryParse(json['endTime'] as String)
            : null,
        antiCheatingEnabled: json['antiCheatingEnabled'] as bool? ?? false,
        questions: (json['questions'] as List<dynamic>? ?? [])
            .map((e) => ExamQuestion.fromJson(e as Map<String, dynamic>))
            .toList(),
      );
}

class ExamQuestion {
  final String questionId;
  final int type; // 1=Single,2=Multi,3=TrueFalse,4=Short
  final String content;
  final int score;
  final int order;
  final String? knowledgePoint;
  final int difficulty;
  final List<String>? options;

  const ExamQuestion({
    required this.questionId,
    required this.type,
    required this.content,
    required this.score,
    required this.order,
    this.knowledgePoint,
    required this.difficulty,
    this.options,
  });

  String get typeLabel => switch (type) {
        1 => '单选题',
        2 => '多选题',
        3 => '判断题',
        4 => '简答题',
        _ => '题目',
      };

  factory ExamQuestion.fromJson(Map<String, dynamic> json) => ExamQuestion(
        questionId: json['questionId'].toString(),
        type: json['type'] as int? ?? 1,
        content: json['content'] as String? ?? '',
        score: json['score'] as int? ?? 1,
        order: json['order'] as int? ?? 0,
        knowledgePoint: json['knowledgePoint'] as String?,
        difficulty: json['difficulty'] as int? ?? 1,
        options: (json['options'] as List<dynamic>?)
            ?.map((e) => e.toString())
            .toList(),
      );
}

class ExamAnswerItem {
  final String questionId;
  final String content;

  const ExamAnswerItem({required this.questionId, required this.content});

  Map<String, dynamic> toJson() => {
        'questionId': questionId,
        'content': content,
      };
}

class StudentExamSummary {
  final String examPaperId;
  final String examTitle;
  final int? totalScore;
  final int? maxScore;
  final DateTime? submittedAt;
  final bool isGraded;

  const StudentExamSummary({
    required this.examPaperId,
    required this.examTitle,
    this.totalScore,
    this.maxScore,
    this.submittedAt,
    required this.isGraded,
  });

  factory StudentExamSummary.fromJson(Map<String, dynamic> json) =>
      StudentExamSummary(
        examPaperId: json['examPaperId'].toString(),
        examTitle: json['examTitle'] as String? ?? '',
        totalScore: json['totalScore'] as int?,
        maxScore: json['maxScore'] as int?,
        submittedAt: json['submittedAt'] != null
            ? DateTime.tryParse(json['submittedAt'] as String)
            : null,
        isGraded: json['isGraded'] as bool? ?? false,
      );
}
