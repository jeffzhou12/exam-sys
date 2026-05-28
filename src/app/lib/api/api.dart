import 'api_client.dart';
import 'models/auth_models.dart';
import 'models/exam_models.dart';
import 'models/question_models.dart';
import 'models/message_models.dart';
import 'models/book_models.dart';

final _dio = createDio();

// ── Auth ─────────────────────────────────────────────────────────────────────
class AuthApi {
  Future<LoginResponse> login(String identifier, String password) async {
    final res = await _dio.post<Map<String, dynamic>>('/auth/login', data: {
      'identifier': identifier,
      'password': password,
    });
    return LoginResponse.fromJson(res.data!);
  }

  Future<LoginResponse> loginWithCode({
    required String target,
    required String code,
    String? tenantId,
    String? role,
    String? nickname,
  }) async {
    final res = await _dio.post<Map<String, dynamic>>('/auth/login-code', data: {
      'target': target,
      'code': code,
      if (tenantId != null) 'tenantId': tenantId,
      if (role != null) 'role': role,
      if (nickname != null) 'nickname': nickname,
    });
    return LoginResponse.fromJson(res.data!);
  }

  Future<void> register({
    required String username,
    required String password,
    required String role,
    required String tenantId,
    String? phoneNumber,
    String? email,
    String? nickname,
  }) async {
    await _dio.post('/auth/register', data: {
      'username': username,
      'password': password,
      'role': role,
      'tenantId': tenantId,
      if (phoneNumber != null && phoneNumber.isNotEmpty) 'phoneNumber': phoneNumber,
      if (email != null && email.isNotEmpty) 'email': email,
      if (nickname != null && nickname.isNotEmpty) 'nickname': nickname,
    });
  }

  Future<String?> sendCode(String target) async {
    final res = await _dio.post<Map<String, dynamic>>('/auth/send-code', data: {
      'target': target,
    });
    return res.data?['devCode'] as String?;
  }

  Future<List<TenantItem>> getPublicTenants() async {
    final res = await _dio.get<List<dynamic>>('/auth/tenants');
    return (res.data ?? [])
        .map((e) => TenantItem.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<void> resetPassword({
    required String target,
    required String code,
    required String newPassword,
  }) async {
    await _dio.post('/auth/reset-password', data: {
      'target': target,
      'code': code,
      'newPassword': newPassword,
    });
  }
}

// ── Exams ─────────────────────────────────────────────────────────────────────
class ExamsApi {
  Future<List<ExamPaper>> getMyExams() async {
    final res = await _dio.get<List<dynamic>>('/exampapers/my');
    return res.data!.map((e) => ExamPaper.fromJson(e as Map<String, dynamic>)).toList();
  }

  Future<ExamPaper> getExam(String id) async {
    final res = await _dio.get<Map<String, dynamic>>('/exampapers/$id');
    return ExamPaper.fromJson(res.data!);
  }

  Future<void> submitAnswers(String examId, List<StudentAnswer> answers) async {
    await _dio.post('/studentanswers', data: {
      'examPaperId': examId,
      'answers': answers.map((a) => a.toJson()).toList(),
    });
  }
}

// ── Questions ─────────────────────────────────────────────────────────────────
class QuestionsApi {
  Future<List<Question>> getPracticeQuestions({
    String? subject,
    int page = 1,
    int pageSize = 20,
  }) async {
    final res = await _dio.get<Map<String, dynamic>>('/questions', queryParameters: {
      if (subject != null) 'subject': subject,
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

// ── Books ────────────────────────────────────────────────────────────────────
class BooksApi {
  Future<List<Book>> getBooks({String? category, int page = 1, int pageSize = 20}) async {
    final res = await _dio.get<Map<String, dynamic>>('/books', queryParameters: {
      if (category != null) 'category': category,
      'page': page,
      'pageSize': pageSize,
    });
    return (res.data!['items'] as List)
        .map((e) => Book.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<Book> getBook(String id) async {
    final res = await _dio.get<Map<String, dynamic>>('/books/$id');
    return Book.fromJson(res.data!);
  }
}

// ── Favorites ─────────────────────────────────────────────────────────────────
class FavoritesApi {
  Future<bool> toggle(int targetType, String targetId) async {
    final res = await _dio.post<Map<String, dynamic>>('/favorites/toggle', data: {
      'targetType': targetType,
      'targetId': targetId,
    });
    return res.data!['isFavorited'] as bool;
  }

  Future<bool> check(int targetType, String targetId) async {
    final res = await _dio.get<Map<String, dynamic>>('/favorites/check', queryParameters: {
      'targetType': targetType,
      'targetId': targetId,
    });
    return res.data!['isFavorited'] as bool;
  }

  Future<PagedResult<FavoriteItem>> getList(int targetType, {int page = 1, int pageSize = 20}) async {
    final res = await _dio.get<Map<String, dynamic>>('/favorites', queryParameters: {
      'targetType': targetType,
      'page': page,
      'pageSize': pageSize,
    });
    final data = res.data!;
    return PagedResult(
      items: (data['items'] as List)
          .map((e) => FavoriteItem.fromJson(e as Map<String, dynamic>))
          .toList(),
      total: data['total'] as int,
      page: data['page'] as int,
      pageSize: data['pageSize'] as int,
    );
  }
}

// ── WrongBook ─────────────────────────────────────────────────────────────────
class WrongBookApi {
  Future<List<WrongQuestion>> getWrongQuestions({String? subject}) async {
    final res = await _dio.get<Map<String, dynamic>>('/wrongbook', queryParameters: {
      if (subject != null) 'subject': subject,
    });
    return (res.data!['items'] as List)
        .map((e) => WrongQuestion.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<int> getCount() async {
    final res = await _dio.get<Map<String, dynamic>>('/wrongbook/count');
    return res.data!['count'] as int;
  }
}

// ── Singletons ────────────────────────────────────────────────────────────────
final authApi = AuthApi();
final examsApi = ExamsApi();
final questionsApi = QuestionsApi();
final messagesApi = MessagesApi();
final booksApi = BooksApi();
final favoritesApi = FavoritesApi();
final wrongBookApi = WrongBookApi();
final questionsApi = QuestionsApi();
final messagesApi = MessagesApi();
