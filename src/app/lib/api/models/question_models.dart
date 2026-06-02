class Question {
  final String id;
  final String content;
  final int type; // 1=Single,2=Multi,3=TrueFalse,4=Short
  final List<String> options;
  final String? answer;
  final String? explanation;
  final int score;

  const Question({
    required this.id,
    required this.content,
    required this.type,
    required this.options,
    this.answer,
    this.explanation,
    required this.score,
  });

  String get typeLabel => switch (type) {
        1 => '单选题',
        2 => '多选题',
        3 => '判断题',
        4 => '简答题',
        _ => '题目',
      };

  factory Question.fromJson(Map<String, dynamic> json) => Question(
        id: json['id'].toString(),
        content: json['content'] as String,
        type: json['type'] as int? ?? 1,
        options: (json['options'] as List<dynamic>?)
                ?.map((e) => e.toString())
                .toList() ??
            [],
        answer: json['answer'] as String?,
        explanation: json['explanation'] as String?,
        score: json['score'] as int? ?? 1,
      );
}

class PracticeQuestion {
  final String id;
  final int type; // 1=Single,2=Multi,3=TrueFalse,4=Short
  final String content;
  final List<String> options;
  final String? knowledgePoint;
  final int difficulty;

  const PracticeQuestion({
    required this.id,
    required this.type,
    required this.content,
    required this.options,
    this.knowledgePoint,
    required this.difficulty,
  });

  String get typeLabel => switch (type) {
        1 => '单选题',
        2 => '多选题',
        3 => '判断题',
        4 => '简答题',
        _ => '题目',
      };

  factory PracticeQuestion.fromJson(Map<String, dynamic> json) =>
      PracticeQuestion(
        id: json['id'].toString(),
        type: json['type'] as int? ?? 1,
        content: json['content'] as String? ?? '',
        options: (json['options'] as List<dynamic>?)
                ?.map((e) => e.toString())
                .toList() ??
            [],
        knowledgePoint: json['knowledgePoint'] as String?,
        difficulty: json['difficulty'] as int? ?? 1,
      );
}

class PracticeResultItem {
  final String questionId;
  final int type;
  final String content;
  final List<String> options;
  final String studentAnswer;
  final String correctAnswer;
  final String? explanation;
  final String? knowledgePoint;
  final int difficulty;
  final bool isCorrect;
  final int score;
  final int maxScore;

  const PracticeResultItem({
    required this.questionId,
    required this.type,
    required this.content,
    required this.options,
    required this.studentAnswer,
    required this.correctAnswer,
    this.explanation,
    this.knowledgePoint,
    required this.difficulty,
    required this.isCorrect,
    required this.score,
    required this.maxScore,
  });

  factory PracticeResultItem.fromJson(Map<String, dynamic> json) =>
      PracticeResultItem(
        questionId: json['questionId'].toString(),
        type: json['type'] as int? ?? 1,
        content: json['content'] as String? ?? '',
        options: (json['options'] as List<dynamic>?)
                ?.map((e) => e.toString())
                .toList() ??
            [],
        studentAnswer: json['studentAnswer'] as String? ?? '',
        correctAnswer: json['correctAnswer'] as String? ?? '',
        explanation: json['explanation'] as String?,
        knowledgePoint: json['knowledgePoint'] as String?,
        difficulty: json['difficulty'] as int? ?? 1,
        isCorrect: json['isCorrect'] as bool? ?? false,
        score: json['score'] as int? ?? 0,
        maxScore: json['maxScore'] as int? ?? 1,
      );
}

class PracticeResult {
  final List<PracticeResultItem> items;
  final int totalScore;
  final int maxScore;
  final int correctCount;

  const PracticeResult({
    required this.items,
    required this.totalScore,
    required this.maxScore,
    required this.correctCount,
  });

  factory PracticeResult.fromJson(Map<String, dynamic> json) => PracticeResult(
        items: (json['items'] as List<dynamic>? ?? [])
            .map((e) => PracticeResultItem.fromJson(e as Map<String, dynamic>))
            .toList(),
        totalScore: json['totalScore'] as int? ?? 0,
        maxScore: json['maxScore'] as int? ?? 0,
        correctCount: json['correctCount'] as int? ?? 0,
      );
}

class PracticeSession {
  final String id;
  final int count;
  final int correctCount;
  final int totalScore;
  final int maxScore;
  final double correctRate;
  final String? typeName;
  final String? knowledgePoint;
  final int? questionType;
  final int? difficulty;
  final int setupCount;
  final DateTime createdAt;

  const PracticeSession({
    required this.id,
    required this.count,
    required this.correctCount,
    required this.totalScore,
    required this.maxScore,
    required this.correctRate,
    this.typeName,
    this.knowledgePoint,
    this.questionType,
    this.difficulty,
    required this.setupCount,
    required this.createdAt,
  });

  factory PracticeSession.fromJson(Map<String, dynamic> json) =>
      PracticeSession(
        id: json['id'].toString(),
        count: json['count'] as int? ?? 0,
        correctCount: json['correctCount'] as int? ?? 0,
        totalScore: json['totalScore'] as int? ?? 0,
        maxScore: json['maxScore'] as int? ?? 0,
        correctRate: (json['correctRate'] as num?)?.toDouble() ?? 0.0,
        typeName: json['typeName'] as String?,
        knowledgePoint: json['knowledgePoint'] as String?,
        questionType: json['questionType'] as int?,
        difficulty: json['difficulty'] as int?,
        setupCount: json['setupCount'] as int? ?? 0,
        createdAt: json['createdAt'] != null
            ? DateTime.tryParse(json['createdAt'] as String) ?? DateTime.now()
            : DateTime.now(),
      );
}
