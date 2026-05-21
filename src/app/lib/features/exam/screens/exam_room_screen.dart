import 'package:flutter/material.dart';

/// 考试答题界面 —— 核心功能，后续根据题型扩展
class ExamRoomScreen extends StatefulWidget {
  final int examId;
  const ExamRoomScreen({super.key, required this.examId});

  @override
  State<ExamRoomScreen> createState() => _ExamRoomScreenState();
}

class _ExamRoomScreenState extends State<ExamRoomScreen> {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('考试中'),
        automaticallyImplyLeading: false,
        actions: [
          TextButton(
            onPressed: _confirmSubmit,
            child: const Text('交卷', style: TextStyle(color: Colors.white)),
          ),
        ],
      ),
      body: const Center(child: Text('题目加载中...')),
    );
  }

  Future<void> _confirmSubmit() async {
    final ok = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('确认交卷'),
        content: const Text('交卷后不可修改，确认吗？'),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('取消')),
          ElevatedButton(onPressed: () => Navigator.pop(ctx, true), child: const Text('确认交卷')),
        ],
      ),
    );
    if (ok == true && mounted) Navigator.pop(context);
  }
}
