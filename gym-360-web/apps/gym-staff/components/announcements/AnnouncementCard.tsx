import { StatusBadge, formatDate } from '@gym360/ui';
import type { Announcement } from '@gym360/ui';
import { Megaphone, Edit, Trash2, Eye, EyeOff } from 'lucide-react';

interface Props {
  ann: Announcement;
  onTogglePublish: () => void;
  onEdit: () => void;
  onDelete: () => void;
}

export function AnnouncementCard({ ann, onTogglePublish, onEdit, onDelete }: Props) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-start gap-3 flex-1">
          <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary shrink-0">
            <Megaphone className="h-5 w-5" />
          </div>
          <div className="flex-1">
            <h3 className="font-semibold">{ann.title}</h3>
            {ann.titleAr && (
              <p className="font-semibold text-sm mt-0.5" dir="rtl">{ann.titleAr}</p>
            )}
            <p className="text-sm text-muted-foreground mt-1">{ann.body}</p>
            {ann.bodyAr && (
              <p className="text-sm text-muted-foreground mt-1" dir="rtl">{ann.bodyAr}</p>
            )}
            <div className="flex items-center gap-3 mt-3 text-xs text-muted-foreground flex-wrap">
              <span>By {ann.createdByName}</span>
              <span>·</span>
              <span>{formatDate(ann.publishedAt)}</span>
              <span>·</span>
              <StatusBadge variant={ann.isActive ? 'active' : 'inactive'} label={ann.isActive ? 'Published' : 'Draft'} />
              <StatusBadge variant="default" label={ann.targetAudience} />
              {ann.expiresAt && <span>Expires {formatDate(ann.expiresAt)}</span>}
            </div>
          </div>
        </div>
        <div className="flex items-center gap-1 shrink-0">
          <button
            onClick={onTogglePublish}
            className="rounded-md border p-1.5 hover:bg-accent"
            aria-label={ann.isActive ? 'Unpublish' : 'Publish'}
            title={ann.isActive ? 'Unpublish' : 'Publish'}
          >
            {ann.isActive ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
          </button>
          <button onClick={onEdit} className="rounded-md border p-1.5 hover:bg-accent" aria-label="Edit">
            <Edit className="h-3.5 w-3.5" />
          </button>
          <button
            onClick={onDelete}
            className="rounded-md border border-red-200 bg-red-50 p-1.5 text-red-600 hover:bg-red-100"
            aria-label="Delete"
          >
            <Trash2 className="h-3.5 w-3.5" />
          </button>
        </div>
      </div>
    </div>
  );
}
