'use client';

import { useEffect, useState } from 'react';
import { PageHeader } from '@gym360/ui';
import { Check, MapPin, Bell } from 'lucide-react';
import { toast } from 'sonner';
import { useGym, useUpdateGym, useUpdateBranding } from '@/lib/hooks/useGym';

const amenities = [
  { icon: '🅿️', label: 'Parking', checked: true },
  { icon: '🚿', label: 'Showers', checked: true },
  { icon: '🔐', label: 'Lockers', checked: true },
  { icon: '📶', label: 'WiFi', checked: true },
  { icon: '🧖', label: 'Sauna', checked: true },
  { icon: '♨️', label: 'Steam Room', checked: true },
  { icon: '🛁', label: 'Jacuzzi', checked: true },
  { icon: '🏊', label: 'Pool', checked: false },
  { icon: '🧃', label: 'Juice Bar', checked: false },
  { icon: '🛍️', label: 'Pro Shop', checked: false },
];

const notificationSettings = [
  { label: 'Session Reminders', desc: 'Remind members about upcoming PT sessions', enabled: true },
  { label: 'Expiry Warnings', desc: 'Notify members when membership is expiring', enabled: true },
  { label: 'Announcements', desc: 'Push announcements to member app', enabled: true },
  { label: 'Check-in Confirmation', desc: 'Send confirmation after check-in', enabled: false },
];

