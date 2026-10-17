'use client';

import type { Dispatch, ReactNode, SetStateAction } from 'react';
import { StatusBadge } from '@gym360/ui';
import { Settings, Shield, Users, Lock } from 'lucide-react';
import type { PlatformConfig } from './use-settings-actions';

export const SETTINGS_TABS = ['Platform Config', 'Admin Team', 'Security', 'PDPL Compliance'] as const;
export type SettingsTab = (typeof SETTINGS_TABS)[number];

const TAB_ICONS: Record<SettingsTab, typeof Settings> = {
  'Platform Config': Settings,
  'Admin Team': Users,
  Security: Shield,
  'PDPL Compliance': Lock,
};

const INPUT_CLASS = 'w-full h-9 rounded-lg border border-input bg-background px-3 text-sm';

function tabClass(isActive: boolean): string {
  const tone = isActive ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground';
  return `pb-3 text-sm font-medium transition-colors border-b-2 flex items-center gap-2 ${tone}`;
}

export function SettingsTabBar({ activeTab, onChange }: { activeTab: SettingsTab; onChange: (tab: SettingsTab) => void }) {
  return (
    <div className="border-b">
      <div className="flex gap-6">
        {SETTINGS_TABS.map((tab) => {
          const Icon = TAB_ICONS[tab];
          return (
            <button key={tab} onClick={() => onChange(tab)} className={tabClass(activeTab === tab)}>
              <Icon className="h-4 w-4" />
              {tab}
            </button>
          );
        })}
      </div>
    </div>
  );
}

export function LabeledField({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <label className="block text-sm font-medium mb-1.5">{label}</label>
      {children}
    </div>
  );
}

interface PlatformConfigTabProps {
  config: PlatformConfig;
  setConfig: Dispatch<SetStateAction<PlatformConfig>>;
  busy: boolean;
  onSave: () => void;
}

export function PlatformConfigTab({ config, setConfig, busy, onSave }: PlatformConfigTabProps) {
  const set = (key: keyof PlatformConfig) => (value: string) => setConfig((c) => ({ ...c, [key]: value }));
  return (
    <div className="space-y-6">
      <div className="rounded-xl border bg-card p-6 shadow-sm">
        <h3 className="text-sm font-semibold mb-4">General</h3>
        <div className="grid grid-cols-2 gap-4">
          <LabeledField label="Platform Name">
            <input type="text" value={config.platformName} onChange={(e) => set('platformName')(e.target.value)} className={INPUT_CLASS} />
          </LabeledField>
          <LabeledField label="Default Language">
            <select value={config.defaultLanguage} onChange={(e) => set('defaultLanguage')(e.target.value)} className={INPUT_CLASS}>
              <option value="ar">Arabic</option>
              <option value="en">English</option>
            </select>
          </LabeledField>
          <LabeledField label="Default Currency">
            <input type="text" value={config.defaultCurrency} onChange={(e) => set('defaultCurrency')(e.target.value)} className={INPUT_CLASS} />
          </LabeledField>
          <LabeledField label="Support Email">
            <input type="email" value={config.supportEmail} onChange={(e) => set('supportEmail')(e.target.value)} className={INPUT_CLASS} />
          </LabeledField>
        </div>
        <button
          onClick={onSave}
          disabled={busy}
          className="mt-4 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
        >
          {busy ? 'Saving...' : 'Save Changes'}
        </button>
      </div>
    </div>
  );
}

function SettingRow({ title, hint, last = false, children }: { title: string; hint: string; last?: boolean; children: ReactNode }) {
  return (
    <div className={last ? 'flex items-center justify-between py-3' : 'flex items-center justify-between py-3 border-b'}>
      <div>
        <p className="text-sm font-medium">{title}</p>
        <p className="text-xs text-muted-foreground">{hint}</p>
      </div>
      {children}
    </div>
  );
}

function ToggleSwitch({ on, onToggle, ariaLabel }: { on: boolean; onToggle: () => void; ariaLabel: string }) {
  return (
    <button
      onClick={onToggle}
      aria-label={ariaLabel}
      aria-pressed={on}
      className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${on ? 'bg-emerald-500' : 'bg-gray-300'}`}
    >
      <span className={`inline-block h-4 w-4 transform rounded-full bg-white transition ${on ? 'translate-x-6' : 'translate-x-1'}`} />
    </button>
  );
}

interface SecurityTabProps {
  twoFactorOn: boolean;
  ipWhitelistOn: boolean;
  toggleTwoFactor: () => void;
  toggleIpWhitelist: () => void;
}

export function SecurityTab({ twoFactorOn, ipWhitelistOn, toggleTwoFactor, toggleIpWhitelist }: SecurityTabProps) {
  return (
    <div className="rounded-xl border bg-card p-6 shadow-sm">
      <h3 className="text-sm font-semibold mb-4">Security Settings</h3>
      <div className="space-y-4">
        <SettingRow title="Two-Factor Authentication" hint="Require 2FA for all admin accounts">
          <ToggleSwitch on={twoFactorOn} onToggle={toggleTwoFactor} ariaLabel="Toggle two-factor authentication" />
        </SettingRow>
        <SettingRow title="Session Timeout" hint="Auto-logout after inactivity">
          <select className="h-9 rounded-lg border border-input bg-background px-3 text-sm">
            <option>30 minutes</option><option>1 hour</option><option>2 hours</option><option>4 hours</option>
          </select>
        </SettingRow>
        <SettingRow title="IP Whitelisting" hint="Restrict admin access to specific IPs" last>
          <ToggleSwitch on={ipWhitelistOn} onToggle={toggleIpWhitelist} ariaLabel="Toggle IP whitelisting" />
        </SettingRow>
      </div>
    </div>
  );
}

const PDPL_ITEMS: { label: string; status: string }[] = [
  { label: 'Consent Tracking', status: 'Compliant' },
  { label: 'Data Export (My Data)', status: 'Enabled' },
  { label: 'Account Deletion', status: 'Enabled' },
  { label: 'Encryption at Rest (TDE)', status: 'Active' },
  { label: 'Data Retention Policy', status: 'Configured' },
  { label: 'Audit Logging', status: 'Active' },
];

export function PdplComplianceTab() {
  return (
    <div className="space-y-6">
      <div className="rounded-xl border bg-card p-6 shadow-sm">
        <h3 className="text-sm font-semibold mb-4 flex items-center gap-2">
          <Shield className="h-4 w-4 text-primary" />
          Egypt PDPL Compliance Status
        </h3>
        <div className="grid grid-cols-2 gap-4">
          {PDPL_ITEMS.map((item) => (
            <div key={item.label} className="flex items-center justify-between py-3 border-b">
              <span className="text-sm">{item.label}</span>
              <StatusBadge variant="active" label={item.status} />
            </div>
          ))}
        </div>
      </div>
      <div className="rounded-xl border bg-card p-6 shadow-sm">
        <h3 className="text-sm font-semibold mb-4">Data Retention Settings</h3>
        <div className="grid grid-cols-2 gap-4">
          <LabeledField label="Deleted Account Data Retention">
            <select className={INPUT_CLASS}>
              <option>30 days</option><option>60 days</option><option>90 days</option>
            </select>
          </LabeledField>
          <LabeledField label="Audit Log Retention">
            <select className={INPUT_CLASS}>
              <option>1 year</option><option>2 years</option><option>5 years</option>
            </select>
          </LabeledField>
        </div>
      </div>
    </div>
  );
}
