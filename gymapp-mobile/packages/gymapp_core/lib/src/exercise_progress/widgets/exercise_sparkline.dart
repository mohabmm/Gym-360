import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';

/// Tiny trend line of one exercise's lead number (oldest → newest). Hidden below two points.
class ExerciseSparkline extends StatelessWidget {
  final List<double> values;
  final Color color;
  final double height;

  const ExerciseSparkline({super.key, required this.values, required this.color, this.height = 36});

  @override
  Widget build(BuildContext context) {
    if (values.length < 2) return SizedBox(height: height);
    final spots = [for (var i = 0; i < values.length; i++) FlSpot(i.toDouble(), values[i])];
    return SizedBox(
      height: height,
      // Time always runs left → right, also in Arabic.
      child: Directionality(
        textDirection: TextDirection.ltr,
        child: LineChart(
          LineChartData(
            lineBarsData: [
              LineChartBarData(
                spots: spots,
                isCurved: true,
                color: color,
                barWidth: 2.5,
                dotData: FlDotData(show: values.length <= 8),
                belowBarData: BarAreaData(show: true, color: color.withValues(alpha: 0.1)),
              ),
            ],
            titlesData: const FlTitlesData(show: false),
            gridData: const FlGridData(show: false),
            borderData: FlBorderData(show: false),
            lineTouchData: const LineTouchData(enabled: false),
          ),
        ),
      ),
    );
  }
}
