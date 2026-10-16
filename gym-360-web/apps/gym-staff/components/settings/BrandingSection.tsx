import { SettingsCard } from './SettingsCard';

interface Colors {
  primaryColor: string;
  accentColor: string;
}

interface Props extends Colors {
  gymName: string;
  onPrimaryColorChange: (color: string) => void;
  onAccentColorChange: (color: string) => void;
}

// Two-line wordmark from the gym name: first word on top, the rest below (6 chars max each).
function logoWords(gymName: string) {
  const words = (gymName || 'Gym').trim().split(/\s+/).filter(Boolean);
  return {
    top: (words[0] ?? 'GYM').toUpperCase().slice(0, 6),
    bottom: (words.slice(1).join(' ') || '').toUpperCase().slice(0, 6),
  };
}

function LogoPreview({ gymName, primaryColor, accentColor }: Colors & { gymName: string }) {
  const { top, bottom } = logoWords(gymName);
  return (
    <div
      className="flex flex-col items-center justify-center rounded-xl text-white font-heading font-extrabold leading-none p-4"
      style={{ backgroundColor: primaryColor }}
    >
      <span className="text-2xl tracking-tight">{top}</span>
      {bottom && <span className="text-lg tracking-wide" style={{ color: accentColor }}>{bottom}</span>}
    </div>
  );
}

function ColorField({ label, value, onChange }: { label: string; value: string; onChange: (color: string) => void }) {
  return (
    <div>
      <label className="block text-sm font-medium mb-2">{label}</label>
      <div className="flex items-center gap-3">
        <input
          type="color"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className="w-10 h-10 rounded-lg cursor-pointer"
        />
        <span className="text-sm text-muted-foreground font-mono">{value}</span>
      </div>
    </div>
  );
}

function MemberAppPreview({ gymName, primaryColor, accentColor }: Colors & { gymName: string }) {
  return (
    <div className="hidden lg:block">
      <label className="block text-sm font-medium mb-2">Member app preview</label>
      <div
        className="relative mx-auto w-[260px] h-[520px] rounded-[2.25rem] border-[10px] border-zinc-900 bg-white shadow-xl overflow-hidden"
      >
        <div className="absolute top-0 left-1/2 -translate-x-1/2 w-24 h-5 rounded-b-2xl bg-zinc-900 z-10" />
        <div className="h-full overflow-hidden flex flex-col" style={{ backgroundColor: '#f8fafc' }}>
          <div className="px-4 pt-8 pb-4 text-white" style={{ backgroundColor: primaryColor }}>
            <div className="text-[10px] opacity-80">Welcome back</div>
            <div className="text-lg font-bold leading-tight">{gymName || 'Your Gym'}</div>
            <div className="mt-3 rounded-xl bg-white/15 px-3 py-2 backdrop-blur">
              <div className="text-[10px] uppercase tracking-wider opacity-90">Active Plan</div>
              <div className="text-sm font-semibold flex items-center justify-between">
                <span>Monthly</span>
                <span style={{ color: accentColor }}>●</span>
              </div>
            </div>
          </div>
          <div className="px-4 py-3 space-y-2">
            <div className="text-[10px] font-semibold uppercase text-zinc-500 tracking-wider">Quick actions</div>
            <button className="w-full rounded-lg px-3 py-2 text-xs font-semibold text-white" style={{ backgroundColor: primaryColor }}>
              Check In
            </button>
            <button className="w-full rounded-lg px-3 py-2 text-xs font-semibold border" style={{ borderColor: accentColor, color: primaryColor }}>
              Book Session
            </button>
          </div>
          <div className="px-4 py-2">
            <div className="rounded-lg border border-zinc-200 bg-white p-3">
              <div className="text-[10px] font-semibold uppercase text-zinc-500 tracking-wider">Today</div>
              <div className="mt-1 flex items-center justify-between">
                <div className="text-xs font-semibold text-zinc-900">HIIT — 6:00 PM</div>
                <span className="text-[10px] rounded px-1.5 py-0.5 font-bold" style={{ backgroundColor: accentColor, color: '#000' }}>
                  Booked
                </span>
              </div>
            </div>
          </div>
          <div className="mt-auto border-t border-zinc-200 px-4 py-2 flex items-center justify-around text-[10px] text-zinc-500">
            <div className="flex flex-col items-center" style={{ color: primaryColor }}>
              <div className="w-1.5 h-1.5 rounded-full mb-0.5" style={{ backgroundColor: primaryColor }} />
              Home
            </div>
            <div>Classes</div>
            <div>Trainer</div>
            <div>Profile</div>
          </div>
        </div>
      </div>
      <p className="mt-2 text-center text-[11px] text-muted-foreground">Live preview</p>
    </div>
  );
}

export function BrandingSection({ gymName, primaryColor, accentColor, onPrimaryColorChange, onAccentColorChange }: Props) {
  return (
    <SettingsCard title="Branding">
      <div className="grid grid-cols-1 lg:grid-cols-[1fr_auto] gap-8">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label className="block text-sm font-medium mb-2">Logo</label>
            <div className="h-32 rounded-xl border-2 border-dashed border-border flex items-center justify-center cursor-pointer hover:border-primary/50 transition-colors">
              <LogoPreview gymName={gymName} primaryColor={primaryColor} accentColor={accentColor} />
            </div>
          </div>
          <div>
            <label className="block text-sm font-medium mb-2">Cover Photo</label>
            <div className="h-32 rounded-xl border-2 border-dashed border-border flex items-center justify-center text-muted-foreground text-sm cursor-pointer hover:border-primary/50 transition-colors">
              <div className="text-center">
                <p className="text-2xl mb-1">🖼️</p>
                <p>Drag &amp; drop or click to upload</p>
              </div>
            </div>
          </div>
          <ColorField label="Primary Color" value={primaryColor} onChange={onPrimaryColorChange} />
          <ColorField label="Accent Color" value={accentColor} onChange={onAccentColorChange} />
        </div>

        <MemberAppPreview gymName={gymName} primaryColor={primaryColor} accentColor={accentColor} />
      </div>
    </SettingsCard>
  );
}
