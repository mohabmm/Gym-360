import { useState } from 'react';
import { Bell } from 'lucide-react';
import { toast } from 'sonner';
import { Field, INPUT_CLASS, SettingsCard } from './SettingsCard';

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
  { icon: '👩', label: 'Ladies Floor', checked: true },
];

const socialLinks = [
  { label: 'Facebook', defaultValue: 'facebook.com/First.Gym.EG' },
  { label: 'Instagram', defaultValue: '', placeholder: '@firstgym' },
  { label: 'TikTok', defaultValue: '', placeholder: '@firstgym' },
];

const notificationSettings = [
  { label: 'Session Reminders', desc: 'Remind members about upcoming PT sessions', enabled: true },
  { label: 'Expiry Warnings', desc: 'Notify members when membership is expiring', enabled: true },
  { label: 'Announcements', desc: 'Push announcements to member app', enabled: true },
  { label: 'Check-in Confirmation', desc: 'Send confirmation after check-in', enabled: false },
];

export function OperatingHoursSection() {
  return (
    <SettingsCard>
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
    </SettingsCard>
  );
}

export function AmenitiesSection() {
  const [customAmenity, setCustomAmenity] = useState('');

  const addCustomAmenity = () => {
    if (!customAmenity) return;
    toast.success(`"${customAmenity}" added`);
    setCustomAmenity('');
  };

  return (
    <SettingsCard title="Amenities">
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
      </div>
      <div className="mt-3 flex items-center gap-2">
        <input
          value={customAmenity}
          onChange={(e) => setCustomAmenity(e.target.value)}
          placeholder="Add custom amenity…"
          className="flex-1 h-9 px-3 rounded-lg border border-border bg-background text-sm focus:outline-none focus:ring-2 focus:ring-primary/30"
        />
        <button
          onClick={addCustomAmenity}
          className="h-9 px-4 rounded-lg bg-primary text-primary-foreground text-sm font-medium hover:bg-primary/90 transition-colors"
        >
          Add
        </button>
      </div>
    </SettingsCard>
  );
}

export function SocialMediaSection() {
  return (
    <SettingsCard title="Social Media">
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        {socialLinks.map((link) => (
          <Field key={link.label} label={link.label}>
            <input defaultValue={link.defaultValue} placeholder={link.placeholder} className={INPUT_CLASS} />
          </Field>
        ))}
      </div>
    </SettingsCard>
  );
}

export function NotificationsSection() {
  const [notifications, setNotifications] = useState(notificationSettings);

  const toggleNotification = (index: number) => {
    setNotifications((prev) =>
      prev.map((n, i) => (i === index ? { ...n, enabled: !n.enabled } : n))
    );
  };

  return (
    <SettingsCard>
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
    </SettingsCard>
  );
}
