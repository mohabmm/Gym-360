import { StatusBadge, SlideOverPanel } from '@gym360/ui';
import type { SupportTicket } from '@/lib/mock-data';
import type { TicketStatus } from '@/lib/support';

type TicketMessage = SupportTicket['messages'][number];

function messageClass(isInternal: boolean | undefined): string {
  const tone = isInternal
    ? 'bg-amber-50 border border-amber-200 dark:bg-amber-950/20 dark:border-amber-800'
    : 'bg-muted';
  return `rounded-lg p-3 text-sm ${tone}`;
}

function ConversationMessage({ message }: { message: TicketMessage }) {
  return (
    <div className={messageClass(message.isInternal)}>
      <div className="flex items-center justify-between mb-1">
        <span className="font-medium text-xs">{message.sender}</span>
        <span className="text-[10px] text-muted-foreground">{message.time}</span>
      </div>
      <p>{message.text}</p>
      {message.isInternal && (
        <span className="text-[10px] text-amber-600 font-medium">Internal Note</span>
      )}
    </div>
  );
}

interface TicketDetailBodyProps {
  ticket: SupportTicket;
  reply: string;
  onReplyChange: (value: string) => void;
  onStatusChange: (status: TicketStatus) => void;
}

function TicketDetailBody({ ticket, reply, onReplyChange, onStatusChange }: TicketDetailBodyProps) {
  return (
    <div className="space-y-4">
      <div className="flex items-center gap-2 flex-wrap">
        <StatusBadge variant={ticket.priority} />
        <StatusBadge variant={ticket.status} />
        <span className="text-sm text-muted-foreground">{ticket.gym} · {ticket.assignedTo}</span>
      </div>

      <div>
        <p className="text-xs font-medium text-muted-foreground mb-1.5">Status</p>
        <select
          value={ticket.status}
          onChange={(e) => onStatusChange(e.target.value as TicketStatus)}
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
        {ticket.messages.map((m, i) => (
          <ConversationMessage key={i} message={m} />
        ))}
      </div>

      <div>
        <textarea
          placeholder="Write a reply..."
          value={reply}
          onChange={(e) => onReplyChange(e.target.value)}
          rows={4}
          className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring resize-none"
        />
      </div>
    </div>
  );
}

interface TicketDetailPanelProps extends Omit<TicketDetailBodyProps, 'ticket'> {
  ticket: SupportTicket | null;
  onClose: () => void;
  onAddMessage: (isInternal: boolean) => void;
}

export function TicketDetailPanel({ ticket, onClose, onAddMessage, ...bodyProps }: TicketDetailPanelProps) {
  const replyEmpty = !bodyProps.reply.trim();
  return (
    <SlideOverPanel
      open={!!ticket}
      onClose={onClose}
      title={ticket ? `${ticket.number} — ${ticket.subject}` : ''}
      size="lg"
      footer={
        <>
          <button
            className="h-9 px-4 rounded-lg border border-input bg-background text-sm font-medium hover:bg-muted transition-colors disabled:opacity-50"
            onClick={() => onAddMessage(true)}
            disabled={replyEmpty}
          >
            Internal Note
          </button>
          <button
            className="h-9 px-4 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 transition-colors disabled:opacity-50"
            onClick={() => onAddMessage(false)}
            disabled={replyEmpty}
          >
            Reply
          </button>
        </>
      }
    >
      {ticket && <TicketDetailBody ticket={ticket} {...bodyProps} />}
    </SlideOverPanel>
  );
}
