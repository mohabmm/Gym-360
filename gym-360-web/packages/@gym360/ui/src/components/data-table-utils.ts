import type { ColumnFiltersState, Row, Table } from '@tanstack/react-table';

/** Downloads the given rows' visible-column values as `export.csv`. */
export function exportRowsToCsv<TData>(table: Table<TData>, rows: Row<TData>[]) {
  const headers = table.getVisibleLeafColumns().map((col) => col.id);
  const csvRows = rows.map((row) =>
    headers.map((header) => {
      const cell = row.getAllCells().find((c) => c.column.id === header);
      return cell ? String(cell.getValue() ?? '') : '';
    })
  );
  const csv = [headers.join(','), ...csvRows.map((r) => r.join(','))].join('\n');
  const blob = new Blob([csv], { type: 'text/csv' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = 'export.csv';
  a.click();
  URL.revokeObjectURL(url);
}

export interface SearchBindingOptions {
  onSearchChange?: (term: string) => void;
  searchValue?: string;
  searchColumn?: string;
  columnFilters: ColumnFiltersState;
  setColumnFilters: (updater: (prev: ColumnFiltersState) => ColumnFiltersState) => void;
  globalFilter: string;
  setGlobalFilter: (value: string) => void;
}

/**
 * Resolves which search mode the toolbar input drives:
 * server-side (parent-controlled), a single column filter, or the global filter.
 */
export function searchBinding({
  onSearchChange,
  searchValue,
  searchColumn,
  columnFilters,
  setColumnFilters,
  globalFilter,
  setGlobalFilter,
}: SearchBindingOptions): { value: string; onChange: (value: string) => void } {
  if (onSearchChange) {
    // Server-side search: hand the term to the parent (debounced + refetched
    // there) instead of filtering the loaded page client-side.
    return { value: searchValue ?? '', onChange: onSearchChange };
  }
  if (searchColumn) {
    return {
      value: (columnFilters.find((f) => f.id === searchColumn)?.value as string) ?? '',
      onChange: (value) =>
        setColumnFilters((prev) => [...prev.filter((f) => f.id !== searchColumn), { id: searchColumn, value }]),
    };
  }
  return { value: globalFilter, onChange: setGlobalFilter };
}

/** Total shown in "Showing X of Y": server total when paging server-side, else filtered row count. */
export function displayedRowCount<TData>(
  table: Table<TData>,
  serverSidePagination: boolean,
  totalCount: number | undefined,
  dataLength: number,
): number {
  if (serverSidePagination) return totalCount ?? dataLength;
  return table.getFilteredRowModel().rows.length;
}

export function serverPageCount(serverSidePagination: boolean, totalCount: number | undefined, pageSize: number) {
  return serverSidePagination && totalCount ? Math.ceil(totalCount / pageSize) : undefined;
}
