'use client';

import { useState, useCallback } from 'react';
import { PageHeader, SearchInput } from '@gym360/ui';
import { Download } from 'lucide-react';
import { supportTickets, type SupportTicket } from '@/lib/mock-data';
import {
  ADMIN_NAME,
  downloadTicketsCsv,
  matchesTicketSearch,
  messageTimestamp,
  openTicketsLabel,
  patchStatus,
  postReply,
  type TicketStatus,
} from '@/lib/support';
import { TicketsTable } from '@/components/support/tickets-table';
import { TicketDetailPanel } from '@/components/support/ticket-detail-panel';

/** Local ticket list + selected ticket, with optimistic updates mirrored to both. */
function useTicketState() {
  const [selected, setSelected] = useState<SupportTicket | null>(null);
  const [tickets, setTickets] = useState<SupportTicket[]>(supportTickets);

  const replaceSelected = (updated: SupportTicket) => {
    setTickets((prev) => prev.map((t) => (t.id === updated.id ? updated : t)));
    setSelected(updated);
  };

  return { tickets, selected, setSelected, replaceSelected };
}

export default function SupportPage() {
  const [search, setSearch] = useState('');
  const [reply, setReply] = useState('');
  const [toast, setToast] = useState<string | null>(null);
  const { tickets, selected, setSelected, replaceSelected } = useTicketState();
  const handleSearch = useCallback((v: string) => setSearch(v), []);

  const flash = (msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 2000);
  };

  const addMessage = (isInternal: boolean) => {
    if (!selected || !reply.trim()) return;
    const msg = { sender: ADMIN_NAME, text: reply.trim(), time: messageTimestamp(new Date()), isInternal };
    replaceSelected({ ...selected, messages: [...selected.messages, msg] });
    postReply(selected.id, reply.trim(), isInternal).catch(() => {});
    setReply('');
    flash(isInternal ? 'Internal note added' : 'Reply sent');
  };

  const changeStatus = (newStatus: TicketStatus) => {
    if (!selected) return;
    replaceSelected({ ...selected, status: newStatus });
    patchStatus(selected.id, newStatus).catch(() => {});
    flash(`Status updated to ${newStatus.replace('_', ' ')}`);
  };

  const filtered = tickets.filter((t) => matchesTicketSearch(t, search));
  const openCount = tickets.filter((t) => t.status === 'open').length;

  return (
    <div className="space-y-6">
      {toast && (
        <div className="fixed top-4 right-4 z-50 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white shadow-lg">
          {toast}
        </div>
      )}
      <PageHeader
        title="Support Tickets"
        description={openTicketsLabel(openCount)}
        breadcrumbs={[{ label: 'Dashboard', href: '/' }, { label: 'Support' }]}
        actions={
          <button
            onClick={() => downloadTicketsCsv(tickets)}
            className="inline-flex items-center gap-1.5 h-9 px-3 rounded-lg border border-input bg-background text-sm font-medium hover:bg-muted transition-colors"
          >
            <Download className="h-4 w-4" /> Export
          </button>
        }
      />

      <div className="rounded-xl border bg-card p-5 shadow-sm">
        <div className="mb-4">
          <SearchInput
            value={search}
            onChange={handleSearch}
            placeholder="Search tickets..."
            className="max-w-sm"
          />
        </div>

        <TicketsTable tickets={filtered} onSelect={setSelected} />
      </div>

      <TicketDetailPanel
        ticket={selected}
        onClose={() => setSelected(null)}
        reply={reply}
        onReplyChange={setReply}
        onStatusChange={changeStatus}
        onAddMessage={addMessage}
      />
    </div>
  );
}
