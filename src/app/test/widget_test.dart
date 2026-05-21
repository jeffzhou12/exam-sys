import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:exam_system/app.dart';

void main() {
  testWidgets('App smoke test — renders without crash', (tester) async {
    await tester.pumpWidget(
      const ProviderScope(child: ExamSystemApp()),
    );
    // 未登录时应看到登录页
    expect(find.text('考试系统'), findsWidgets);
  });
}
