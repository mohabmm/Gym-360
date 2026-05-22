'use client';

import { useState, useMemo, type ReactNode } from 'react';
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  flexRender,
  type ColumnDef,
  type SortingState,
  type ColumnFiltersState,
  type VisibilityState,
  type PaginationState,
} from '@tanstack/react-table';
import { cn } from '../lib/utils';
import { ChevronUp, ChevronDown, ChevronsUpDown, ChevronLeft, ChevronRight, Download, Columns3 } from 'lucide-react';

interface DataTableProps<TData> {
  data: TData[];
  columns: ColumnDef<TData, unknown>[];
  searchPlaceholder?: string;
  searchColumn?: string;
  pageSize?: number;
  enableExport?: boolean;
  enableColumnVisibility?: boolean;
  onRowClick?: (row: TData) => void;
  emptyMessage?: string;
  className?: string;
  toolbar?: ReactNode;
  isLoading?: boolean;
  totalCount?: number;
  serverSidePagination?: boolean;
  onPageChange?: (page: number) => void;
  onSortChange?: (sortBy: string, direction: 'asc' | 'desc') => void;
  /**
   * Render a custom mobile card for each row below the md breakpoint.
   * When omitted, the default mobile layout stacks visible columns as
   * label/value pairs.
   */
  renderMobileRow?: (row: TData, index: number) => ReactNode;
}

