import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

// Đã sửa: Sử dụng package import thay vì import tương đối
import 'package:cleanai/core/theme/app_theme.dart';
import 'package:cleanai/core/routes/app_router.dart';

void main() {
  runApp(const ProviderScope(child: CleanAIApp()));
}

class CleanAIApp extends StatelessWidget {
  const CleanAIApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp.router(
      title: 'CleanAI',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light,
      darkTheme: AppTheme.dark,
      themeMode: ThemeMode.system,
      routerConfig: appRouter,
    );
  }
}