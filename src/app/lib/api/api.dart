import 'api_client.dart';
import 'models/auth_models.dart';
import 'models/exam_models.dart';
import 'models/question_models.dart';
import 'models/message_models.dart';
import 'models/book_models.dart';
import 'models/profile_models.dart';

final _dio = createDio();

// 鈹€鈹€ Auth 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
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

// 鈹€鈹€ Exams 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
class ExamsApi {
  Future<PagedResult<ExamPaper>> getExams({
    int page = 1,
    int pageSize = 10,
    int? status,
  }) async {
    final res = await _dio.get<Map<String, dynamic>>('/exam-papers', queryParameters: {
      'page': page,
      'pageSize': pageSize,
      if (status != null) 'status': status,
    });
    return PagedResult.fromJson(res.data!, ExamPaper.fromJson);
  }

  Future<ExamPaperDetail> getExamDetail(String id) async {
    final res = await _dio.get<Map<String, dynamic>>('/exam-papers/$id');
    return ExamPaperDetail.fromJson(res.data!);
  }

  Future<void> submitAnswers({
    required String examId,
    required String studentId,
    required List<ExamAnswerItem> answers,
  }) async {
    await _dio.post('/exam-papers/$examId/answers', data: {
      'studentId': studentId,
      'answers': answers.map((a) => a.toJson()).toList(),
    });
  }

  Future<List<StudentExamSummary>> getMyResults() async {
    final res = await _dio.get<List<dynamic>>('/student/my-results');
    return (res.data ?? [])
        .map((e) => StudentExamSummary.fromJson(e as Map<String, dynamic>))
        .toList();
  }
}

// 鈹€鈹€ Practice 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
class PracticeApi {
  Future<List<PracticeQuestion>> getQuestions({
    int count = 10,
    int? type,
    int? difficulty,
    String? knowledgePoint,
  }) async {
    final res = await _dio.get<List<dynamic>>('/practice/questions', queryParameters: {
      'count': count,
      if (type != null) 'type': type,
      if (difficulty != null) 'difficulty': difficulty,
      if (knowledgePoint != null) 'knowledgePoint': knowledgePoint,
    });
    return (res.data ?? [])
        .map((e) => PracticeQuestion.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<PracticeResult> submitAnswers(List<Map<String, String>> answers) async {
    final res = await _dio.post<Map<String, dynamic>>('/practice/submit', data: {
      'answers': answers,
    });
    return PracticeResult.fromJson(res.data!);
  }

  Future<List<PracticeSession>> getHistory() async {
    final res = await _dio.get<List<dynamic>>('/practice/sessions');
    return (res.data ?? [])
        .map((e) => PracticeSession.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  Future<void> saveSession({
    required int count,
    required int correctCount,
    required int totalScore,
    required int maxScore,
    String? typeName,
    String? knowledgePoint,
    int? questionType,
    int? difficulty,
    required int setupCount,
  }) async {
    await _dio.post('/practice/sessions', data: {
      'count': count,
      'correctCount': correctCount,
      'totalScore': totalScore,
      'maxScore': maxScore,
      if (typeName != null) 'typeName': typeName,
      if (knowledgePoint != null) 'knowledgePoint': knowledgePoint,
      if (questionType != null) 'questionType': questionType,
      if (difficulty != null) 'difficulty': difficulty,
      'setupCount': setupCount,
    });
  }

  Future<String> explainQuestion(String questionId) async {
    final res = await _dio.post<Map<String, dynamic>>(
        '/practice/questions/$questionId/explain');
    return res.data?['explanation'] as String? ?? '';
  }
}

// 鈹€鈹€ Questions 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
class QuestionsApi {
  Future<PagedResult<Question>> getQuestions({
    int page = 1,
    int pageSize = 20,
  }) async {
    final res = await _dio.get<Map<String, dynamic>>('/questions', queryParameters: {
      'page': page,
      'pageSize': pageSize,
    });
    return PagedResult.fromJson(res.data!, Question.fromJson);
  }
}

// 鈹€鈹€ Messages 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
class MessagesApi {
  Future<PagedResult<Message>> getInbox({int page = 1, int pageSize = 20}) async {
    final res = await _dio.get<Map<String, dynamic>>('/messages/inbox', queryParameters: {
      'page': page,
      'pageSize': pageSize,
    });
    return PagedResult.fromJson(res.data!, Message.fromJson);
  }

  Future<PagedResult<Message>> getSent({int page = 1, int pageSize = 20}) async {
    final res = await _dio.get<Map<String, dynamic>>('/messages/sent', queryParameters: {
      'page': page,
      'pageSize': pageSize,
    });
    return PagedResult.fromJson(res.data!, Message.fromJson);
  }

  Future<void> markAsRead(String messageId) async {
    await _dio.patch('/messages/$messageId/read');
  }

  Future<void> sendMessage({
    required String recipientId,
    required String subject,
    required String body,
    String? questionContent,
  }) async {
    await _dio.post('/messages', data: {
      'recipientId': recipientId,
      'subject': subject,
      'body': body,
      if (questionContent != null) 'questionContent': questionContent,
    });
  }
}

// 鈹€鈹€ Books 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
class BooksApi {
  Future<PagedResult<Book>> getBooks({String? category, int page = 1, int pageSize = 20}) async {
    final res = await _dio.get<Map<String, dynamic>>('/books', queryParameters: {
      if (category != null) 'category': category,
      'page': page,
      'pageSize': pageSize,
    });
    return PagedResult.fromJson(res.data!, Book.fromJson);
  }

  Future<Book> getBook(String id) async {
    final res = await _dio.get<Map<String, dynamic>>('/books/$id');
    return Book.fromJson(res.data!);
  }
}

// 鈹€鈹€ Favorites 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
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
    return PagedResult.fromJson(res.data!, FavoriteItem.fromJson);
  }
}

// 鈹€鈹€ Profile 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
class ProfileApi {
  Future<UserProfile> getProfile() async {
    final res = await _dio.get<Map<String, dynamic>>('/profile');
    return UserProfile.fromJson(res.data!);
  }

  Future<void> updateProfile({
    String? nickname,
    String? gender,
    String? address,
    String? educationLevel,
    List<String>? interestedSubjects,
  }) async {
    await _dio.patch('/profile', data: {
      if (nickname != null) 'nickname': nickname,
      if (gender != null) 'gender': gender,
      if (address != null) 'address': address,
      if (educationLevel != null) 'educationLevel': educationLevel,
      if (interestedSubjects != null) 'interestedSubjects': interestedSubjects,
    });
  }
}

// 鈹€鈹€ Singletons 鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€鈹€
final authApi = AuthApi();
final examsApi = ExamsApi();
final practiceApi = PracticeApi();
final questionsApi = QuestionsApi();
final messagesApi = MessagesApi();
final booksApi = BooksApi();
final favoritesApi = FavoritesApi();
final profileApi = ProfileApi();
