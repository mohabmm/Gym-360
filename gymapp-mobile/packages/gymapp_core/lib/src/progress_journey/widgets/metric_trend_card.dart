import 'package:fl_chart/fl_chart.dart';
import 'package:flutter/material.dart';

import '../../theme/app_colors.dart';
import '../../widgets/app_card.dart';
import '../progress_journey.dart';
import '../progress_journey_strings.dart';
import '../progress_metric.dart';

/// Chart period filter.
enum TrendRange {
  month(30),
  quarter(90),
  all(null);

  const TrendRange(this.days);

  final int? days;
}

/// Metric chips + period filter + line chart with a dashed day-one line.
/// Coach-measured points are filled, self-logged points hollow.
class MetricTrendCard extends StatefulWidget {
  final ProgressJourney journey;
  final ProgressJourneyStrings strings;
  final DateTime now;

  /// Tooltip label for self-logged points ("You" for the member, "Client" for the coach).
  final String selfLabel;

  const MetricTrendCard({
    super.key,
    required this.journey,
    required this.strings,
    required this.now,
    required this.selfLabel,
  });

  @override
  State<MetricTrendCard> createState() => _MetricTrendCardState();
}

class _MetricTrendCardState extends State<MetricTrendCard> {
  MetricGroup? _group;
  TrendRange _range = TrendRange.all;

  @override
  Widget build(BuildContext context) {
    final groups = widget.journey.trackedGroups;
    if (groups.isEmpty) return const SizedBox.shrink();
    final group = groups.contains(_group) ? _group! : groups.first;
    final s = widget.strings;
    return AppCard(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Wraps the period selector under the title on narrow screens.
            Wrap(
              alignment: WrapAlignment.spaceBetween,
              crossAxisAlignment: WrapCrossAlignment.center,
              spacing: 8,
              runSpacing: 8,
              children: [
                Text(s.trend, style: Theme.of(context).textTheme.titleSmall?.copyWith(fontWeight: FontWeight.w700)),
                _RangeSelector(range: _range, strings: s, onChanged: (r) => setState(() => _range = r)),
              ],
            ),
            const SizedBox(height: 12),
            _GroupChips(groups: groups, selected: group, strings: s, onSelected: (g) => setState(() => _group = g)),
            const SizedBox(height: 16),
            _TrendChart(
              lines: [for (final m in group.metrics) _TrendLine(m, _inRange(widget.journey.series(m)))],
              baseline: _baselineFor(group),
              strings: s,
              selfLabel: widget.selfLabel,
            ),
          ],
        ),
      ),
    );
  }

  List<MetricPoint> _inRange(List<MetricPoint> points) {
    final days = _range.days;
    if (days == null) return points;
    final from = widget.now.subtract(Duration(days: days));
    return points.where((p) => !p.date.isBefore(from)).toList();
  }

  double? _baselineFor(MetricGroup group) {
    if (group.metrics.length != 1) return null;
    final series = widget.journey.series(group.metrics.single);
    return series.isEmpty ? null : series.first.value;
  }
}

class _RangeSelector extends StatelessWidget {
  final TrendRange range;
  final ProgressJourneyStrings strings;
  final ValueChanged<TrendRange> onChanged;

  const _RangeSelector({required this.range, required this.strings, required this.onChanged});

  @override
  Widget build(BuildContext context) {
    return SegmentedButton<TrendRange>(
      segments: [
        ButtonSegment(value: TrendRange.month, label: Text(strings.range1m)),
        ButtonSegment(value: TrendRange.quarter, label: Text(strings.range3m)),
        ButtonSegment(value: TrendRange.all, label: Text(strings.rangeAll)),
      ],
      selected: {range},
      showSelectedIcon: false,
      style: const ButtonStyle(visualDensity: VisualDensity.compact),
      onSelectionChanged: (s) => onChanged(s.first),
    );
  }
}

class _GroupChips extends StatelessWidget {
  final List<MetricGroup> groups;
  final MetricGroup selected;
  final ProgressJourneyStrings strings;
  final ValueChanged<MetricGroup> onSelected;

  const _GroupChips({required this.groups, required this.selected, required this.strings, required this.onSelected});

  @override
  Widget build(BuildContext context) {
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: [
          for (final g in groups)
            Padding(
              padding: const EdgeInsetsDirectional.only(end: 8),
              child: ChoiceChip(
                label: Text(strings.group(g)),
                selected: g == selected,
                onSelected: (_) => onSelected(g),
              ),
            ),
        ],
      ),
    );
  }
}

class _TrendLine {
  final ProgressMetric metric;
  final List<MetricPoint> points;

  const _TrendLine(this.metric, this.points);
}

class _TrendChart extends StatelessWidget {
  static const _secondLineColor = AppColors.accentGold;

  final List<_TrendLine> lines;
  final double? baseline;
  final ProgressJourneyStrings strings;
  final String selfLabel;

  const _TrendChart({required this.lines, required this.baseline, required this.strings, required this.selfLabel});

