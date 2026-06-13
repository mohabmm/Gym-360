import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';

/// Weekly bar chart for activity/workout tracking.
class WeeklyBarChart extends StatelessWidget {
  final List<double> values;
  final List<String> labels;
  final Color? barColor;
  final double height;

  const WeeklyBarChart({
    super.key,
    required this.values,
    required this.labels,
    this.barColor,
    this.height = 180,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final color = barColor ?? theme.colorScheme.primary;
    final maxY =
        values.isEmpty ? 10.0 : (values.reduce((a, b) => a > b ? a : b) * 1.2);

    return SizedBox(
      height: height,
      child: BarChart(
        BarChartData(
          alignment: BarChartAlignment.spaceAround,
          maxY: maxY,
          barTouchData: BarTouchData(
            enabled: true,
            touchTooltipData: BarTouchTooltipData(
              getTooltipItem: (group, groupIndex, rod, rodIndex) {
                return BarTooltipItem(
                  rod.toY.toInt().toString(),
                  TextStyle(color: color, fontWeight: FontWeight.w600),
                );
              },
            ),
          ),
          titlesData: FlTitlesData(
            show: true,
            bottomTitles: AxisTitles(
              sideTitles: SideTitles(
                showTitles: true,
                getTitlesWidget: (value, meta) {
                  final index = value.toInt();
                  if (index < 0 || index >= labels.length) {
                    return const SizedBox.shrink();
                  }
                  return Text(
                    labels[index],
                    style: theme.textTheme.labelSmall,
                  );
                },
              ),
            ),
            leftTitles:
                const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            topTitles:
                const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            rightTitles:
                const AxisTitles(sideTitles: SideTitles(showTitles: false)),
          ),
          gridData: const FlGridData(show: false),
          borderData: FlBorderData(show: false),
          barGroups: List.generate(values.length, (index) {
            return BarChartGroupData(
              x: index,
              barRods: [
                BarChartRodData(
                  toY: values[index],
                  color: color,
                  width: 24,
                  borderRadius: const BorderRadius.vertical(
                    top: Radius.circular(6),
                  ),
                ),
              ],
            );
          }),
        ),
      ),
    );
  }
}

/// Line chart for trend visualization (weight, progress, etc).
class TrendLineChart extends StatelessWidget {
  final List<FlSpot> spots;
  final Color? lineColor;
  final double height;
  final bool showDots;
  final bool showArea;
  final List<String>? bottomLabels;

  const TrendLineChart({
    super.key,
    required this.spots,
    this.lineColor,
    this.height = 200,
    this.showDots = true,
    this.showArea = true,
    this.bottomLabels,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final color = lineColor ?? theme.colorScheme.primary;

    if (spots.isEmpty) {
      return SizedBox(
        height: height,
        child: const Center(child: Text('No data')),
      );
    }

    return SizedBox(
      height: height,
      child: LineChart(
        LineChartData(
          lineBarsData: [
            LineChartBarData(
              spots: spots,
              isCurved: true,
              color: color,
              barWidth: 3,
              dotData: FlDotData(show: showDots),
              belowBarData: showArea
                  ? BarAreaData(
                      show: true,
                      color: color.withOpacity(0.1),
                    )
                  : BarAreaData(show: false),
            ),
          ],
          titlesData: FlTitlesData(
            show: true,
            bottomTitles: AxisTitles(
              sideTitles: SideTitles(
                showTitles: bottomLabels != null,
                getTitlesWidget: (value, meta) {
                  final index = value.toInt();
                  if (bottomLabels == null ||
                      index < 0 ||
                      index >= bottomLabels!.length) {
                    return const SizedBox.shrink();
                  }
                  return Text(
                    bottomLabels![index],
                    style: theme.textTheme.labelSmall,
                  );
                },
              ),
            ),
            leftTitles:
                const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            topTitles:
                const AxisTitles(sideTitles: SideTitles(showTitles: false)),
            rightTitles:
                const AxisTitles(sideTitles: SideTitles(showTitles: false)),
          ),
          gridData: FlGridData(
            show: true,
            drawVerticalLine: false,
            horizontalInterval: 1,
            getDrawingHorizontalLine: (value) => FlLine(
              color: theme.dividerColor.withOpacity(0.3),
              strokeWidth: 1,
            ),
          ),
          borderData: FlBorderData(show: false),
          lineTouchData: LineTouchData(
            touchTooltipData: LineTouchTooltipData(
              getTooltipItems: (touchedSpots) {
                return touchedSpots.map((spot) {
                  return LineTooltipItem(
                    spot.y.toStringAsFixed(1),
                    TextStyle(
                      color: color,
                      fontWeight: FontWeight.w600,
                    ),
                  );
                }).toList();
              },
            ),
          ),
        ),
      ),
    );
  }
}

/// Donut chart for distribution data.
class DonutChart extends StatelessWidget {
  final List<DonutChartSection> sections;
  final double size;
  final String? centerText;

  const DonutChart({
    super.key,
    required this.sections,
    this.size = 150,
    this.centerText,
  });

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: size,
      height: size,
      child: Stack(
        alignment: Alignment.center,
        children: [
          PieChart(
            PieChartData(
              sections: sections
                  .map((s) => PieChartSectionData(
                        value: s.value,
                        color: s.color,
                        title: '',
                        radius: size * 0.2,
                      ))
                  .toList(),
              centerSpaceRadius: size * 0.25,
              sectionsSpace: 2,
            ),
          ),
          if (centerText != null)
            Text(
              centerText!,
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.w700,
                  ),
            ),
        ],
      ),
    );
  }
}

class DonutChartSection {
  final double value;
  final Color color;
  final String label;

  const DonutChartSection({
    required this.value,
    required this.color,
    required this.label,
  });
}
