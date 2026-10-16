import { StatusBadge } from '@gym360/ui';
import type { SupportTicket } from '@/lib/mock-data';

const COLUMNS = ['Ticket', 'Subject', 'Gym', 'Priority', 'Status', 'Assigned', 'Created'];

export function TicketsTable({ tickets, onSelect }: { tickets: SupportTicket[]; onSelect: (ticket: SupportTicket) => void }) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-sm">
        <thead>
          <tr className="border-b text-left text-muted-foreground">
            {COLUMNS.map((label) => (
              <th key={label} className="pb-2 font-medium">{label}</th>
            ))}
            <th className="pb-2 font-medium w-10"></th>
          </tr>
        </thead>
        <tbody>
          {tickets.map((t) => (
            <tr
              key={t.id}
              className="border-b border-border/50 hover:bg-muted/30 cursor-pointer transition-colors"
              onClick={() => onSelect(t)}
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
  );
}
