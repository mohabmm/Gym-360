import { DAY_NAMES, apiGet, GYMS as GYMS_ENDPOINTS } from '@gym360/ui';

export type DaySchedule = {
  day: (typeof DAY_NAMES)[number];
  openTime: string;
  closeTime: string;
  is24Hour: boolean;
  isClosed: boolean;
};

export type NoshowAction = 'FlagOnly' | 'DeductSession' | 'SuspendBookings';
export type SectionType = 'Mens' | 'Womens' | 'Mixed';

export type SeasonalOverride = {
  id: string;
  name: string;
  startDate: string;
  endDate: string;
  isActive: boolean;
};

export type SessionPolicy = {
  cancellationWindowHours: number;
  noshowThreshold: number;
  noshowWindowDays: number;
  noshowAction: string;
};

type OperatingHourDto = { dayOfWeek: string; openTime: string; closeTime: string; isClosed: boolean };
type SectionDto = { id: string; sectionType: string; name: string | null; isActive: boolean };

const defaultDay = (day: DaySchedule['day']): DaySchedule => ({
  day,
  openTime: '06:00',
  closeTime: '23:00',
  is24Hour: false,
  isClosed: false,
});

export const DEFAULT_HOURS: DaySchedule[] = DAY_NAMES.map(defaultDay);

const SECTION_TYPES: SectionType[] = ['Mens', 'Womens', 'Mixed'];

const isSectionType = (v: string): v is SectionType => (SECTION_TYPES as string[]).includes(v);

function isAllDay(open: string, close: string) {
  return open === '00:00' && (close === '23:59' || close === '00:00');
}

function toDaySchedule(day: DaySchedule['day'], found: OperatingHourDto | undefined): DaySchedule {
  if (!found) return defaultDay(day);
  const open = (found.openTime || '06:00').slice(0, 5);
  const close = (found.closeTime || '23:00').slice(0, 5);
  return { day, openTime: open, closeTime: close, is24Hour: isAllDay(open, close), isClosed: found.isClosed };
}

/** Maps BE operating hours onto the fixed week; `null` when the gym has none saved. */
export function mapOperatingHours(data: OperatingHourDto[] | undefined | null): DaySchedule[] | null {
  const rows = data ?? [];
  if (!rows.length) return null;
  const map = new Map(rows.map((h) => [h.dayOfWeek, h]));
  return DAY_NAMES.map((day) => toDaySchedule(day, map.get(day)));
}

/** Active section flags; `null` when the gym has no sections saved. */
export function mapSections(data: SectionDto[] | undefined | null): Record<SectionType, boolean> | null {
  const rows = data ?? [];
  if (!rows.length) return null;
  const next: Record<SectionType, boolean> = { Mens: false, Womens: false, Mixed: false };
  for (const s of rows) {
    if (s.isActive && isSectionType(s.sectionType)) next[s.sectionType] = true;
  }
  return next;
}

/** Seasonal overrides; `null` when the gym has none saved. */
export function mapSeasonal(data: SeasonalOverride[] | undefined | null): SeasonalOverride[] | null {
  const rows = data ?? [];
  if (!rows.length) return null;
  return rows.map((s) => ({ id: s.id, name: s.name, startDate: s.startDate, endDate: s.endDate, isActive: s.isActive }));
}

export type LoadedGymSettings = {
  hours: DaySchedule[] | null;
  policy: SessionPolicy | null | undefined;
  sections: Record<SectionType, boolean> | null;
  seasonal: SeasonalOverride[] | null;
};

export async function fetchGymSettings(gymId: string): Promise<LoadedGymSettings> {
  const [hoursRes, policyRes, sectionsRes, seasonalRes] = await Promise.all([
    apiGet<OperatingHourDto[]>(GYMS_ENDPOINTS.OPERATING_HOURS(gymId)),
    apiGet<SessionPolicy>(GYMS_ENDPOINTS.SESSION_POLICY(gymId)),
    apiGet<SectionDto[]>(GYMS_ENDPOINTS.SECTIONS(gymId)),
    apiGet<SeasonalOverride[]>(GYMS_ENDPOINTS.SEASONAL_HOURS(gymId)),
  ]);
  return {
    hours: mapOperatingHours(hoursRes.data),
    policy: policyRes.data,
    sections: mapSections(sectionsRes.data),
    seasonal: mapSeasonal(seasonalRes.data),
  };
}
