import 'api_client.dart';
import 'models/auth_models.dart';
import 'models/exam_models.dart';
import 'models/question_models.dart';
import 'models/message_models.dart';

final _dio = createDio();

// ── Auth ─────────────────────────────────────────────────────────────────────
class AuthApi {
  Future<LoginResponse> login(String username, String password) async {
    final res = await _dio.post<Map<String, dynamic>>('/auth/login', data: {
      'username': username,
      'password': password,
    });
    return LoginResponse.fromJson(res.data!);
  }
}

// ── Exams ─────────────────────────────────────────────────────────────────────
class ExamsApi {
  Future<List<ExamPaper>> getMyExams() async {
    final res = await _dio.get<List<dynamic>>('/exampapers/my');
    return res.data!.map((e) => ExamPaper.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<ExamPaper> getExam(int id) async {
    final res = await _dio.get<Map<String, dynamic>>('/exampapers/$id');
    return ExamPaper.fromJson(res.data!);
  }

  Future<void> submitAnswers(int examId, List<StudentAnswer> answers) async {
    await _dio.post('/studentanswers', data: {
      'examPaperId': examId,
      'answers': answers.map((a) => a.toJson()).toList(),
    });
  }
}

// ── Questions ─────────────────────────────────────────────────────────────────
class QuestionsApi {
  Future<List<Question>> getPracticeQuestions({
    int? bookId,
    int page = 1,
    int pageSize = 20,
  }) async {
    final res = await _dio.get<Map<String, dynamic>>('/questions', queryParameters: {
      if (bookId != null) 'bookId': bookId,
      'page': page,
      'pageSize': pageSize,
    });
    return (res.data!['items'] as List)
        .map((e) => Question.fromJson(e as Map<String, dynamic>))
        .toList();
  }
}

// ── Messages ───────────────────────────────────────────────────────────────────
class MessagesApi {
  Future<List<Message>> getMessages({int page = 1, int pageSize = 20}) async {
    final res = await _dio.get<Map<String, dynamic>>('/messages', queryParameters: {
      'page': page,
      'pageSize': pageSize,
    });
    return (res.data!['items'] as List)
        .map((e) => Message.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<int> getUnreadCount() async {
    final res = await _dio.get<Map<String, dynamic>>('/messages/unread-count');
    return res.data!['count'] as int;
  }

  Future<void> markAsRead(int messageId) async {
    await _dio.post('/messages/$messageId/read');
  }
}

// ── Singletons ────────────────────────────────────────────────────────────────
final authApi = AuthApi();
final examsApi = ExamsApi();
final questionsApi = QuestionsApi();
final messagesApi = MessagesApi();
