import type { SupportTicket } from '@/lib/mock-data';

export type TicketStatus = SupportTicket['status'];

export const ADMIN_NAME = 'Ahmed Hassan';

export async function postReply(ticketId: string, message: string, isInternal: boolean) {
  await fetch('/api/support/reply', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ticketId, message, isInternal }),
  });
}

export async function patchStatus(ticketId: string, status: string) {
  await fetch('/api/support/status', {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ticketId, status }),
  });
}

/** `YYYY-MM-DD HH:mm` in local time, matching the mock ticket timestamps. */
export function messageTimestamp(now: Date): string {
  return `${now.toISOString().slice(0, 10)} ${now.toTimeString().slice(0, 5)}`;
}

export function matchesTicketSearch(ticket: SupportTicket, search: string): boolean {
  if (!search) return true;
  const q = search.toLowerCase();
  return [ticket.subject, ticket.number, ticket.gym].some((field) => field.toLowerCase().includes(q));
}

export function openTicketsLabel(openCount: number): string {
  return `${openCount} open ticket${openCount !== 1 ? 's' : ''}`;
}

const quoteCsv = (value: string) => `"${value.replace(/"/g, '""')}"`;

export function ticketsToCsv(tickets: SupportTicket[]): string {
  const header = ['Number', 'Subject', 'Gym', 'Priority', 'Status', 'Assigned', 'Created'];
  const rows = tickets.map((t) => [
    t.number,
    quoteCsv(t.subject),
    quoteCsv(t.gym),
    t.priority,
    t.status,
    quoteCsv(t.assignedTo),
    t.created,
  ]);
  return [header.join(','), ...rows.map((r) => r.join(','))].join('\n');
}

export function downloadTicketsCsv(tickets: SupportTicket[]) {
  const blob = new Blob([ticketsToCsv(tickets)], { type: 'text/csv;charset=utf-8;' });
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = `support-tickets-${new Date().toISOString().slice(0, 10)}.csv`;
  a.click();
  URL.revokeObjectURL(url);
}
