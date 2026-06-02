import 'package:flutter/material.dart';

class AppColors {
  static const primary = Color(0xFF1D4ED8);
  static const primaryLight = Color(0xFF3B82F6);
  static const primaryDark = Color(0xFF1E40AF);
  static const success = Color(0xFF10B981);
  static const warning = Color(0xFFF59E0B);
  static const error = Color(0xFFEF4444);
  static const info = Color(0xFF6366F1);

  // Text
  static const textDeep = Color(0xFF0F172A);
  static const textMain = Color(0xFF1E293B);
  static const textSecondary = Color(0xFF475569);
  static const textWeak = Color(0xFF94A3B8);

  // Border / Background
  static const borderStrong = Color(0xFFE2E8F0);
  static const borderWeak = Color(0xFFF1F5F9);
  static const bgPage = Color(0xFFF8FAFC);
  static const bgCard = Color(0xFFFFFFFF);
}

class AppTheme {
  static const _primaryColor = AppColors.primary;

  static ThemeData get light => ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: _primaryColor,
          brightness: Brightness.light,
          surface: AppColors.bgPage,
        ),
        scaffoldBackgroundColor: AppColors.bgPage,
        appBarTheme: const AppBarTheme(
          elevation: 0,
          centerTitle: true,
          backgroundColor: AppColors.bgCard,
          foregroundColor: AppColors.textMain,
          surfaceTintColor: Colors.transparent,
        ),
        inputDecorationTheme: InputDecorationTheme(
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: const BorderSide(color: AppColors.borderStrong),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: const BorderSide(color: AppColors.borderStrong),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
            borderSide: const BorderSide(color: AppColors.primary, width: 1.5),
          ),
          filled: true,
          fillColor: AppColors.bgCard,
          contentPadding: const EdgeInsets.symmetric(
            horizontal: 16,
            vertical: 14,
          ),
        ),
        cardTheme: CardThemeData(
          elevation: 0,
          color: AppColors.bgCard,
          surfaceTintColor: Colors.transparent,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
            side: const BorderSide(color: AppColors.borderWeak),
          ),
        ),
        elevatedButtonTheme: ElevatedButtonThemeData(
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.primary,
            foregroundColor: Colors.white,
            elevation: 0,
            padding: const EdgeInsets.symmetric(vertical: 14),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(8),
            ),
          ),
        ),
        outlinedButtonTheme: OutlinedButtonThemeData(
          style: OutlinedButton.styleFrom(
            foregroundColor: AppColors.primary,
            side: const BorderSide(color: AppColors.primary),
            padding: const EdgeInsets.symmetric(vertical: 14),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(8),
            ),
          ),
        ),
        textButtonTheme: TextButtonThemeData(
          style: TextButton.styleFrom(
            foregroundColor: AppColors.primary,
          ),
        ),
        chipTheme: ChipThemeData(
          backgroundColor: AppColors.borderWeak,
          selectedColor: AppColors.primary.withAlpha(26),
          labelStyle: const TextStyle(fontSize: 13),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(20),
          ),
          side: BorderSide.none,
        ),
        dividerTheme: const DividerThemeData(
          color: AppColors.borderWeak,
          thickness: 1,
          space: 0,
        ),
        listTileTheme: const ListTileThemeData(
          contentPadding: EdgeInsets.symmetric(horizontal: 16, vertical: 4),
        ),
        navigationBarTheme: NavigationBarThemeData(
          backgroundColor: AppColors.bgCard,
          indicatorColor: AppColors.primary.withAlpha(26),
          labelTextStyle: WidgetStateProperty.all(
            const TextStyle(fontSize: 12),
          ),
        ),
      );

  static ThemeData get dark => ThemeData(
        useMaterial3: true,
        colorScheme: ColorScheme.fromSeed(
          seedColor: _primaryColor,
          brightness: Brightness.dark,
        ),
        appBarTheme: const AppBarTheme(
          elevation: 0,
          centerTitle: true,
        ),
        inputDecorationTheme: InputDecorationTheme(
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(8),
          ),
          contentPadding: const EdgeInsets.symmetric(
            horizontal: 16,
            vertical: 14,
          ),
        ),
        cardTheme: CardThemeData(
          elevation: 0,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
        ),
        elevatedButtonTheme: ElevatedButtonThemeData(
          style: ElevatedButton.styleFrom(
            padding: const EdgeInsets.symmetric(vertical: 14),
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(8),
            ),
          ),
        ),
      );
}
