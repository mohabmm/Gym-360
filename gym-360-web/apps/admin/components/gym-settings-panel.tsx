'use client';

import { useEffect, useState } from 'react';
import { StatusBadge, DAY_NAMES, apiGet, apiPut, GYMS as GYMS_ENDPOINTS } from '@gym360/ui';
import { Clock, Shield, Users, CalendarDays, Plus, Trash2, Save } from 'lucide-react';

type DaySchedule = {
  day: (typeof DAY_NAMES)[number];
  openTime: string;
  closeTime: string;
  is24Hour: boolean;
  isClosed: boolean;
};

type NoshowAction = 'FlagOnly' | 'DeductSession' | 'SuspendBookings';
type SectionType = 'Mens' | 'Womens' | 'Mixed';

type SeasonalOverride = {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
};

const DEFAULT_HOURS: DaySchedule[] = DAY_NAMES.map((day) => ({
  day,
  openTime: '06:00',
  closeTime: '23:00',
  is24Hour: false,
  isClosed: false,
}));

type Props = {
  gymId: string;
  gymName: string;
};

export function GymSettingsPanel({ gymId, gymName }: Props) {
  const [hours, setHours] = useState<DaySchedule[]>(DEFAULT_HOURS);

  // Q5: Session Policy
  const [cancellationWindow, setCancellationWindow] = useState(24);
  const [noshowThreshold, setNoshowThreshold] = useState(3);
  const [noshowWindow, setNoshowWindow] = useState(30);
  const [noshowAction, setNoshowAction] = useState<NoshowAction>('FlagOnly');

  // Q31: Sections
  const [sections, setSections] = useState<Record<SectionType, boolean>>({
    Mens: true,
    Womens: true,
    Mixed: false,
  });

  // Q32: Seasonal Hours (Ramadan etc.)
  const [seasonalOverrides, setSeasonalOverrides] = useState<SeasonalOverride[]>([
    {
      id: 'ramadan-2026',
      name: 'Ramadan 2026',
      startDate: '2026-02-17',
      endDate: '2026-03-18',
      isActive: true,
    },
  ]);

  const [saved, setSaved] = useState<string | null>(null);
  const [savingKey, setSavingKey] = useState<string | null>(null);
  const [loaded, setLoaded] = useState(false);

  useEffect(() => {
    let cancelled = false;
    async function load() {
      try {
        const [hoursRes, policyRes, sectionsRes, seasonalRes] = await Promise.all([
          apiGet<Array<{ dayOfWeek: string; openTime: string; closeTime: string; isClosed: boolean }>>(
            GYMS_ENDPOINTS.OPERATING_HOURS(gymId),
          ),
          apiGet<{ cancellationWindowHours: number; noshowThreshold: number; noshowWindowDays: number; noshowAction: string }>(
            GYMS_ENDPOINTS.SESSION_POLICY(gymId),
          ),
          apiGet<Array<{ id: string; sectionType: string; name: string | null; isActive: boolean }>>(
            GYMS_ENDPOINTS.SECTIONS(gymId),
          ),
          apiGet<Array<{ id: string; name: string; startDate: string; endDate: string; isActive: boolean }>>(
            GYMS_ENDPOINTS.SEASONAL_HOURS(gymId),
          ),
        ]);
        if (cancelled) return;

        const hoursData = hoursRes.data ?? [];
        if (hoursData.length) {
          const map = new Map(hoursData.map((h) => [h.dayOfWeek, h]));
          setHours(
            DAY_NAMES.map((day) => {
              const found = map.get(day);
              if (!found) return { day, openTime: '06:00', closeTime: '23:00', is24Hour: false, isClosed: false };
              const open = (found.openTime || '06:00').slice(0, 5);
              const close = (found.closeTime || '23:00').slice(0, 5);
              const is24 = open === '00:00' && (close === '23:59' || close === '00:00');
              return { day, openTime: open, closeTime: close, is24Hour: is24, isClosed: found.isClosed };
            }),
          );
        }

        const policy = policyRes.data;
        if (policy) {
          setCancellationWindow(policy.cancellationWindowHours);
          setNoshowThreshold(policy.noshowThreshold);
          setNoshowWindow(policy.noshowWindowDays);
          setNoshowAction(policy.noshowAction as NoshowAction);
        }

        const sectionsData = sectionsRes.data ?? [];
        if (sectionsData.length) {
          const next: Record<SectionType, boolean> = { Mens: false, Womens: false, Mixed: false };
          for (const s of sectionsData) {
            if (s.isActive && (s.sectionType === 'Mens' || s.sectionType === 'Womens' || s.sectionType === 'Mixed')) {
              next[s.sectionType] = true;
            }
          }
          setSections(next);
        }

        const seasonalData = seasonalRes.data ?? [];
        if (seasonalData.length) {
          setSeasonalOverrides(seasonalData.map((s) => ({ id: s.id, name: s.name, startDate: s.startDate, endDate: s.endDate, isActive: s.isActive })));
        }
      } finally {
        if (!cancelled) setLoaded(true);
      }
    }
    load();
    return () => {
      cancelled = true;
    };
  }, [gymId]);

  const updateHour = (idx: number, patch: Partial<DaySchedule>) => {
    setHours((prev) => prev.map((h, i) => (i === idx ? { ...h, ...patch } : h)));
  };

  const saveOperatingHours = async () => {
    setSavingKey('hours');
    try {
      const payload = hours.map((h) => ({
        dayOfWeek: h.day,
        openTime: h.is24Hour ? '00:00' : h.openTime,
        closeTime: h.is24Hour ? '23:59' : h.closeTime,
        isClosed: h.isClosed,
      }));
      await apiPut(GYMS_ENDPOINTS.OPERATING_HOURS(gymId), { hours: payload });
      flash('Operating hours saved');
    } finally {
      setSavingKey(null);
    }
  };

  const saveSessionPolicy = async () => {
    setSavingKey('policy');
    try {
      await apiPut(GYMS_ENDPOINTS.SESSION_POLICY(gymId), {
        cancellationWindowHours: cancellationWindow,
        noshowThreshold,
        noshowWindowDays: noshowWindow,
        noshowAction,
      });
      flash('Session policy saved');
    } finally {
      setSavingKey(null);
    }
  };

  const saveSections = async () => {
    setSavingKey('sections');
    try {
      const enabled = (Object.keys(sections) as SectionType[]).filter((s) => sections[s]);
      await apiPut(GYMS_ENDPOINTS.SECTIONS(gymId), { sections: enabled });
      flash('Sections saved');
    } finally {
      setSavingKey(null);
    }
  };

  const persistSeasonal = async (next: SeasonalOverride[]) => {
    setSavingKey('seasonal');
    try {
      await apiPut(GYMS_ENDPOINTS.SEASONAL_HOURS(gymId), { overrides: next });
    } finally {
      setSavingKey(null);
    }
  };

  const addSeasonal = async () => {
    const next: SeasonalOverride[] = [
      ...seasonalOverrides,
      {
        id: crypto.randomUUID(),
        name: 'New Override',
        startDate: new Date().toISOString().slice(0, 10),
        endDate: new Date().toISOString().slice(0, 10),
        isActive: true,
      },
    ];
    setSeasonalOverrides(next);
    await persistSeasonal(next);
    flash('Override added');
  };

  const removeSeasonal = async (id: string) => {
    const next = seasonalOverrides.filter((s) => s.id !== id);
    setSeasonalOverrides(next);
    await persistSeasonal(next);
    flash('Override removed');
  };

  const updateSeasonal = (id: string, patch: Partial<SeasonalOverride>) =>
    setSeasonalOverrides((prev) => prev.map((s) => (s.id === id ? { ...s, ...patch } : s)));

  function flash(msg: string) {
    setSaved(msg);
    setTimeout(() => setSaved(null), 2500);
  }

  return (
    <div className="space-y-6">
      {saved && (
        <div className="fixed top-4 right-4 z-50 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white shadow-lg">
          {saved}
        </div>
      )}

      {/* Operating Hours */}
      <section className="rounded-xl border bg-card p-6 shadow-sm">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Clock className="h-4 w-4 text-muted-foreground" />
            <h3 className="text-sm font-semibold">Operating Hours</h3>
          </div>
          <button
            onClick={saveOperatingHours}
            disabled={savingKey === 'hours'}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-3 py-1.5 text-xs font-medium text-white hover:bg-primary/90 disabled:opacity-50"
          >
            <Save className="h-3.5 w-3.5" /> {savingKey === 'hours' ? 'Saving...' : 'Save'}
          </button>
        </div>
        <div className="space-y-2">
          {hours.map((h, i) => (
            <div key={h.day} className="grid grid-cols-12 items-center gap-3 rounded-lg border px-3 py-2 text-sm">
              <div className="col-span-2 font-medium">{h.day}</div>
              <label className="col-span-2 flex items-center gap-2 text-xs">
                <input
                  type="checkbox"
                  checked={h.is24Hour}
                  onChange={(e) => updateHour(i, { is24Hour: e.target.checked, isClosed: false })}
                />
                24h
              </label>
              <label className="col-span-2 flex items-center gap-2 text-xs">
                <input
                  type="checkbox"
                  checked={h.isClosed}
                  onChange={(e) => updateHour(i, { isClosed: e.target.checked, is24Hour: false })}
                />
                Closed
              </label>
              <input
                type="time"
                value={h.openTime}
                disabled={h.is24Hour || h.isClosed}
                onChange={(e) => updateHour(i, { openTime: e.target.value })}
                className="col-span-3 rounded-md border px-2 py-1 text-xs disabled:bg-muted"
              />
              <input
                type="time"
                value={h.closeTime}
                disabled={h.is24Hour || h.isClosed}
                onChange={(e) => updateHour(i, { closeTime: e.target.value })}
                className="col-span-3 rounded-md border px-2 py-1 text-xs disabled:bg-muted"
              />
            </div>
          ))}
        </div>
      </section>

      {/* Session Policy (Q5) */}
      <section className="rounded-xl border bg-card p-6 shadow-sm">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Shield className="h-4 w-4 text-muted-foreground" />
            <h3 className="text-sm font-semibold">Session Policy</h3>
            <span className="text-xs text-muted-foreground">(Q5 — configurable per gym)</span>
          </div>
          <button
            onClick={saveSessionPolicy}
            disabled={savingKey === 'policy'}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-3 py-1.5 text-xs font-medium text-white hover:bg-primary/90 disabled:opacity-50"
          >
            <Save className="h-3.5 w-3.5" /> {savingKey === 'policy' ? 'Saving...' : 'Save'}
          </button>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <label className="block">
            <span className="text-xs font-medium text-muted-foreground">
              Cancellation Window (hours)
            </span>
            <input
              type="number"
              min={0}
              max={168}
              value={cancellationWindow}
              onChange={(e) => setCancellationWindow(Number(e.target.value))}
              className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
            />
            <p className="mt-1 text-xs text-muted-foreground">
              Cancellations made within this window will be deducted from the member's package.
            </p>
          </label>
          <label className="block">
            <span className="text-xs font-medium text-muted-foreground">No-show Threshold</span>
            <input
              type="number"
              min={1}
              max={20}
              value={noshowThreshold}
              onChange={(e) => setNoshowThreshold(Number(e.target.value))}
              className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
            />
          </label>
          <label className="block">
            <span className="text-xs font-medium text-muted-foreground">No-show Window (days)</span>
            <input
              type="number"
              min={1}
              max={365}
              value={noshowWindow}
              onChange={(e) => setNoshowWindow(Number(e.target.value))}
              className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
            />
          </label>
          <label className="block">
            <span className="text-xs font-medium text-muted-foreground">Action on Threshold</span>
            <select
              value={noshowAction}
              onChange={(e) => setNoshowAction(e.target.value as NoshowAction)}
              className="mt-1 w-full rounded-md border px-3 py-2 text-sm"
            >
              <option value="FlagOnly">Flag only</option>
              <option value="DeductSession">Deduct session from package</option>
              <option value="SuspendBookings">Suspend bookings</option>
            </select>
          </label>
        </div>
      </section>

      {/* Sections (Q31) */}
      <section className="rounded-xl border bg-card p-6 shadow-sm">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <Users className="h-4 w-4 text-muted-foreground" />
            <h3 className="text-sm font-semibold">Gym Sections</h3>
            <span className="text-xs text-muted-foreground">(Q31)</span>
          </div>
          <button
            onClick={saveSections}
            disabled={savingKey === 'sections'}
            className="inline-flex items-center gap-2 rounded-lg bg-primary px-3 py-1.5 text-xs font-medium text-white hover:bg-primary/90 disabled:opacity-50"
          >
            <Save className="h-3.5 w-3.5" /> {savingKey === 'sections' ? 'Saving...' : 'Save'}
          </button>
        </div>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
          {(['Mens', 'Womens', 'Mixed'] as SectionType[]).map((s) => (
            <label
              key={s}
              className="flex items-center gap-3 rounded-lg border px-4 py-3 cursor-pointer hover:bg-accent"
            >
              <input
                type="checkbox"
                checked={sections[s]}
                onChange={(e) => setSections((prev) => ({ ...prev, [s]: e.target.checked }))}
              />
              <div>
                <div className="text-sm font-medium">{s === 'Mens' ? "Men's Floor" : s === 'Womens' ? "Women's Floor" : 'Mixed Floor'}</div>
                <div className="text-xs text-muted-foreground">
                  {sections[s] ? 'Enabled for member assignment' : 'Disabled'}
                </div>
              </div>
            </label>
          ))}
        </div>
      </section>

      {/* Seasonal Hours (Q32) */}
      <section className="rounded-xl border bg-card p-6 shadow-sm">
        <div className="flex items-center justify-between mb-4">
          <div className="flex items-center gap-2">
            <CalendarDays className="h-4 w-4 text-muted-foreground" />
            <h3 className="text-sm font-semibold">Seasonal Hour Overrides</h3>
            <span className="text-xs text-muted-foreground">(Q32 — Ramadan, holidays, etc.)</span>
          </div>
          <button
            onClick={addSeasonal}
            disabled={savingKey === 'seasonal'}
            className="inline-flex items-center gap-2 rounded-lg border px-3 py-1.5 text-xs font-medium hover:bg-accent disabled:opacity-50"
          >
            <Plus className="h-3.5 w-3.5" /> {savingKey === 'seasonal' ? 'Saving...' : 'Add override'}
          </button>
        </div>
        {seasonalOverrides.length === 0 ? (
          <p className="text-sm text-muted-foreground">No seasonal overrides configured.</p>
        ) : (
          <div className="space-y-2">
            {seasonalOverrides.map((s) => (
              <div key={s.id} className="grid grid-cols-12 items-center gap-3 rounded-lg border px-3 py-2 text-sm">
                <input
                  type="text"
                  value={s.name}
                  onChange={(e) => updateSeasonal(s.id, { name: e.target.value })}
                  className="col-span-4 rounded-md border px-2 py-1 text-sm"
                  placeholder="e.g., Ramadan 2026"
                />
                <input
                  type="date"
                  value={s.startDate}
                  onChange={(e) => updateSeasonal(s.id, { startDate: e.target.value })}
                  className="col-span-3 rounded-md border px-2 py-1 text-xs"
                />
                <input
                  type="date"
                  value={s.endDate}
                  onChange={(e) => updateSeasonal(s.id, { endDate: e.target.value })}
                  className="col-span-3 rounded-md border px-2 py-1 text-xs"
                />
                <div className="col-span-1">
                  <StatusBadge variant={s.isActive ? 'active' : 'inactive'} label={s.isActive ? 'On' : 'Off'} />
                </div>
                <button
                  onClick={() => removeSeasonal(s.id)}
                  className="col-span-1 inline-flex items-center justify-center rounded-md border border-red-200 bg-red-50 p-1.5 text-red-600 hover:bg-red-100"
                  aria-label="Remove"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </button>
              </div>
            ))}
          </div>
        )}
        <p className="mt-3 text-xs text-muted-foreground">
          Configured: {gymName} ({gymId.slice(0, 8)})
        </p>
      </section>
    </div>
  );
}
