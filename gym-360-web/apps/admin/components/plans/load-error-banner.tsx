import { AlertCircle } from 'lucide-react';

export function LoadErrorBanner({ message }: { message: string }) {
  return (
    <div className="flex items-center gap-3 rounded-xl border border-red-200 bg-red-50 px-4 py-4 dark:border-red-800 dark:bg-red-950/20">
      <AlertCircle className="h-5 w-5 text-red-500 shrink-0" />
      <p className="text-sm text-red-700 dark:text-red-400">{message}</p>
    </div>
  );
}
