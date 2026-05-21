class ExamPaper {
  final int id;
  final String title;
  final String? description;
  final int duration; // 分钟
  final int totalScore;
  final DateTime? startTime;
  final DateTime? endTime;
  final String status; // pending | ongoing | submitted | graded

  const ExamPaper({
    required this.id,
    required this.title,
    this.description,
    required this.duration,
    required this.totalScore,
    this.startTime,
    this.endTime,
    required this.status,
  });

  factory ExamPaper.fromJson(Map<String, dynamic> json) => ExamPaper(
        id: json['id'] as int,
        title: json['title'] as String,
        description: json['description'] as String?,
        duration: json['duration'] as int? ?? 0,
        totalScore: json['totalScore'] as int? ?? 100,
        startTime: json['startTime'] != null
            ? DateTime.parse(json['startTime'] as String)
            : null,
        endTime: json['endTime'] != null
            ? DateTime.parse(json['endTime'] as String)
            : null,
        status: json['status'] as String? ?? 'pending',
      );
}

class StudentAnswer {
  final int questionId;
  final String? answer;

  const StudentAnswer({required this.questionId, this.answer});

  Map<String, dynamic> toJson() => {
        'questionId': questionId,
        'answer': answer,
      };
}
