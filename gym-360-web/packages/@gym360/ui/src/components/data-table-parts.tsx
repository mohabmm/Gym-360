'use client';

import type { ReactNode } from 'react';
import { flexRender, type Header, type Row, type Table } from '@tanstack/react-table';
import { cn } from '../lib/utils';
import { ChevronUp, ChevronDown, ChevronsUpDown, ChevronLeft, ChevronRight, Download, Columns3 } from 'lucide-react';

const TOOLBAR_BUTTON = 'inline-flex items-center gap-1.5 rounded-lg border bg-background px-3 py-1.5 text-xs font-medium hover:bg-accent';
const PAGE_BUTTON = 'inline-flex h-8 w-8 items-center justify-center rounded-lg border bg-background hover:bg-accent disabled:opacity-50';

interface DataTableToolbarProps<TData> {
  table: Table<TData>;
  searchPlaceholder: string;
  search: { value: string; onChange: (value: string) => void };
  toolbar?: ReactNode;
  enableColumnVisibility: boolean;
  enableExport: boolean;
  onExport: () => void;
}

export function DataTableToolbar<TData>({
  table,
  searchPlaceholder,
  search,
  toolbar,
  enableColumnVisibility,
  enableExport,
  onExport,
}: DataTableToolbarProps<TData>) {
  return (
    <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between sm:gap-4">
      <div className="flex flex-1 items-center gap-2">
        <input
          type="text"
          placeholder={searchPlaceholder}
          value={search.value}
          onChange={(e) => search.onChange(e.target.value)}
          className="h-9 w-full max-w-sm rounded-lg border border-input bg-background px-3 py-1 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
        />
        {toolbar}
      </div>
      <div className="flex items-center gap-2">
        {enableColumnVisibility && (
          <button
            className={TOOLBAR_BUTTON}
            onClick={() => table.toggleAllColumnsVisible(!table.getIsAllColumnsVisible())}
          >
            <Columns3 className="h-3.5 w-3.5" />
            Columns
          </button>
        )}
        {enableExport && (
          <button className={TOOLBAR_BUTTON} onClick={onExport}>
            <Download className="h-3.5 w-3.5" />
            Export
          </button>
        )}
      </div>
    </div>
  );
}

function SortIcon({ direction }: { direction: false | 'asc' | 'desc' }) {
  if (direction === 'asc') return <ChevronUp className="h-3.5 w-3.5" />;
  if (direction === 'desc') return <ChevronDown className="h-3.5 w-3.5" />;
  return <ChevronsUpDown className="h-3.5 w-3.5" />;
}

function HeaderCell<TData>({ header }: { header: Header<TData, unknown> }) {
  const canSort = header.column.getCanSort();
  return (
    <th
      className={cn(
        'h-10 px-4 text-left font-semibold text-muted-foreground whitespace-nowrap',
        canSort && 'cursor-pointer select-none hover:text-foreground'
      )}
      onClick={header.column.getToggleSortingHandler()}
    >
      <div className="flex items-center gap-1.5">
        {header.isPlaceholder ? null : flexRender(header.column.columnDef.header, header.getContext())}
        {canSort && (
          <span className="text-muted-foreground/60">
            <SortIcon direction={header.column.getIsSorted()} />
          </span>
        )}
      </div>
    </th>
  );
}

interface BodyStateProps<TData> {
  rows: Row<TData>[];
  isLoading: boolean;
  pageSize: number;
  columnCount: number;
  emptyMessage: string;
  onRowClick?: (row: TData) => void;
}

function DesktopBody<TData>({ rows, isLoading, pageSize, columnCount, emptyMessage, onRowClick }: BodyStateProps<TData>) {
  if (isLoading) {
    return (
      <>
        {Array.from({ length: pageSize }).map((_, i) => (
          <tr key={`skeleton-${i}`} className="border-b">
            {Array.from({ length: columnCount }).map((_, j) => (
              <td key={`skeleton-${i}-${j}`} className="px-4 py-3">
                <div className="h-4 w-full animate-pulse rounded bg-muted" />
              </td>
            ))}
          </tr>
        ))}
      </>
    );
  }
  if (rows.length === 0) {
    return (
      <tr>
        <td colSpan={columnCount} className="h-32 text-center text-muted-foreground">
          {emptyMessage}
        </td>
      </tr>
    );
  }
  return (
    <>
      {rows.map((row, index) => (
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
      ))}
    </>
  );
}

