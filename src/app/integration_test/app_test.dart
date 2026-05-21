import 'package:flutter_test/flutter_test.dart';
import 'package:integration_test/integration_test.dart';
import 'package:exam_system/main.dart' as app;

void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();

  testWidgets('Login flow integration test', (tester) async {
    app.main();
    await tester.pumpAndSettle();

    // 未登录 → 应显示登录页
    expect(find.text('登 录'), findsOneWidget);

    // 填写账号密码（集成测试使用测试账号，配置于 .env.test）
    // 此处为占位，实际测试中注入测试用户
  });
}
