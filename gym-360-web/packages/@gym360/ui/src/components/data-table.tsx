'use client';

import { useState, useEffect, type ReactNode } from 'react';
import {
  useReactTable,
  getCoreRowModel,
  getSortedRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  type ColumnDef,
  type SortingState,
  type ColumnFiltersState,
  type VisibilityState,
  type PaginationState,
} from '@tanstack/react-table';
import { cn } from '../lib/utils';
import { DataTablePagination, DataTableToolbar, DesktopTable, MobileCards } from './data-table-parts';
import { displayedRowCount, exportRowsToCsv, searchBinding, serverPageCount } from './data-table-utils';

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
   * Server-side search. When provided, the search box becomes controlled and
   * reports the term to the parent (which fetches + debounces) instead of
   * filtering the loaded rows client-side. Pair with `serverSidePagination`
   * + `totalCount` so search spans the whole dataset, not just one page.
   */
  onSearchChange?: (term: string) => void;
  searchValue?: string;
  /**
   * Controlled current page (1-based) for `serverSidePagination`. Lets the parent
   * own paging so changing search/filters can reset to page 1 without remounting.
   */
  page?: number;
  /**
   * Render a custom mobile card for each row below the md breakpoint.
   * When omitted, the default mobile layout stacks visible columns as
   * label/value pairs.
   */
  renderMobileRow?: (row: TData, index: number) => ReactNode;
}

interface TableStateOptions<TData> {
  data: TData[];
  columns: ColumnDef<TData, unknown>[];
  pageSize: number;
  totalCount?: number;
  serverSidePagination: boolean;
  onPageChange?: (page: number) => void;
  onSortChange?: (sortBy: string, direction: 'asc' | 'desc') => void;
  page?: number;
}

/** In server mode the parent owns the current page (so a search/filter change can jump back to page 1). */
function useControlledPageIndex(
  serverSidePagination: boolean,
  page: number | undefined,
  pagination: PaginationState,
  setPagination: (updater: (prev: PaginationState) => PaginationState) => void,
) {
  // Mirror the parent's page into the table's internal pagination state.
  const controlledPageIndex = serverSidePagination && page ? page - 1 : undefined;
  useEffect(() => {
    if (controlledPageIndex !== undefined && controlledPageIndex !== pagination.pageIndex) {
      setPagination((prev) => ({ ...prev, pageIndex: controlledPageIndex }));
    }
  }, [controlledPageIndex, pagination.pageIndex]);
}

function useDataTableState<TData>({
  data,
  columns,
  pageSize,
  totalCount,
  serverSidePagination,
  onPageChange,
  onSortChange,
  page,
}: TableStateOptions<TData>) {
  const [sorting, setSorting] = useState<SortingState>([]);
  const [columnFilters, setColumnFilters] = useState<ColumnFiltersState>([]);
  const [columnVisibility, setColumnVisibility] = useState<VisibilityState>({});
  const [pagination, setPagination] = useState<PaginationState>({ pageIndex: 0, pageSize });
  const [globalFilter, setGlobalFilter] = useState('');

  useControlledPageIndex(serverSidePagination, page, pagination, setPagination);

  const clientModel = <T,>(model: T) => (serverSidePagination ? undefined : model);

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
      if (!onSortChange || typeof updater !== 'function') return;
      const first = updater(sorting)[0];
      if (first) onSortChange(first.id, first.desc ? 'desc' : 'asc');
    },
    onColumnFiltersChange: setColumnFilters,
    onColumnVisibilityChange: setColumnVisibility,
    onPaginationChange: (updater) => {
      setPagination(updater);
      if (onPageChange && typeof updater === 'function') {
        onPageChange(updater(pagination).pageIndex + 1);
      }
    },
    onGlobalFilterChange: setGlobalFilter,
    getCoreRowModel: getCoreRowModel(),
    getSortedRowModel: clientModel(getSortedRowModel()),
    getFilteredRowModel: clientModel(getFilteredRowModel()),
    getPaginationRowModel: clientModel(getPaginationRowModel()),
    manualPagination: serverSidePagination,
    pageCount: serverPageCount(serverSidePagination, totalCount, pageSize),
  });

  return { table, columnFilters, setColumnFilters, globalFilter, setGlobalFilter };
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
  onSearchChange,
  searchValue,
  page,
  renderMobileRow,
}: DataTableProps<TData>) {
  const { table, ...filters } = useDataTableState({
    data,
    columns,
    pageSize,
    totalCount,
    serverSidePagination,
    onPageChange,
    onSortChange,
    page,
  });
  const rows = table.getRowModel().rows;
  const search = searchBinding({ onSearchChange, searchValue, searchColumn, ...filters });
  const bodyProps = { rows, isLoading, pageSize, emptyMessage, onRowClick };

  return (
    <div className={cn('space-y-4', className)}>
      {/* Toolbar */}
      <DataTableToolbar
        table={table}
        searchPlaceholder={searchPlaceholder}
        search={search}
        toolbar={toolbar}
        enableColumnVisibility={enableColumnVisibility}
        enableExport={enableExport}
        onExport={() => exportRowsToCsv(table, rows)}
      />

      <DesktopTable table={table} columnCount={columns.length} {...bodyProps} />

      <MobileCards table={table} renderMobileRow={renderMobileRow} {...bodyProps} />

      {/* Pagination */}
      <DataTablePagination
        table={table}
        shown={rows.length}
        total={displayedRowCount(table, serverSidePagination, totalCount, data.length)}
      />
    </div>
  );
}