/** Desktop table (md and up) — rendered first so .first() locator matches visible row */
export function DesktopTable<TData>({ table, ...bodyProps }: BodyStateProps<TData> & { table: Table<TData> }) {
  return (
    <div className="hidden md:block rounded-xl border bg-card shadow-sm overflow-hidden">
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            {table.getHeaderGroups().map((headerGroup) => (
              <tr key={headerGroup.id} className="border-b bg-muted/50">
                {headerGroup.headers.map((header) => (
                  <HeaderCell key={header.id} header={header} />
                ))}
              </tr>
            ))}
          </thead>
          <tbody>
            <DesktopBody {...bodyProps} />
          </tbody>
        </table>
      </div>
    </div>
  );
}

function headerLabel<TData>(table: Table<TData>, columnId: string): ReactNode {
  const h = table
    .getHeaderGroups()
    .flatMap((g) => g.headers)
    .find((hh) => hh.column.id === columnId);
  if (!h || h.isPlaceholder) return columnId;
  return flexRender(h.column.columnDef.header, h.getContext());
}

interface MobileRowProps<TData> {
  table: Table<TData>;
  row: Row<TData>;
  onRowClick?: (row: TData) => void;
}

/** Default mobile layout: first visible cell as title, remaining cells as label/value pairs. */
function DefaultMobileCard<TData>({ table, row, onRowClick }: MobileRowProps<TData>) {
  const [primary, ...rest] = row.getVisibleCells();
  return (
    <div
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
          {flexRender(primary.column.columnDef.cell, primary.getContext())}
        </div>
      )}
      {rest.length > 0 && (
        <dl className="mt-3 grid grid-cols-2 gap-x-4 gap-y-2 text-xs">
          {rest.map((cell) => (
            <div key={cell.id} className="min-w-0">
              <dt className="truncate font-medium uppercase tracking-wide text-muted-foreground">
                {headerLabel(table, cell.column.id)}
              </dt>
              <dd className="mt-0.5 truncate text-foreground">
                {flexRender(cell.column.columnDef.cell, cell.getContext())}
              </dd>
            </div>
          ))}
        </dl>
      )}
    </div>
  );
}

function MobileSkeletons({ count }: { count: number }) {
  return (
    <>
      {Array.from({ length: count }).map((_, i) => (
        <div key={`m-skel-${i}`} className="rounded-xl border bg-card p-4 shadow-sm">
          <div className="h-4 w-2/3 animate-pulse rounded bg-muted" />
          <div className="mt-3 space-y-2">
            <div className="h-3 w-full animate-pulse rounded bg-muted" />
            <div className="h-3 w-5/6 animate-pulse rounded bg-muted" />
          </div>
        </div>
      ))}
    </>
  );
}

interface MobileCardsProps<TData> extends Omit<BodyStateProps<TData>, 'columnCount'> {
  table: Table<TData>;
  renderMobileRow?: (row: TData, index: number) => ReactNode;
}

function MobileRows<TData>({ table, rows, onRowClick, renderMobileRow }: MobileCardsProps<TData>) {
  return (
    <>
      {rows.map((row, index) =>
        renderMobileRow ? (
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
        ) : (
          <DefaultMobileCard key={row.id} table={table} row={row} onRowClick={onRowClick} />
        ),
      )}
    </>
  );
}

/** Mobile: stacked cards (shown below md) */
export function MobileCards<TData>(props: MobileCardsProps<TData>) {
  const { rows, isLoading, pageSize, emptyMessage } = props;
  if (isLoading) {
    return (
      <div className="md:hidden space-y-2">
        <MobileSkeletons count={Math.min(pageSize, 6)} />
      </div>
    );
  }
  if (rows.length === 0) {
    return (
      <div className="md:hidden space-y-2">
        <div className="rounded-xl border bg-card p-8 text-center text-sm text-muted-foreground shadow-sm">
          {emptyMessage}
        </div>
      </div>
    );
  }
  return (
    <div className="md:hidden space-y-2">
      <MobileRows {...props} />
    </div>
  );
}

export function DataTablePagination<TData>({ table, shown, total }: { table: Table<TData>; shown: number; total: number }) {
  return (
    <div className="flex flex-col items-start justify-between gap-2 text-sm text-muted-foreground sm:flex-row sm:items-center">
      <p>
        Showing {shown} of {total} results
      </p>
      <div className="flex items-center gap-2">
        <button className={PAGE_BUTTON} onClick={() => table.previousPage()} disabled={!table.getCanPreviousPage()}>
          <ChevronLeft className="h-4 w-4" />
        </button>
        <span className="text-sm font-medium">
          Page {table.getState().pagination.pageIndex + 1} of {table.getPageCount()}
        </span>
        <button className={PAGE_BUTTON} onClick={() => table.nextPage()} disabled={!table.getCanNextPage()}>
          <ChevronRight className="h-4 w-4" />
        </button>
      </div>
    </div>
  );
}
