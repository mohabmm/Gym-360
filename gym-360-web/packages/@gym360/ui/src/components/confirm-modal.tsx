'use client';

import { type ReactNode } from 'react';
import { cn } from '../lib/utils';
import { AlertTriangle, Info, CheckCircle, XCircle } from 'lucide-react';
import { Modal } from './modal';

type ConfirmVariant = 'danger' | 'warning' | 'info' | 'success';

interface ConfirmModalProps {
  open: boolean;
  onClose: () => void;
  onConfirm: () => void;
  title: string;
  description: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: ConfirmVariant;
  isLoading?: boolean;
  children?: ReactNode;
}

const variantConfig: Record<ConfirmVariant, { icon: ReactNode; buttonClass: string }> = {
  danger: {
    icon: <XCircle className="h-6 w-6 text-red-500" />,
    buttonClass: 'bg-red-600 hover:bg-red-700 text-white',
  },
  warning: {
    icon: <AlertTriangle className="h-6 w-6 text-amber-500" />,
    buttonClass: 'bg-amber-600 hover:bg-amber-700 text-white',
  },
  info: {
    icon: <Info className="h-6 w-6 text-blue-500" />,
    buttonClass: 'bg-blue-600 hover:bg-blue-700 text-white',
  },
  success: {
    icon: <CheckCircle className="h-6 w-6 text-emerald-500" />,
    buttonClass: 'bg-emerald-600 hover:bg-emerald-700 text-white',
  },
};

export function ConfirmModal({
  open,
  onClose,
  onConfirm,
  title,
  description,
  confirmLabel = 'Confirm',
  cancelLabel = 'Cancel',
  variant = 'danger',
  isLoading = false,
  children,
}: ConfirmModalProps) {
  const config = variantConfig[variant];

  return (
    <Modal
      open={open}
      onClose={onClose}
      title={title}
      size="sm"
      footer={
        <>
          <button
            onClick={onClose}
            disabled={isLoading}
            className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted transition-colors disabled:opacity-50"
          >
            {cancelLabel}
          </button>
          <button
            onClick={onConfirm}
            disabled={isLoading}
            className={cn(
              'rounded-lg px-4 py-2 text-sm font-medium transition-colors disabled:opacity-50',
              config.buttonClass
            )}
          >
            {isLoading ? 'Processing...' : confirmLabel}
          </button>
        </>
      }
    >
      <div className="flex gap-4">
        <div className="shrink-0">{config.icon}</div>
        <div>
          <p className="text-sm text-muted-foreground">{description}</p>
          {children && <div className="mt-4">{children}</div>}
        </div>
      </div>
    </Modal>
  );
}
