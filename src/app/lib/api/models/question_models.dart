class Question {
  final int id;
  final String content;
  final String type; // single | multiple | judge | short
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

  factory Question.fromJson(Map<String, dynamic> json) => Question(
        id: json['id'] as int,
        content: json['content'] as String,
        type: json['type'] as String? ?? 'single',
        options: (json['options'] as List<dynamic>?)
                ?.map((e) => e as String)
                .toList() ??
            [],
        answer: json['answer'] as String?,
        explanation: json['explanation'] as String?,
        score: json['score'] as int? ?? 1,
      );
}
