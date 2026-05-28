import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

class MainShell extends ConsumerStatefulWidget {
  final Widget child;
  const MainShell({super.key, required this.child});

  @override
  ConsumerState<MainShell> createState() => _MainShellState();
}

class _MainShellState extends ConsumerState<MainShell> {
  int _currentIndex = 0;

  static const _tabs = [
    _TabItem(path: '/home',     label: '首页',   icon: Icons.home_outlined,      activeIcon: Icons.home),
    _TabItem(path: '/practice', label: '练习',   icon: Icons.edit_note_outlined,  activeIcon: Icons.edit_note),
    _TabItem(path: '/exams',    label: '考试',   icon: Icons.timer_outlined,      activeIcon: Icons.timer),
    _TabItem(path: '/library',  label: '文库',   icon: Icons.menu_book_outlined,  activeIcon: Icons.menu_book),
    _TabItem(path: '/profile',  label: '我的',   icon: Icons.person_outline,      activeIcon: Icons.person),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: widget.child,
      bottomNavigationBar: NavigationBar(
        selectedIndex: _currentIndex,
        onDestinationSelected: (index) {
          setState(() => _currentIndex = index);
          context.go(_tabs[index].path);
        },
        destinations: _tabs
            .map((t) => NavigationDestination(
                  icon: Icon(t.icon),
                  selectedIcon: Icon(t.activeIcon),
                  label: t.label,
                ))
            .toList(),
      ),
    );
  }
}

class _TabItem {
  final String path;
  final String label;
  final IconData icon;
  final IconData activeIcon;
  const _TabItem({
    required this.path,
    required this.label,
    required this.icon,
    required this.activeIcon,
  });
}
