import 'package:flutter_test/flutter_test.dart';
import 'package:exam_system/api/models/exam_models.dart';
import 'package:exam_system/api/models/question_models.dart';
import 'package:exam_system/api/models/message_models.dart';

void main() {
  group('ExamPaper.fromJson', () {
    test('parses basic fields', () {
      final paper = ExamPaper.fromJson({
        'id': 1,
        'title': '期末考试',
        'duration': 120,
        'totalScore': 100,
        'status': 'ongoing',
      });
      expect(paper.id, 1);
      expect(paper.title, '期末考试');
      expect(paper.status, 'ongoing');
    });
  });

  group('Question.fromJson', () {
    test('parses options correctly', () {
      final q = Question.fromJson({
        'id': 10,
        'content': '下列哪个是正确的？',
        'type': 'single',
        'options': ['A 选项', 'B 选项', 'C 选项', 'D 选项'],
        'score': 2,
      });
      expect(q.options.length, 4);
      expect(q.options[0], 'A 选项');
    });
  });

  group('Message.fromJson', () {
    test('parses isRead and createdAt', () {
      final msg = Message.fromJson({
        'id': 5,
        'title': '测试消息',
        'content': '内容',
        'isRead': false,
        'createdAt': '2026-05-21T08:00:00Z',
      });
      expect(msg.isRead, false);
      expect(msg.createdAt.year, 2026);
    });
  });
}
