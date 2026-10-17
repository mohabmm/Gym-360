'use client';

import { useState, useEffect, useRef } from 'react';
import { useSession } from 'next-auth/react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { PageHeader, KpiCard, StatusBadge, Modal, formatTime } from '@gym360/ui';
import { DoorOpen, QrCode, Keyboard, User, Plus, Check } from 'lucide-react';
import { useCheckIns } from '@/lib/hooks/useCheckIns';
import { useMembers } from '@/lib/hooks/useMembers';
import apiClient from '@/lib/api-client';
import type { MemberCheckIn } from '@gym360/ui';

type CheckInMethod = 'qr' | 'numpad' | 'manual';

type CheckInPayload = {
  // Must match the API's CheckInMethod enum: QR | MemberNumber | Manual.
  method: 'QR' | 'MemberNumber' | 'Manual';
  qrToken?: string;
  memberNumber?: string;
  memberId?: string;
  staffId?: string;
};

type MemberLike = { id: string; name: string; phone?: string; status: string };

// Search by name substring, exact member id, or phone suffix (q is already lower-cased).
function matchesMemberQuery(m: MemberLike, q: string) {
  return (m.name ?? '').toLowerCase().includes(q) || m.id.toLowerCase() === q || !!m.phone?.endsWith(q);
}

// Flash message explaining why a manual check-in cannot proceed, or null when it can.
function manualCheckInBlocker(found: MemberLike | undefined, query: string): string | null {
  if (!found) return `No match for "${query}".`;
  if (found.status !== 'active') return `${found.name} — membership is ${found.status}.`;
  return null;
}

function checkInErrorMessage(err: unknown, fallback: string) {
  return (err as { response?: { data?: { error?: { message?: string } } } })?.response?.data?.error?.message ?? fallback;
}

