import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:cleanai/main.dart';

void main() {
  testWidgets('App smoke test', (WidgetTester tester) async {
    await tester.pumpWidget(const ProviderScope(child: CleanAIApp()));
    await tester.pump(const Duration(seconds: 3));
    // The splash screen should be rendered initially
    expect(find.byType(MaterialApp), findsOneWidget);
  });
}
