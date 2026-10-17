/// A bare list, or the `items` of a paged `{ items: [...] }` payload.
List<dynamic> unwrapItems(dynamic data) {
  if (data is List) return data;
  if (data is Map<String, dynamic> && data['items'] is List) {
    return data['items'] as List<dynamic>;
  }
  return const [];
}
