import 'package:flutter/material.dart';
import 'package:shimmer/shimmer.dart';
import '../theme/app_colors.dart';

/// Shimmer loading placeholder for content loading states.
class LoadingShimmer extends StatelessWidget {
  final double width;
  final double height;
  final double borderRadius;

  const LoadingShimmer({
    super.key,
    this.width = double.infinity,
    this.height = 16,
    this.borderRadius = 8,
  });

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;
    return Shimmer.fromColors(
      baseColor: isDark ? AppColors.shimmerBaseDark : AppColors.shimmerBase,
      highlightColor:
          isDark ? AppColors.shimmerHighlightDark : AppColors.shimmerHighlight,
      child: Container(
        width: width,
        height: height,
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(borderRadius),
        ),
      ),
    );
  }
}

/// Shimmer placeholder for a card.
class ShimmerCard extends StatelessWidget {
  final double height;

  const ShimmerCard({super.key, this.height = 120});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: LoadingShimmer(
        height: height,
        borderRadius: 12,
      ),
    );
  }
}

/// Shimmer placeholder for a list.
class ShimmerList extends StatelessWidget {
  final int itemCount;
  final double itemHeight;

  const ShimmerList({
    super.key,
    this.itemCount = 5,
    this.itemHeight = 80,
  });

  @override
  Widget build(BuildContext context) {
    return Column(
      children: List.generate(
        itemCount,
        (index) => Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 6),
          child: Row(
            children: [
              LoadingShimmer(
                width: 48,
                height: 48,
                borderRadius: 24,
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    LoadingShimmer(height: 14, width: 150),
                    const SizedBox(height: 8),
                    LoadingShimmer(height: 12, width: 100),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Shimmer placeholder matching stat card dimensions.
class ShimmerStatCard extends StatelessWidget {
  const ShimmerStatCard({super.key});

  @override
  Widget build(BuildContext context) {
    return const LoadingShimmer(
      height: 100,
      borderRadius: 12,
    );
  }
}

/// Full-screen shimmer for initial page loads.
class ShimmerPage extends StatelessWidget {
  const ShimmerPage({super.key});

  @override
  Widget build(BuildContext context) {
    return const SingleChildScrollView(
      padding: EdgeInsets.all(16),
      child: Column(
        children: [
          ShimmerCard(height: 180),
          SizedBox(height: 16),
          Row(
            children: [
              Expanded(child: ShimmerStatCard()),
              SizedBox(width: 12),
              Expanded(child: ShimmerStatCard()),
              SizedBox(width: 12),
              Expanded(child: ShimmerStatCard()),
            ],
          ),
          SizedBox(height: 16),
          ShimmerList(),
        ],
      ),
    );
  }
}