export default function SettingsPage() {
  const { data: gym } = useGym();
  const updateGym = useUpdateGym();
  const updateBranding = useUpdateBranding();
  const [gymName, setGymName] = useState('');
  const [gymDesc, setGymDesc] = useState('');
  const [phone, setPhone] = useState('');
  const [altPhones, setAltPhones] = useState('');
  const [email, setEmail] = useState('');
  const [website, setWebsite] = useState('');
  const [street, setStreet] = useState('');
  const [city, setCity] = useState('');
  const [area, setArea] = useState('');
  const [customAmenity, setCustomAmenity] = useState('');
  const [notifications, setNotifications] = useState(notificationSettings);
  const [primaryColor, setPrimaryColor] = useState('#C41E3A');
  const [accentColor, setAccentColor] = useState('#D4AF37');
  // Only push branding when the user actually edits a color — branding is a
  // Pro-gated endpoint (400 otherwise), so an untouched save must not call it
  // and sink an otherwise-fine profile save.
  const [colorsTouched, setColorsTouched] = useState(false);

  useEffect(() => {
    if (!gym) return;
    setGymName(gym.name ?? '');
    setGymDesc(gym.description ?? '');
    setPhone(gym.phone ?? '');
    setEmail(gym.email ?? '');
    setWebsite(gym.website ?? '');
    setStreet(gym.street ?? '');
    setCity(gym.city ?? '');
    setArea(gym.area ?? '');
    if (gym.primaryColor) setPrimaryColor(gym.primaryColor);
    if (gym.secondaryColor) setAccentColor(gym.secondaryColor);
  }, [gym]);

  const errMsg = (err: unknown, fallback: string) =>
    (err as { response?: { data?: { errors?: { message?: string }[] } } })
      ?.response?.data?.errors?.[0]?.message ?? fallback;

  const handleSave = async () => {
    // Profile save first — independent of branding so a Pro-gated branding
    // failure can't mask a successful profile save.
    try {
      await updateGym.mutateAsync({
        name: gymName,
        description: gymDesc,
        phone,
        email,
        website,
        street,
        city,
        area,
      });
    } catch (err: unknown) {
      toast.error(errMsg(err, 'Failed to save settings.'));
      return;
    }

    // Branding only when the user actually changed a color. Dedicated endpoint;
    // the BE replaces the whole Branding object, so pass through existing
    // logo/cover to avoid wiping them.
    if (colorsTouched) {
      try {
        await updateBranding.mutateAsync({
          primaryColor,
          secondaryColor: accentColor,
          logoUrl: gym?.logoUrl,
          coverPhotoUrl: gym?.coverPhotoUrl,
        });
        setColorsTouched(false);
      } catch (err: unknown) {
        toast.error(errMsg(err, 'Profile saved, but branding colors need a Pro plan.'));
        return;
      }
    }

    toast.success('Settings saved successfully!');
  };

  const toggleNotification = (index: number) => {
    setNotifications((prev) =>
      prev.map((n, i) => (i === index ? { ...n, enabled: !n.enabled } : n))
    );
  };

  return (
    <div className="space-y-6">
      <PageHeader title="Settings" description="Manage your gym profile and preferences" />

      {/* Gym Profile */}
      <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
        <h3 className="font-heading font-semibold text-base mb-4">Gym Profile</h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium mb-1.5">Gym Name</label>
            <input
              value={gymName}
              onChange={(e) => setGymName(e.target.value)}
              className="w-full h-10 px-3 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Phone</label>
            <input
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              className="w-full h-10 px-3 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Alt Phones</label>
            <input
              value={altPhones}
              onChange={(e) => setAltPhones(e.target.value)}
              className="w-full h-10 px-3 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Email</label>
            <input
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="w-full h-10 px-3 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Website</label>
            <input
              value={website}
              onChange={(e) => setWebsite(e.target.value)}
              placeholder="https://firstgym.eg"
              className="w-full h-10 px-3 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Address</label>
            <input
              value={street}
              onChange={(e) => setStreet(e.target.value)}
              className="w-full h-10 px-3 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">City / Area</label>
            <div className="flex gap-2">
              <input
                value={city}
                onChange={(e) => setCity(e.target.value)}
                className="flex-1 h-10 px-3 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
              />
              <input
                value={area}
                onChange={(e) => setArea(e.target.value)}
                className="flex-1 h-10 px-3 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
              />
            </div>
          </div>
          <div className="md:col-span-2">
            <label className="block text-sm font-medium mb-1.5">Description</label>
            <textarea
              value={gymDesc}
              onChange={(e) => setGymDesc(e.target.value)}
              rows={3}
              className="w-full px-3 py-2 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30 resize-none"
            />
          </div>
        </div>

        {/* Map placeholder */}
        <div className="mt-4">
          <label className="block text-sm font-medium mb-1.5">Location</label>
          <div className="h-40 rounded-xl border-2 border-dashed border-border bg-muted/30 flex flex-col items-center justify-center text-muted-foreground">
            <MapPin size={28} className="mb-2 text-primary/50" />
            <p className="text-sm font-medium">{street}</p>
            <p className="text-xs">{city}, {area}</p>
            <p className="text-xs mt-2 text-muted-foreground/60">Map integration available in production</p>
          </div>
        </div>

        <button
          onClick={handleSave}
          className="mt-4 inline-flex items-center gap-2 h-10 px-5 rounded-xl bg-primary text-primary-foreground font-medium text-sm hover:bg-primary/90 transition-colors"
        >
          <Check size={16} /> Save Changes
        </button>
      </div>

      {/* Operating Hours */}
      <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
        <div className="flex items-center justify-between mb-4">
          <h3 className="font-heading font-semibold text-base">Operating Hours</h3>
          <label className="flex items-center gap-2 text-sm font-medium cursor-pointer">
            <input type="checkbox" defaultChecked className="rounded" />
            24/7 Gym
          </label>
        </div>
        <p className="text-sm text-muted-foreground">
          Your gym is set as 24/7 — open all day, every day!
        </p>
      </div>

      {/* Branding */}
      <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
        <h3 className="font-heading font-semibold text-base mb-4">Branding</h3>
        <div className="grid grid-cols-1 lg:grid-cols-[1fr_auto] gap-8">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label className="block text-sm font-medium mb-2">Logo</label>
              <div className="h-32 rounded-xl border-2 border-dashed border-border flex items-center justify-center cursor-pointer hover:border-primary/50 transition-colors">
                <div
                  className="flex flex-col items-center justify-center rounded-xl text-white font-heading font-extrabold leading-none p-4"
                  style={{ backgroundColor: primaryColor }}
                >
                  {(() => {
                    const words = (gymName || 'Gym').trim().split(/\s+/).filter(Boolean);
                    const top = (words[0] ?? 'GYM').toUpperCase().slice(0, 6);
                    const bottom = (words.slice(1).join(' ') || '').toUpperCase().slice(0, 6);
                    return (
                      <>
                        <span className="text-2xl tracking-tight">{top}</span>
                        {bottom && <span className="text-lg tracking-wide" style={{ color: accentColor }}>{bottom}</span>}
                      </>
                    );
                  })()}
                </div>
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
            <div>
              <label className="block text-sm font-medium mb-2">Primary Color</label>
              <div className="flex items-center gap-3">
                <input
                  type="color"
                  value={primaryColor}
                  onChange={(e) => { setPrimaryColor(e.target.value); setColorsTouched(true); }}
                  className="w-10 h-10 rounded-lg cursor-pointer"
                />
                <span className="text-sm text-muted-foreground font-mono">{primaryColor}</span>
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium mb-2">Accent Color</label>
              <div className="flex items-center gap-3">
                <input
                  type="color"
                  value={accentColor}
                  onChange={(e) => { setAccentColor(e.target.value); setColorsTouched(true); }}
                  className="w-10 h-10 rounded-lg cursor-pointer"
                />
                <span className="text-sm text-muted-foreground font-mono">{accentColor}</span>
              </div>
            </div>
          </div>

          {/* Live phone preview */}
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
        </div>
      </div>

      {/* Amenities */}
      <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
        <h3 className="font-heading font-semibold text-base mb-4">Amenities</h3>
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          {amenities.map((a) => (
            <label
              key={a.label}
              className="flex items-center gap-3 p-3 rounded-xl border border-border hover:bg-muted/50 transition-colors cursor-pointer"
            >
              <input type="checkbox" defaultChecked={a.checked} className="rounded" />
              <span className="text-lg">{a.icon}</span>
              <span className="text-sm font-medium">{a.label}</span>
            </label>
          ))}
          <label className="flex items-center gap-3 p-3 rounded-xl border border-border hover:bg-muted/50 transition-colors cursor-pointer">
            <input type="checkbox" defaultChecked className="rounded" />
            <span className="text-lg">👩</span>
            <span className="text-sm font-medium">Ladies Floor</span>
          </label>
        </div>
        <div className="mt-3 flex items-center gap-2">
          <input
            value={customAmenity}
            onChange={(e) => setCustomAmenity(e.target.value)}
            placeholder="Add custom amenity…"
            className="flex-1 h-9 px-3 rounded-lg border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
          />
          <button
            onClick={() => {
              if (customAmenity) {
                toast.success(`"${customAmenity}" added`);
                setCustomAmenity('');
              }
            }}
            className="h-9 px-4 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 transition-colors"
          >
            Add
          </button>
        </div>
      </div>

      {/* Social Media */}
      <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
        <h3 className="font-heading font-semibold text-base mb-4">Social Media</h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div>
            <label className="block text-sm font-medium mb-1.5">Facebook</label>
            <input
              defaultValue="facebook.com/First.Gym.EG"
              className="w-full h-10 px-3 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">Instagram</label>
            <input
              defaultValue=""
              placeholder="@firstgym"
              className="w-full h-10 px-3 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
            />
          </div>
          <div>
            <label className="block text-sm font-medium mb-1.5">TikTok</label>
            <input
              defaultValue=""
              placeholder="@firstgym"
              className="w-full h-10 px-3 rounded-xl border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
            />
          </div>
        </div>
      </div>

      {/* Notifications */}
      <div className="rounded-xl border border-border bg-card p-6 shadow-sm">
        <h3 className="font-heading font-semibold text-base mb-4 flex items-center gap-2">
          <Bell size={16} className="text-primary" />
          Notifications
        </h3>
        <div className="space-y-3">
          {notifications.map((n, index) => (
            <div
              key={n.label}
              className="flex items-center justify-between p-3 rounded-xl border border-border hover:bg-muted/30 transition-colors"
            >
              <div>
                <p className="text-sm font-medium">{n.label}</p>
                <p className="text-xs text-muted-foreground">{n.desc}</p>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input
                  type="checkbox"
                  checked={n.enabled}
                  onChange={() => toggleNotification(index)}
                  className="sr-only peer"
                />
                <div className="w-10 h-5 bg-muted rounded-full peer peer-checked:bg-primary peer-checked:after:translate-x-5 after:content-[''] after:absolute after:top-0.5 after:left-0.5 after:bg-white after:rounded-full after:h-4 after:w-4 after:transition-all" />
              </label>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