export default function CheckInsPage() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const staffId = session?.user?.id;
  const queryClient = useQueryClient();

  const { data: checkIns = [] } = useCheckIns();
  const { data: members = [] } = useMembers();

  const [showManualCheckIn, setShowManualCheckIn] = useState(false);
  const [manualQuery, setManualQuery] = useState('');
  const [manualSection, setManualSection] = useState<MemberCheckIn['section']>('Mens');
  const [numpadOpen, setNumpadOpen] = useState(false);
  const [numpadValue, setNumpadValue] = useState('');
  const [numpadError, setNumpadError] = useState<string | null>(null);
  const [qrOpen, setQrOpen] = useState(false);
  const [qrError, setQrError] = useState<string | null>(null);
  const [flash, setFlash] = useState<string | null>(null);

  // API serializes CheckInMethod as "QR" | "MemberNumber" | "Manual"; normalize
  // case-insensitively (the numpad flow maps to MemberNumber).
  const methodKey = (m?: string) => (m ?? '').toLowerCase();
  const qrCount = checkIns.filter((c) => methodKey(c.method) === 'qr').length;
  const manualCount = checkIns.filter((c) => methodKey(c.method) === 'manual').length;
  const numpadCount = checkIns.filter((c) => methodKey(c.method) === 'membernumber').length;

  const showFlash = (msg: string) => {
    setFlash(msg);
    setTimeout(() => setFlash(null), 3500);
  };

  const recordMutation = useMutation({
    mutationFn: async (payload: CheckInPayload) => {
      if (!gymId) throw new Error('No gym selected.');
      const res = await apiClient.post(`/gyms/${gymId}/check-in`, payload);
      return res.data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['check-ins', gymId] });
    },
  });

  const numpadSubmit = async () => {
    if (!numpadValue) return;
    try {
      await recordMutation.mutateAsync({
        method: 'MemberNumber',
        memberNumber: numpadValue,
        staffId,
      });
      showFlash(`Checked in (#${numpadValue}).`);
      setNumpadValue('');
      setNumpadError(null);
      setNumpadOpen(false);
    } catch (err) {
      const msg = checkInErrorMessage(err, 'Check-in failed.');
      setNumpadError(msg);
    }
  };

  const numpadAppend = (d: string) => {
    setNumpadError(null);
    setNumpadValue((v) => (v + d).slice(0, 6));
  };
  const numpadBackspace = () => {
    setNumpadError(null);
    setNumpadValue((v) => v.slice(0, -1));
  };

  const manualSubmit = async () => {
    const q = manualQuery.trim().toLowerCase();
    if (!q) return;
    const found = members.find((m) => matchesMemberQuery(m, q));
    const blocker = manualCheckInBlocker(found, manualQuery);
    if (blocker || !found) {
      showFlash(blocker!);
      return;
    }
    try {
      await recordMutation.mutateAsync({
        method: 'Manual',
        memberId: found.id,
        staffId,
      });
      showFlash(`${found.name} checked in (MANUAL).`);
      setManualQuery('');
      setShowManualCheckIn(false);
    } catch (err) {
      const msg = checkInErrorMessage(err, 'Check-in failed.');
      showFlash(msg);
    }
  };

  // --- Real QR scanner via html5-qrcode ---
  const qrRegionId = 'check-in-qr-reader';
  const scannerRef = useRef<{ stop: () => Promise<void>; clear: () => void } | null>(null);

  const handleQrDecoded = async (decoded: string) => {
    if (recordMutation.isPending) return;
    try {
      await recordMutation.mutateAsync({
        method: 'QR',
        qrToken: decoded.trim(),
        staffId,
      });
      showFlash(`Checked in via QR.`);
      setQrOpen(false);
    } catch (err) {
      const msg = checkInErrorMessage(err, 'QR check-in failed.');
      setQrError(msg);
    }
  };

  useEffect(() => {
    if (!qrOpen) return;
    let cancelled = false;
    setQrError(null);

    (async () => {
      const { Html5Qrcode } = await import('html5-qrcode');
      if (cancelled) return;
      const scanner = new Html5Qrcode(qrRegionId);
      scannerRef.current = {
        stop: () => scanner.stop(),
        clear: () => scanner.clear(),
      };
      try {
        await scanner.start(
          { facingMode: 'environment' },
          { fps: 10, qrbox: 240 },
          (decodedText) => {
            scanner.pause(true);
            handleQrDecoded(decodedText).finally(() => {
              try { scanner.resume(); } catch { /* noop */ }
            });
          },
          () => { /* per-frame decode misses are noisy; ignore */ },
        );
      } catch (err) {
        setQrError(err instanceof Error ? err.message : 'Camera unavailable.');
      }
    })();

    return () => {
      cancelled = true;
      const s = scannerRef.current;
      scannerRef.current = null;
      if (s) {
        s.stop().catch(() => undefined).finally(() => {
          try { s.clear(); } catch { /* noop */ }
        });
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [qrOpen]);

  return (
    <div className="space-y-6">
      <PageHeader
        title="Check-Ins"
        description="Today's check-in activity"
        actions={
          <div className="flex items-center gap-2">
            <button
              onClick={() => { setQrError(null); setQrOpen(true); }}
              className="inline-flex items-center gap-2 rounded-lg border border-border px-3.5 py-2 text-sm font-medium hover:bg-muted transition-colors"
            >
              <QrCode className="h-4 w-4" /> Scan QR
            </button>
            <button
              onClick={() => {
                setNumpadOpen(true);
                setNumpadValue('');
                setNumpadError(null);
              }}
              className="inline-flex items-center gap-2 rounded-lg border border-border px-3.5 py-2 text-sm font-medium hover:bg-muted transition-colors"
            >
              <Keyboard className="h-4 w-4" /> Numpad
            </button>
            <button
              onClick={() => setShowManualCheckIn((s) => !s)}
              className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
            >
              <Plus className="h-4 w-4" /> Manual
            </button>
          </div>
        }
      />

      {flash && (
        <div className="rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-2 text-sm font-medium text-emerald-800 flex items-center gap-2">
          <Check className="h-4 w-4" /> {flash}
        </div>
      )}

      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <KpiCard value={checkIns.length.toString()} label="Total Today" icon={<DoorOpen className="h-5 w-5" />} variant="success" />
        <KpiCard value={qrCount.toString()} label="QR Scans" icon={<QrCode className="h-5 w-5" />} variant="info" />
        <KpiCard value={numpadCount.toString()} label="Numpad" icon={<Keyboard className="h-5 w-5" />} />
        <KpiCard value={manualCount.toString()} label="Manual" icon={<User className="h-5 w-5" />} />
      </div>

      {showManualCheckIn && (
        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <h3 className="text-sm font-semibold mb-4">Manual Check-In</h3>
          <div className="flex items-center gap-3 flex-wrap">
            <div className="flex-1 min-w-[240px] relative">
              <input
                type="text"
                value={manualQuery}
                onChange={(e) => setManualQuery(e.target.value)}
                onKeyDown={(e) => e.key === 'Enter' && manualSubmit()}
                placeholder="Name, member ID or phone…"
                className="w-full h-10 rounded-lg border border-input bg-background px-3 text-sm"
              />
              {manualQuery.trim().length > 0 && (() => {
                const q = manualQuery.trim().toLowerCase();
                const filtered = members.filter((m) => matchesMemberQuery(m, q)).slice(0, 5);
                return filtered.length > 0 ? (
                  <ul className="absolute z-10 top-full left-0 right-0 mt-1 rounded-lg border bg-card shadow-lg divide-y">
                    {filtered.map((m) => (
                      <li
                        key={m.id}
                        onClick={() => {
                          setManualQuery(m.name ?? '');
                        }}
                        className="px-3 py-2 text-sm cursor-pointer hover:bg-muted flex items-center justify-between"
                      >
                        <span>{m.name}</span>
                        <span className="text-xs text-muted-foreground">{m.status}</span>
                      </li>
                    ))}
                  </ul>
                ) : null;
              })()}
            </div>
            <select
              value={manualSection}
              onChange={(e) => setManualSection(e.target.value as MemberCheckIn['section'])}
              className="h-10 rounded-lg border border-input bg-background px-3 text-sm"
            >
              <option value="Mens">Men&apos;s Floor</option>
              <option value="Womens">Ladies Floor</option>
              <option value="Mixed">Mixed</option>
            </select>
            <button
              onClick={manualSubmit}
              disabled={!manualQuery.trim() || recordMutation.isPending}
              className="rounded-lg bg-primary px-6 py-2.5 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
            >
              {recordMutation.isPending ? 'Checking in…' : 'Check In'}
            </button>
          </div>
        </div>
      )}

      {/* Live Log */}
      <div className="rounded-xl border bg-card shadow-sm overflow-hidden">
        <div className="p-4 border-b bg-muted/30">
          <h3 className="text-sm font-semibold flex items-center gap-2">
            <div className="h-2 w-2 rounded-full bg-emerald-500 animate-pulse" />
            Live Check-In Log
          </h3>
        </div>
        <div className="divide-y">
          {checkIns.length === 0 ? (
            <div className="p-10 text-center text-sm text-muted-foreground">No check-ins recorded today.</div>
          ) : (
            checkIns.map((checkIn) => (
              <div key={checkIn.id} className="flex items-center justify-between px-6 py-3 hover:bg-muted/50 transition-colors">
                <div className="flex items-center gap-3">
                  <div className="h-9 w-9 rounded-full bg-primary/10 flex items-center justify-center text-xs font-bold text-primary">
                    {(checkIn.memberName ?? '').split(' ').map((n) => n[0]).join('').slice(0, 2)}
                  </div>
                  <div>
                    <p className="text-sm font-medium">{checkIn.memberName}</p>
                    <p className="text-xs text-muted-foreground">{checkIn.section}</p>
                  </div>
                </div>
                <div className="flex items-center gap-3">
                  <StatusBadge
                    variant={methodKey(checkIn.method) === 'qr' ? 'info' : methodKey(checkIn.method) === 'membernumber' ? 'warning' : 'default'}
                    label={methodKey(checkIn.method) === 'membernumber' ? 'NUMPAD' : (checkIn.method ?? '').toUpperCase()}
                  />
                  <span className="text-sm text-muted-foreground">
                    {formatTime(new Date(checkIn.checkInTime).toTimeString().slice(0, 5))}
                  </span>
                </div>
              </div>
            ))
          )}
        </div>
      </div>

      {/* QR Scanner Modal — real camera */}
      <Modal open={qrOpen} onClose={() => setQrOpen(false)} title="Scan Member QR" size="md">
        <div className="flex flex-col items-center justify-center py-2">
          <div id={qrRegionId} className="w-full max-w-sm rounded-xl overflow-hidden border border-border bg-black" />
          {qrError && (
            <p className="mt-3 text-sm text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2">
              {qrError}
            </p>
          )}
          <p className="mt-4 text-sm text-muted-foreground text-center">
            Hold the member card in front of the camera. Trainee QR codes encode the member number.
          </p>
        </div>
      </Modal>

      {/* Numpad Modal */}
      <Modal
        open={numpadOpen}
        onClose={() => setNumpadOpen(false)}
        title="Enter member number"
        size="sm"
        footer={
          <>
            <button
              onClick={() => setNumpadOpen(false)}
              className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted"
            >
              Cancel
            </button>
            <button
              onClick={numpadSubmit}
              disabled={!numpadValue || recordMutation.isPending}
              className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
            >
              {recordMutation.isPending ? 'Checking in…' : 'Check In'}
            </button>
          </>
        }
      >
        <div>
          <div className="rounded-xl bg-muted/40 border border-border p-4 text-center mb-4">
            <p className="text-xs text-muted-foreground uppercase tracking-wide mb-2">Member number</p>
            <p className="text-4xl font-mono font-bold tabular-nums h-10">
              {numpadValue || <span className="text-muted-foreground/40">••••</span>}
            </p>
          </div>
          {numpadError && (
            <p className="text-xs text-red-600 bg-red-50 border border-red-200 rounded-lg px-3 py-2 mb-3">
              {numpadError}
            </p>
          )}
          <div className="grid grid-cols-3 gap-2">
            {['1', '2', '3', '4', '5', '6', '7', '8', '9'].map((d) => (
              <button
                key={d}
                onClick={() => numpadAppend(d)}
                className="h-14 rounded-xl border border-border bg-background text-xl font-semibold hover:bg-muted active:bg-muted/70 transition-colors"
              >
                {d}
              </button>
            ))}
            <button
              onClick={() => setNumpadValue('')}
              className="h-14 rounded-xl border border-border bg-background text-sm font-medium text-muted-foreground hover:bg-muted"
            >
              Clear
            </button>
            <button
              onClick={() => numpadAppend('0')}
              className="h-14 rounded-xl border border-border bg-background text-xl font-semibold hover:bg-muted"
            >
              0
            </button>
            <button
              onClick={numpadBackspace}
              className="h-14 rounded-xl border border-border bg-background text-sm font-medium text-muted-foreground hover:bg-muted"
            >
              ⌫
            </button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