export function DataTable<TData>({
  data,
  columns,
  searchPlaceholder = 'Search...',
  searchColumn,
  pageSize = 10,
  enableExport = false,
  enableColumnVisibility = false,
  onRowClick,
  emptyMessage = 'No data found.',
  className,
  toolbar,
  isLoading = false,
  totalCount,
  serverSidePagination = false,
  onPageChange,
  onSortChange,
  renderMobileRow,
}: DataTableProps<TData>) {
  const [sorting, setSorting] = useState<SortingState>([]);
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([]);
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({});
  const [pagination, setPagination] = useState<PaginationState>({ pageIndex: 0, pageSize });
  const [globalFilter, setGlobalFilter] = useState('');

  const table = useReactTable({
    data,
    columns,
    state: {
      sorting,
      columnFilters,
      columnVisibility,
      pagination,
      globalFilter,
    },
    onSortingChange: (updater) => {
      setSorting(updater);
      if (onSortChange && typeof updater === 'function') {
        const newSorting = updater(sorting);
        if (newSorting.length > 0 && newSorting[0]) {
          onSortChange(newSorting[0].id, newSorting[0].desc ? 'desc' : 'asc');
        }
      }
    },
    onColumnFiltersChange: setColumnFilters,
    onColumnVisibilityChange: setColumnVisibility,
    onPaginationChange: (updater) => {
      setPagination(updater);
      if (onPageChange && typeof updater === 'function') {
        const newPagination = updater(pagination);
        onPageChange(newPagination.pageIndex + 1);
      }
    },
    onGlobalFilterChange: setGlobalFilter,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: serverSidePagination ? undefined : getSortedRowModel(),
    getFilteredRowModel: serverSidePagination ? undefined : getFilteredRowModel(),
    getPaginationRowModel: serverSidePagination ? undefined : getPaginationRowModel(),
    manualPagination: serverSidePagination,
    pageCount: serverSidePagination && totalCount ? Math.ceil(totalCount / pageSize) : undefined,
  });

  const rows = table.getRowModel().rows;

  const exportToCsv = () => {
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
  };

  const displayedCount = serverSidePagination ? totalCount ?? data.length : table.getFilteredRowModel().rows.length;

  return (
    <div className={cn('space-y-4', className)}>
      {/* Toolbar */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between sm:gap-4">
        <div className="flex flex-1 items-center gap-2">
          <input
            type="text"
            placeholder={searchPlaceholder}
            value={searchColumn ? (columnFilters.find((f) => f.id === searchColumn)?.value as string ?? '') : globalFilter}
            onChange={(e) => {
              if (searchColumn) {
                setColumnFilters((prev) => [
                  ...prev.filter((f) => f.id !== searchColumn),
                  { id: searchColumn, value: e.target.value },
                ]);
              } else {
                setGlobalFilter(e.target.value);
              }
            }}
            className="h-9 w-full max-w-sm rounded-lg border border-input bg-background px-3 py-1 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
          />
          {toolbar}
        </div>
        <div className="flex items-center gap-2">
          {enableColumnVisibility && (
            <button
              className="inline-flex items-center gap-1.5 rounded-lg border bg-background px-3 py-1.5 text-xs font-medium hover:bg-accent"
              onClick={() => {
                const allVisible = table.getIsAllColumnsVisible();
                table.toggleAllColumnsVisible(!allVisible);
              }}
            >
              <Columns3 className="h-3.5 w-3.5" />
              Columns
            </button>
          )}
          {enableExport && (
            <button
              className="inline-flex items-center gap-1.5 rounded-lg border bg-background px-3 py-1.5 text-xs font-medium hover:bg-accent"
              onClick={exportToCsv}
            >
              <Download className="h-3.5 w-3.5" />
              Export
            </button>
          )}
        </div>
      </div>

      {/* Desktop table (md and up) — rendered first so .first() locator matches visible row */}
      <div className="hidden md:block rounded-xl border bg-card shadow-sm overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              {table.getHeaderGroups().map((headerGroup) => (
                <tr key={headerGroup.id} className="border-b bg-muted/50">
                  {headerGroup.headers.map((header) => (
                    <th
                      key={header.id}
                      className={cn(
                        'h-10 px-4 text-left font-semibold text-muted-foreground whitespace-nowrap',
                        header.column.getCanSort() && 'cursor-pointer select-none hover:text-foreground'
                      )}
                      onClick={header.column.getToggleSortingHandler()}
                    >
                      <div className="flex items-center gap-1.5">
                        {header.isPlaceholder
                          ? null
                          : flexRender(header.column.columnDef.header, header.getContext())}
                        {header.column.getCanSort() && (
                          <span className="text-muted-foreground/60">
                            {header.column.getIsSorted() === 'asc' ? (
                              <ChevronUp className="h-3.5 w-3.5" />
                            ) : header.column.getIsSorted() === 'desc' ? (
                              <ChevronDown className="h-3.5 w-3.5" />
                            ) : (
                              <ChevronsUpDown className="h-3.5 w-3.5" />
                            )}
                          </span>
                        )}
                      </div>
                    </th>
                  ))}
                </tr>
              ))}
            </thead>
            <tbody>
              {isLoading ? (
                Array.from({ length: pageSize }).map((_, i) => (
                  <tr key={`skeleton-${i}`} className="border-b">
                    {columns.map((_, j) => (
                      <td key={`skeleton-${i}-${j}`} className="px-4 py-3">
                        <div className="h-4 w-full animate-pulse rounded bg-muted" />
                      </td>
                    ))}
                  </tr>
                ))
              ) : rows.length === 0 ? (
                <tr>
                  <td colSpan={columns.length} className="h-32 text-center text-muted-foreground">
                    {emptyMessage}
                  </td>
                </tr>
              ) : (
                rows.map((row, index) => (
                  <tr
                    key={row.id}
                    className={cn(
                      'border-b transition-colors',
                      index % 2 === 1 && 'bg-muted/20',
                      onRowClick && 'cursor-pointer hover:bg-muted/50'
                    )}
                    onClick={() => onRowClick?.(row.original)}
                  >
                    {row.getVisibleCells().map((cell) => (
                      <td key={cell.id} className="px-4 py-3 whitespace-nowrap">
                        {flexRender(cell.column.columnDef.cell, cell.getContext())}
                      </td>
                    ))}
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Mobile: stacked cards (shown below md) */}
      <div className="md:hidden space-y-2">
        {isLoading ? (
          Array.from({ length: Math.min(pageSize, 6) }).map((_, i) => (
            <div
              key={`m-skel-${i}`}
              className="rounded-xl border bg-card p-4 shadow-sm"
            >
              <div className="h-4 w-2/3 animate-pulse rounded bg-muted" />
              <div className="mt-3 space-y-2">
                <div className="h-3 w-full animate-pulse rounded bg-muted" />
                <div className="h-3 w-5/6 animate-pulse rounded bg-muted" />
              </div>
            </div>
          ))
        ) : rows.length === 0 ? (
          <div className="rounded-xl border bg-card p-8 text-center text-sm text-muted-foreground shadow-sm">
            {emptyMessage}
          </div>
        ) : (
          rows.map((row, index) => {
            if (renderMobileRow) {
              return (
                <button
                  key={row.id}
                  type="button"
                  onClick={() => onRowClick?.(row.original)}
                  className={cn(
                    'block w-full rounded-xl border bg-card p-0 text-start shadow-sm transition-colors',
                    onRowClick && 'hover:bg-muted/40 active:bg-muted/60',
                  )}
                >
                  {renderMobileRow(row.original, index)}
                </button>
              );
            }

            const visibleCells = row.getVisibleCells();
            const [primary, ...rest] = visibleCells;
            const headerFor = (cellId: string): ReactNode => {
              const h = table
                .getHeaderGroups()
                .flatMap((g) => g.headers)
                .find((hh) => hh.column.id === cellId);
              if (!h || h.isPlaceholder) return cellId;
              return flexRender(h.column.columnDef.header, h.getContext());
            };

            return (
              <div
                key={row.id}
                role={onRowClick ? 'button' : undefined}
                tabIndex={onRowClick ? 0 : undefined}
                onClick={() => onRowClick?.(row.original)}
                className={cn(
                  'rounded-xl border bg-card p-4 shadow-sm transition-colors',
                  onRowClick && 'cursor-pointer hover:bg-muted/40',
                )}
              >
                {primary && (
                  <div className="text-sm font-semibold text-foreground">
                    {flexRender(
                      primary.column.columnDef.cell,
                      primary.getContext(),
                    )}
                  </div>
                )}
                {rest.length > 0 && (
                  <dl className="mt-3 grid grid-cols-2 gap-x-4 gap-y-2 text-xs">
                    {rest.map((cell) => (
                      <div key={cell.id} className="min-w-0">
                        <dt className="truncate font-medium uppercase tracking-wide text-muted-foreground">
                          {headerFor(cell.column.id)}
                        </dt>
                        <dd className="mt-0.5 truncate text-foreground">
                          {flexRender(
                            cell.column.columnDef.cell,
                            cell.getContext(),
                          )}
                        </dd>
                      </div>
                    ))}
                  </dl>
                )}
              </div>
            );
          })
        )}
      </div>

      {/* Pagination */}
      <div className="flex flex-col items-start justify-between gap-2 text-sm text-muted-foreground sm:flex-row sm:items-center">
        <p>
          Showing {rows.length} of {displayedCount} results
        </p>
        <div className="flex items-center gap-2">
          <button
            className="inline-flex h-8 w-8 items-center justify-center rounded-lg border bg-background hover:bg-accent disabled:opacity-50"
            onClick={() => table.previousPage()}
            disabled={!table.getCanPreviousPage()}
          >
            <ChevronLeft className="h-4 w-4" />
          </button>
          <span className="text-sm font-medium">
            Page {table.getState().pagination.pageIndex + 1} of {table.getPageCount()}
          </span>
          <button
            className="inline-flex h-8 w-8 items-center justify-center rounded-lg border bg-background hover:bg-accent disabled:opacity-50"
            onClick={() => table.nextPage()}
            disabled={!table.getCanNextPage()}
          >
            <ChevronRight className="h-4 w-4" />
          </button>
        </div>
      </div>
    </div>
  );
}
