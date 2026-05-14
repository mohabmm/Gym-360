'use client';

import { useState, useCallback } from 'react';
import { PageHeader, StatusBadge, SearchInput, SlideOverPanel } from '@gymapp/ui';
import { Download } from 'lucide-react';
import { supportTickets, type SupportTicket } from '@/lib/mock-data';

async function postReply(ticketId: string, message: string, isInternal: boolean) {
  await fetch('/api/support/reply', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ticketId, message, isInternal }),
  });
}

async function patchStatus(ticketId: string, status: string) {
  await fetch('/api/support/status', {
    method: 'PATCH',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ticketId, status }),
  });
}

const ADMIN_NAME = 'Ahmed Hassan';

export default function SupportPage() {
  const [search, setSearch] = useState('');
  const [selected, setSelected] = useState<SupportTicket | null>(null);
  const [reply, setReply] = useState('');
  const [tickets, setTickets] = useState<SupportTicket[]>(supportTickets);
  const [toast, setToast] = useState<string | null>(null);
  const handleSearch = useCallback((v: string) => setSearch(v), []);

  const flash = (msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 2000);
  };

  const addMessage = (isInternal: boolean) => {
    if (!selected || !reply.trim()) return;
    const now = new Date();
    const stamp = `${now.toISOString().slice(0, 10)} ${now.toTimeString().slice(0, 5)}`;
    const msg = { sender: ADMIN_NAME, text: reply.trim(), time: stamp, isInternal };
    const updated = { ...selected, messages: [...selected.messages, msg] };
    setTickets((prev) => prev.map((t) => (t.id === selected.id ? updated : t)));
    setSelected(updated);
    postReply(selected.id, reply.trim(), isInternal).catch(() => {});
    setReply('');
    flash(isInternal ? 'Internal note added' : 'Reply sent');
  };

  const exportTicketsCsv = () => {
    const header = ['Number', 'Subject', 'Gym', 'Priority', 'Status', 'Assigned', 'Created'];
    const rows = tickets.map((t) => [
      t.number,
      `"${t.subject.replace(/"/g, '""')}"`,
      `"${t.gym.replace(/"/g, '""')}"`,
      t.priority,
      t.status,
      `"${t.assignedTo.replace(/"/g, '""')}"`,
      t.created,
    ]);
    const csv = [header.join(','), ...rows.map((r) => r.join(','))].join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `support-tickets-${new Date().toISOString().slice(0, 10)}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const filtered = tickets.filter(
    (t) =>
      !search ||
      t.subject.toLowerCase().includes(search.toLowerCase()) ||
      t.number.toLowerCase().includes(search.toLowerCase()) ||
      t.gym.toLowerCase().includes(search.toLowerCase()),
  );

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
        description={`${openCount} open ticket${openCount !== 1 ? 's' : ''}`}
        breadcrumbs={[{ label: 'Dashboard', href: '/' }, { label: 'Support' }]}
        actions={
          <button
            onClick={exportTicketsCsv}
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

        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-left text-muted-foreground">
                <th className="pb-2 font-medium">Ticket</th>
                <th className="pb-2 font-medium">Subject</th>
                <th className="pb-2 font-medium">Gym</th>
                <th className="pb-2 font-medium">Priority</th>
                <th className="pb-2 font-medium">Status</th>
                <th className="pb-2 font-medium">Assigned</th>
                <th className="pb-2 font-medium">Created</th>
                <th className="pb-2 font-medium w-10"></th>
              </tr>
            </thead>
            <tbody>
              {filtered.map((t) => (
                <tr
                  key={t.id}
                  className="border-b border-border/50 hover:bg-muted/30 cursor-pointer transition-colors"
                  onClick={() => setSelected(t)}
                >
                  <td className="py-2.5 font-mono text-xs">{t.number}</td>
                  <td className="py-2.5 font-medium">{t.subject}</td>
                  <td className="py-2.5 text-muted-foreground">{t.gym}</td>
                  <td className="py-2.5">
                    <StatusBadge variant={t.priority} />
                  </td>
                  <td className="py-2.5">
                    <StatusBadge variant={t.status} />
                  </td>
                  <td className="py-2.5 text-muted-foreground">{t.assignedTo}</td>
                  <td className="py-2.5 text-muted-foreground">{t.created}</td>
                  <td className="py-2.5">
                    <button className="h-7 px-2 text-xs rounded-md hover:bg-muted transition-colors">
                      View
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <SlideOverPanel
        open={!!selected}
        onClose={() => setSelected(null)}
        title={selected ? `${selected.number} — ${selected.subject}` : ''}
        size="lg"
        footer={
          <>
            <button
              className="h-9 px-4 rounded-lg border border-input bg-background text-sm font-medium hover:bg-muted transition-colors disabled:opacity-50"
              onClick={() => addMessage(true)}
              disabled={!reply.trim()}
            >
              Internal Note
            </button>
            <button
              className="h-9 px-4 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 transition-colors disabled:opacity-50"
              onClick={() => addMessage(false)}
              disabled={!reply.trim()}
            >
              Reply
            </button>
          </>
        }
      >
        {selected && (
          <div className="space-y-4">
            <div className="flex items-center gap-2 flex-wrap">
              <StatusBadge variant={selected.priority} />
              <StatusBadge variant={selected.status} />
              <span className="text-sm text-muted-foreground">{selected.gym} · {selected.assignedTo}</span>
            </div>

            <div>
              <p className="text-xs font-medium text-muted-foreground mb-1.5">Status</p>
              <select
                value={selected.status}
                onChange={(e) => {
                  const newStatus = e.target.value;
                  const updated = { ...selected, status: newStatus as typeof selected.status };
                  setTickets((prev) => prev.map((t) => (t.id === selected.id ? updated : t)));
                  setSelected(updated);
                  patchStatus(selected.id, newStatus).catch(() => {});
                  flash(`Status updated to ${newStatus.replace('_', ' ')}`);
                }}
                className="h-9 rounded-lg border border-input bg-background px-3 text-sm focus:outline-none focus:ring-1 focus:ring-ring w-[160px]"
              >
                <option value="open">Open</option>
                <option value="in_progress">In Progress</option>
                <option value="resolved">Resolved</option>
                <option value="closed">Closed</option>
              </select>
            </div>

            <div className="space-y-3">
              <p className="text-xs font-medium text-muted-foreground">Conversation</p>
              {selected.messages.map((m, i) => (
                <div
                  key={i}
                  className={`rounded-lg p-3 text-sm ${
                    m.isInternal
                      ? 'bg-amber-50 border border-amber-200 dark:bg-amber-950/20 dark:border-amber-800'
                      : 'bg-muted'
                  }`}
                >
                  <div className="flex items-center justify-between mb-1">
                    <span className="font-medium text-xs">{m.sender}</span>
                    <span className="text-[10px] text-muted-foreground">{m.time}</span>
                  </div>
                  <p>{m.text}</p>
                  {m.isInternal && (
                    <span className="text-[10px] text-amber-600 font-medium">Internal Note</span>
                  )}
                </div>
              ))}
            </div>

            <div>
              <textarea
                placeholder="Write a reply..."
                value={reply}
                onChange={(e) => setReply(e.target.value)}
                rows={4}
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring resize-none"
              />
            </div>
          </div>
        )}
      </SlideOverPanel>
    </div>
  );
}
