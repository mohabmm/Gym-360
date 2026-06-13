import 'package:freezed_annotation/freezed_annotation.dart';

part 'pagination_model.freezed.dart';
part 'pagination_model.g.dart';

@freezed
class PaginationMeta with _$PaginationMeta {
  const PaginationMeta._();

  const factory PaginationMeta({
    required int totalCount,
    required int pageSize,
    required int currentPage,
    required int totalPages,
  }) = _PaginationMeta;

  factory PaginationMeta.fromJson(Map<String, dynamic> json) =>
      _$PaginationMetaFromJson(json);

  bool get hasNextPage => currentPage < totalPages;
  bool get hasPreviousPage => currentPage > 1;
}

@freezed
class PaginatedResult<T> with _$PaginatedResult<T> {
  const factory PaginatedResult({
    required List<T> items,
    required PaginationMeta meta,
  }) = _PaginatedResult<T>;
}

class PaginationParams {
  final int page;
  final int pageSize;
  final String? sortBy;
  final bool sortDescending;

  const PaginationParams({
    this.page = 1,
    this.pageSize = 20,
    this.sortBy,
    this.sortDescending = false,
  });

  Map<String, dynamic> toQueryParams() => {
        'page': page.toString(),
        'pageSize': pageSize.toString(),
        if (sortBy != null) 'sortBy': sortBy!,
        if (sortDescending) 'sortDesc': 'true',
      };
}