  @override
  Widget build(BuildContext context) {
    final drawn = lines.where((l) => l.points.isNotEmpty).toList();
    if (drawn.isEmpty) {
      return SizedBox(height: 180, child: Center(child: Text(strings.baselineSetHint)));
    }
    final primary = Theme.of(context).colorScheme.primary;
    final origin = drawn.map((l) => l.points.first.date).reduce((a, b) => a.isBefore(b) ? a : b);
    final values = [for (final l in drawn) ...l.points.map((p) => p.value)];
    final pad = _padding(values);
    return Column(
      children: [
        SizedBox(
          height: 200,
          child: LineChart(
            LineChartData(
              minY: values.reduce((a, b) => a < b ? a : b) - pad,
              maxY: values.reduce((a, b) => a > b ? a : b) + pad,
              lineBarsData: [
                for (var i = 0; i < drawn.length; i++)
                  _bar(drawn[i], origin, i == 0 ? primary : _secondLineColor),
              ],
              extraLinesData: ExtraLinesData(horizontalLines: [
                if (baseline != null)
                  HorizontalLine(y: baseline!, color: AppColors.grey400, strokeWidth: 1, dashArray: [6, 4]),
              ]),
              gridData: const FlGridData(show: false),
              borderData: FlBorderData(show: false),
              titlesData: _titles(context, origin),
              lineTouchData: _touch(drawn, origin),
            ),
          ),
        ),
        if (drawn.length > 1) _Legend(lines: drawn, colors: [primary, _secondLineColor], strings: strings),
      ],
    );
  }

  static double _padding(List<double> values) {
    final spread = values.reduce((a, b) => a > b ? a : b) - values.reduce((a, b) => a < b ? a : b);
    return spread < 1 ? 1 : spread * 0.2;
  }

  static double _x(DateTime date, DateTime origin) => date.difference(origin).inHours / 24;

  LineChartBarData _bar(_TrendLine line, DateTime origin, Color color) => LineChartBarData(
        spots: [for (final p in line.points) FlSpot(_x(p.date, origin), p.value)],
        isCurved: line.points.length > 2,
        preventCurveOverShooting: true,
        color: color,
        barWidth: 2.5,
        dotData: FlDotData(
          getDotPainter: (spot, _, __, index) => FlDotCirclePainter(
            radius: 4,
            color: line.points[index].coachMeasured ? color : Colors.white,
            strokeColor: color,
            strokeWidth: 2,
          ),
        ),
        belowBarData: BarAreaData(show: lines.length == 1, color: color.withValues(alpha: 0.08)),
      );

  FlTitlesData _titles(BuildContext context, DateTime origin) {
    final style = Theme.of(context).textTheme.labelSmall;
    return FlTitlesData(
      topTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
      rightTitles: const AxisTitles(sideTitles: SideTitles(showTitles: false)),
      leftTitles: AxisTitles(
        sideTitles: SideTitles(
          showTitles: true,
          reservedSize: 36,
          getTitlesWidget: (v, meta) => v == meta.min || v == meta.max
              ? const SizedBox.shrink()
              : Text(v.compact, style: style),
        ),
      ),
      bottomTitles: AxisTitles(
        sideTitles: SideTitles(
          showTitles: true,
          reservedSize: 24,
          getTitlesWidget: (v, meta) => v == meta.min || v == meta.max
              ? Text(strings.shortDate(origin.add(Duration(hours: (v * 24).round()))), style: style)
              : const SizedBox.shrink(),
        ),
      ),
    );
  }

  LineTouchData _touch(List<_TrendLine> drawn, DateTime origin) => LineTouchData(
        touchTooltipData: LineTouchTooltipData(
          tooltipBgColor: AppColors.grey900,
          fitInsideHorizontally: true,
          fitInsideVertically: true,
          getTooltipItems: (spots) => [
            for (final s in spots) _tooltip(drawn[s.barIndex], s.spotIndex),
          ],
        ),
      );

  LineTooltipItem _tooltip(_TrendLine line, int index) {
    final point = line.points[index];
    final who = point.coachMeasured ? strings.coach : selfLabel;
    return LineTooltipItem(
      '${strings.value(MetricValue(point.value, line.metric.unitFor(null)))}\n${strings.shortDate(point.date)} · $who',
      const TextStyle(color: Colors.white, fontWeight: FontWeight.w600, fontSize: 12),
    );
  }
}

class _Legend extends StatelessWidget {
  final List<_TrendLine> lines;
  final List<Color> colors;
  final ProgressJourneyStrings strings;

  const _Legend({required this.lines, required this.colors, required this.strings});

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(top: 8),
      child: Wrap(
        spacing: 16,
        children: [
          for (var i = 0; i < lines.length; i++)
            Row(mainAxisSize: MainAxisSize.min, children: [
              CircleAvatar(radius: 4, backgroundColor: colors[i]),
              const SizedBox(width: 6),
              Text(strings.metric(lines[i].metric), style: Theme.of(context).textTheme.labelSmall),
            ]),
        ],
      ),
    );
  }
}
