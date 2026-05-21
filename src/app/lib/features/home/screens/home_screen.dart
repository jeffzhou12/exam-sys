import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  static const _entries = [
    _Entry(icon: Icons.assignment, label: '参加考试', path: '/exams', color: Color(0xFF1D4ED8)),
    _Entry(icon: Icons.edit_note,  label: '练习刷题', path: '/practice', color: Color(0xFF059669)),
    _Entry(icon: Icons.mail,       label: '站内消息', path: '/messages', color: Color(0xFFD97706)),
    _Entry(icon: Icons.person,     label: '个人中心', path: '/profile', color: Color(0xFF7C3AED)),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('考试系统')),
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: GridView.count(
          crossAxisCount: 2,
          crossAxisSpacing: 16,
          mainAxisSpacing: 16,
          children: _entries.map((e) => _EntryCard(entry: e)).toList(),
        ),
      ),
    );
  }
}

class _EntryCard extends StatelessWidget {
  final _Entry entry;
  const _EntryCard({required this.entry});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => context.go(entry.path),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(entry.icon, size: 48, color: entry.color),
            const SizedBox(height: 12),
            Text(entry.label,
                style: const TextStyle(fontSize: 16, fontWeight: FontWeight.w500)),
          ],
        ),
      ),
    );
  }
}

class _Entry {
  final IconData icon;
  final String label;
  final String path;
  final Color color;
  const _Entry({required this.icon, required this.label, required this.path, required this.color});
}
