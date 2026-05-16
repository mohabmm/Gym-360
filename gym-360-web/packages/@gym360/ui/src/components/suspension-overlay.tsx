'use client';

import { AlertTriangle } from 'lucide-react';

interface SuspensionOverlayProps {
  gymName?: string;
  message?: string;
  onContactSupport?: () => void;
}

export function SuspensionOverlay({
  gymName,
  message = "Your gym's subscription has been suspended.",
  onContactSupport,
}: SuspensionOverlayProps) {
  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-black/80 backdrop-blur-sm">
      <div className="mx-4 max-w-md rounded-2xl bg-white p-8 text-center shadow-2xl">
        <div className="flex justify-center mb-4">
          <div className="flex h-16 w-16 items-center justify-center rounded-full bg-red-100">
            <AlertTriangle className="h-8 w-8 text-red-600" />
          </div>
        </div>
        <h2 className="text-xl font-bold text-gray-900 mb-2">Subscription Suspended</h2>
        {gymName && (
          <p className="text-sm font-medium text-gray-500 mb-3">{gymName}</p>
        )}
        <p className="text-gray-600 mb-6">{message}</p>
        <p className="text-sm text-gray-500 mb-4">
          Please contact your administrator or renew your subscription to continue using the platform.
        </p>
        {onContactSupport && (
          <button
            onClick={onContactSupport}
            className="inline-flex items-center justify-center rounded-lg bg-red-600 px-6 py-2.5 text-sm font-medium text-white hover:bg-red-700 transition-colors"
          >
            Contact Support
          </button>
        )}
      </div>
    </div>
  );
}
