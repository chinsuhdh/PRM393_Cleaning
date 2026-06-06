import 'package:flutter/material.dart';
import 'package:google_fonts/google_fonts.dart';
import 'app_colors.dart';

class AppTheme {
  AppTheme._();

  static ThemeData get light => ThemeData(
        useMaterial3: true,
        colorScheme: const ColorScheme(
          brightness: Brightness.light,
          primary: kPrimary,
          onPrimary: kOnPrimary,
          primaryContainer: kPrimaryContainer,
          onPrimaryContainer: kOnPrimaryContainer,
          secondary: kSecondary,
          onSecondary: kOnSecondary,
          secondaryContainer: kSecondaryContainer,
          onSecondaryContainer: kOnSecondaryContainer,
          tertiary: kTertiary,
          onTertiary: kOnTertiary,
          tertiaryContainer: kTertiaryContainer,
          onTertiaryContainer: kOnTertiaryContainer,
          error: kError,
          onError: kOnError,
          errorContainer: kErrorContainer,
          onErrorContainer: kOnErrorContainer,
          surface: kSurfaceLight,
          onSurface: kOnSurfaceLight,
          surfaceContainerHighest: kSurfaceVariantLight,
          onSurfaceVariant: kOnSurfaceVariantLight,
        ),
        textTheme: GoogleFonts.interTextTheme(),
        appBarTheme: const AppBarTheme(
          backgroundColor: kSurfaceLight,
          foregroundColor: kOnSurfaceLight,
          elevation: 0,
          scrolledUnderElevation: 1,
        ),
        navigationBarTheme: NavigationBarThemeData(
          backgroundColor: kSurfaceLight,
          indicatorColor: kPrimaryContainer,
          labelTextStyle: WidgetStateProperty.resolveWith((states) {
            if (states.contains(WidgetState.selected)) {
              return GoogleFonts.inter(
                  fontWeight: FontWeight.w600, fontSize: 12, color: kPrimary);
            }
            return GoogleFonts.inter(
                fontSize: 12, color: kOnSurfaceVariantLight);
          }),
        ),
        cardTheme: CardThemeData(
          elevation: 2,
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        ),
        inputDecorationTheme: InputDecorationTheme(
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
          ),
          filled: false,
        ),
        elevatedButtonTheme: ElevatedButtonThemeData(
          style: ElevatedButton.styleFrom(
            backgroundColor: kPrimary,
            foregroundColor: kOnPrimary,
            shape:
                RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            minimumSize: const Size.fromHeight(56),
            textStyle: GoogleFonts.inter(
                fontWeight: FontWeight.w600, fontSize: 16),
          ),
        ),
        filledButtonTheme: FilledButtonThemeData(
          style: FilledButton.styleFrom(
            shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12)),
            minimumSize: const Size.fromHeight(56),
            textStyle: GoogleFonts.inter(
                fontWeight: FontWeight.w600, fontSize: 16),
          ),
        ),
      );

  static ThemeData get dark => ThemeData(
        useMaterial3: true,
        colorScheme: const ColorScheme(
          brightness: Brightness.dark,
          primary: kPrimary,
          onPrimary: kOnPrimary,
          primaryContainer: kPrimaryContainer,
          onPrimaryContainer: kOnPrimaryContainer,
          secondary: kSecondary,
          onSecondary: kOnSecondary,
          secondaryContainer: kSecondaryContainer,
          onSecondaryContainer: kOnSecondaryContainer,
          tertiary: kTertiary,
          onTertiary: kOnTertiary,
          tertiaryContainer: kTertiaryContainer,
          onTertiaryContainer: kOnTertiaryContainer,
          error: kError,
          onError: kOnError,
          errorContainer: kErrorContainer,
          onErrorContainer: kOnErrorContainer,
          surface: kSurfaceDark,
          onSurface: kOnSurfaceDark,
          surfaceContainerHighest: kSurfaceVariantDark,
          onSurfaceVariant: kOnSurfaceVariantDark,
        ),
        textTheme: GoogleFonts.interTextTheme(ThemeData.dark().textTheme),
        appBarTheme: const AppBarTheme(
          backgroundColor: kSurfaceDark,
          foregroundColor: kOnSurfaceDark,
          elevation: 0,
          scrolledUnderElevation: 1,
        ),
        navigationBarTheme: NavigationBarThemeData(
          backgroundColor: kSurfaceDark,
          indicatorColor: kPrimaryContainer,
          labelTextStyle: WidgetStateProperty.resolveWith((states) {
            if (states.contains(WidgetState.selected)) {
              return GoogleFonts.inter(
                  fontWeight: FontWeight.w600, fontSize: 12, color: kPrimary);
            }
            return GoogleFonts.inter(
                fontSize: 12, color: kOnSurfaceVariantDark);
          }),
        ),
        cardTheme: CardThemeData(
          elevation: 2,
          shape:
              RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        ),
        inputDecorationTheme: InputDecorationTheme(
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
          ),
        ),
        elevatedButtonTheme: ElevatedButtonThemeData(
          style: ElevatedButton.styleFrom(
            backgroundColor: kPrimary,
            foregroundColor: kOnPrimary,
            shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12)),
            minimumSize: const Size.fromHeight(56),
          ),
        ),
      );
}
