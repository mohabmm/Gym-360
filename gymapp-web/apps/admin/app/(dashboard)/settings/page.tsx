'use client';

import { useState, useEffect } from 'react';
import {
  PageHeader,
  StatusBadge,
  DataTable,
  Modal,
  ConfirmModal,
  apiPost,
  apiPatch,
  apiDelete,
  ADMIN,
  IDENTITY,
  useInvalidateQueries,
  useApiQuery,
} from '@gymapp/ui';
import { type ColumnDef } from '@tanstack/react-table';
import { Settings, Shield, Users, Database, Plus, Trash2, Lock } from 'lucide-react';
import { useAdminUsers } from '@/lib/hooks';
import type { AdminUser } from '@gymapp/ui';

const tabs = ['Platform Config', 'Admin Team', 'Security', 'PDPL Compliance'] as const;

export default function SettingsPage() {
  const [activeTab, setActiveTab] = useState<(typeof tabs)[number]>('Platform Config');
  const [showAddAdmin, setShowAddAdmin] = useState(false);
  const [addAdminForm, setAddAdminForm] = useState({
    email: '',
    password: '',
    firstName: '',
    lastName: '',
    adminRole: 'admin' as 'super_admin' | 'admin' | 'support' | 'analyst',
  });
  const [deleteAdmin, setDeleteAdmin] = useState<AdminUser | null>(null);
  const [twoFactorOn, setTwoFactorOn] = useState(true);
  const [ipWhitelistOn, setIpWhitelistOn] = useState(false);
  const { data: securityData } = useApiQuery<{ twoFactorRequired: boolean; ipWhitelistEnabled: boolean }>(
    ['security-settings'],
    ADMIN.SECURITY_SETTINGS,
  );

  useEffect(() => {
    if (securityData?.data) {
      setTwoFactorOn(securityData.data.twoFactorRequired ?? true);
      setIpWhitelistOn(securityData.data.ipWhitelistEnabled ?? false);
    }
  }, [securityData]);
  const [busy, setBusy] = useState(false);
  const [toast, setToast] = useState<string | null>(null);
  const [errorToast, setErrorToast] = useState<string | null>(null);
  const [platformConfig, setPlatformConfig] = useState({
    platformName: 'Gym 360',
    defaultLanguage: 'ar',
    defaultCurrency: 'EGP',
    supportEmail: 'support@gym360.com',
  });
  const { invalidate } = useInvalidateQueries();
  const { data: adminUsersData } = useAdminUsers();
  const adminUsers = (adminUsersData?.data ?? []).filter(
    (u) => u.role === 'admin' || u.role === 'platform_admin',
  );

  const flash = (msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 2500);
  };

  const flashError = (msg: string) => {
    setErrorToast(msg);
    setTimeout(() => setErrorToast(null), 4000);
  };

  const handleSavePlatformConfig = async () => {
    setBusy(true);
    try {
      await apiPatch(ADMIN.PLATFORM_CONFIG, platformConfig);
      flash('Platform config saved');
    } catch (err: unknown) {
      flashError(err instanceof Error ? err.message : 'Failed to save platform config');
    } finally {
      setBusy(false);
    }
  };

  const handleDeleteAdmin = async () => {
    if (!deleteAdmin) return;
    setBusy(true);
    try {
      await apiDelete(IDENTITY.ADMIN_USER(deleteAdmin.id));
      invalidate(['admin-users']);
      setDeleteAdmin(null);
      flash('Admin user removed');
    } catch (err: unknown) {
      flashError(err instanceof Error ? err.message : 'Failed to remove admin user');
    } finally {
      setBusy(false);
    }
  };

  const handleAddAdmin = async () => {
    if (!addAdminForm.email.trim() || !addAdminForm.firstName.trim() || !addAdminForm.lastName.trim()) return;
    setBusy(true);
    try {
      await apiPost(IDENTITY.ADMIN_USERS, addAdminForm);
      invalidate(['admin-users']);
      setShowAddAdmin(false);
      setAddAdminForm({ email: '', password: '', firstName: '', lastName: '', adminRole: 'admin' });
      flash('Admin user added');
    } catch (err: unknown) {
      flashError(err instanceof Error ? err.message : 'Failed to add admin user');
    } finally {
      setBusy(false);
    }
  };

  const handleToggleTwoFactor = async () => {
    const next = !twoFactorOn;
    setTwoFactorOn(next);
    try {
      await apiPatch(ADMIN.SECURITY_SETTINGS, { twoFactorRequired: next });
      flash(`2FA ${next ? 'enabled' : 'disabled'}`);
    } catch {
      setTwoFactorOn(!next);
      flashError('Failed to update 2FA setting');
    }
  };

  const handleToggleIpWhitelist = async () => {
    const next = !ipWhitelistOn;
    setIpWhitelistOn(next);
    try {
      await apiPatch(ADMIN.SECURITY_SETTINGS, { ipWhitelistEnabled: next });
      flash(`IP whitelist ${next ? 'enabled' : 'disabled'}`);
    } catch {
      setIpWhitelistOn(!next);
      flashError('Failed to update IP whitelist setting');
    }
  };

  const adminColumns: ColumnDef<AdminUser, unknown>[] = [
    {
      accessorKey: 'firstName',
      header: 'Name',
      cell: ({ row }) => (
        <div className="flex items-center gap-3">
          <div className="h-8 w-8 rounded-full bg-primary/10 flex items-center justify-center text-xs font-bold text-primary">
            {row.original.firstName[0]}{row.original.lastName[0]}
          </div>
          <div>
            <p className="font-medium">{row.original.firstName} {row.original.lastName}</p>
            <p className="text-xs text-muted-foreground">{row.original.email}</p>
          </div>
        </div>
      ),
    },
    {
      accessorKey: 'adminRole',
      header: 'Role',
      cell: ({ row }) =>
        row.original.adminRole ? (
          <StatusBadge variant={row.original.adminRole} />
        ) : (
          <span className="text-xs text-muted-foreground">—</span>
        ),
    },
    {
      accessorKey: 'status',
      header: 'Status',
      cell: ({ row }) => <StatusBadge variant={row.original.status} dot />,
    },
    {
      id: 'actions',
      header: '',
      cell: ({ row }) => (
        <div className="flex items-center gap-1">
          {!row.original.isYou && (
            <button
              onClick={() => setDeleteAdmin(row.original)}
              aria-label={`Remove ${row.original.firstName} ${row.original.lastName}`}
              className="p-1.5 rounded-lg hover:bg-muted text-muted-foreground hover:text-red-500"
            >
              <Trash2 className="h-3.5 w-3.5" />
            </button>
          )}
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      {toast && (
        <div className="fixed top-4 right-4 z-50 rounded-lg bg-emerald-600 px-4 py-2 text-sm font-medium text-white shadow-lg">
          {toast}
        </div>
      )}
      {errorToast && (
        <div className="fixed top-4 right-4 z-50 rounded-lg bg-red-600 px-4 py-2 text-sm font-medium text-white shadow-lg">
          {errorToast}
        </div>
      )}
      <PageHeader
        title="Settings"
        description="Platform configuration and administration"
      />

      {/* Tabs */}
      <div className="border-b">
        <div className="flex gap-6">
          {tabs.map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`pb-3 text-sm font-medium transition-colors border-b-2 flex items-center gap-2 ${
                activeTab === tab ? 'border-primary text-primary' : 'border-transparent text-muted-foreground hover:text-foreground'
              }`}
            >
              {tab === 'Platform Config' && <Settings className="h-4 w-4" />}
              {tab === 'Admin Team' && <Users className="h-4 w-4" />}
              {tab === 'Security' && <Shield className="h-4 w-4" />}
              {tab === 'PDPL Compliance' && <Lock className="h-4 w-4" />}
              {tab}
            </button>
          ))}
        </div>
      </div>

      {activeTab === 'Platform Config' && (
        <div className="space-y-6">
          <div className="rounded-xl border bg-card p-6 shadow-sm">
            <h3 className="text-sm font-semibold mb-4">General</h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium mb-1.5">Platform Name</label>
                <input
                  type="text"
                  value={platformConfig.platformName}
                  onChange={(e) => setPlatformConfig((c) => ({ ...c, platformName: e.target.value }))}
                  className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1.5">Default Language</label>
                <select
                  value={platformConfig.defaultLanguage}
                  onChange={(e) => setPlatformConfig((c) => ({ ...c, defaultLanguage: e.target.value }))}
                  className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
                >
                  <option value="ar">Arabic</option>
                  <option value="en">English</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1.5">Default Currency</label>
                <input
                  type="text"
                  value={platformConfig.defaultCurrency}
                  onChange={(e) => setPlatformConfig((c) => ({ ...c, defaultCurrency: e.target.value }))}
                  className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1.5">Support Email</label>
                <input
                  type="email"
                  value={platformConfig.supportEmail}
                  onChange={(e) => setPlatformConfig((c) => ({ ...c, supportEmail: e.target.value }))}
                  className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
                />
              </div>
            </div>
            <button
              onClick={handleSavePlatformConfig}
              disabled={busy}
              className="mt-4 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
            >
              {busy ? 'Saving...' : 'Save Changes'}
            </button>
          </div>
        </div>
      )}

      {activeTab === 'Admin Team' && (
        <div className="space-y-4">
          <div className="flex items-center justify-between">
            <p className="text-sm text-muted-foreground">
              Manage platform administrators. Only super_admin can access this section.
            </p>
            <button
              onClick={() => setShowAddAdmin(true)}
              className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90"
            >
              <Plus className="h-4 w-4" /> Add Admin
            </button>
          </div>
          <DataTable data={adminUsers} columns={adminColumns} />
          <Modal
            open={showAddAdmin}
            onClose={() => setShowAddAdmin(false)}
            title="Add Admin User"
            size="md"
            footer={
              <>
                <button onClick={() => setShowAddAdmin(false)} className="rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted">Cancel</button>
                <button
                  onClick={handleAddAdmin}
                  disabled={busy || !addAdminForm.email.trim() || !addAdminForm.firstName.trim() || !addAdminForm.lastName.trim()}
                  className="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white hover:bg-primary/90 disabled:opacity-50"
                >
                  {busy ? 'Adding...' : 'Add Admin'}
                </button>
              </>
            }
          >
            <div className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="block text-sm font-medium mb-1.5">First Name</label>
                  <input
                    type="text"
                    value={addAdminForm.firstName}
                    onChange={(e) => setAddAdminForm((f) => ({ ...f, firstName: e.target.value }))}
                    className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium mb-1.5">Last Name</label>
                  <input
                    type="text"
                    value={addAdminForm.lastName}
                    onChange={(e) => setAddAdminForm((f) => ({ ...f, lastName: e.target.value }))}
                    className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1.5">Email</label>
                <input
                  type="email"
                  value={addAdminForm.email}
                  onChange={(e) => setAddAdminForm((f) => ({ ...f, email: e.target.value }))}
                  className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1.5">Temporary Password (optional)</label>
                <input
                  type="text"
                  value={addAdminForm.password}
                  placeholder="Leave blank for a generated password"
                  onChange={(e) => setAddAdminForm((f) => ({ ...f, password: e.target.value }))}
                  className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1.5">Role</label>
                <select
                  value={addAdminForm.adminRole}
                  onChange={(e) => setAddAdminForm((f) => ({ ...f, adminRole: e.target.value as typeof f.adminRole }))}
                  className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm"
                >
                  <option value="super_admin">Super Admin</option>
                  <option value="admin">Admin</option>
                  <option value="support">Support</option>
                  <option value="analyst">Analyst</option>
                </select>
              </div>
            </div>
          </Modal>
        </div>
      )}

      {activeTab === 'Security' && (
        <div className="rounded-xl border bg-card p-6 shadow-sm">
          <h3 className="text-sm font-semibold mb-4">Security Settings</h3>
          <div className="space-y-4">
            <div className="flex items-center justify-between py-3 border-b">
              <div>
                <p className="text-sm font-medium">Two-Factor Authentication</p>
                <p className="text-xs text-muted-foreground">Require 2FA for all admin accounts</p>
              </div>
              <button
                onClick={handleToggleTwoFactor}
                aria-label="Toggle two-factor authentication"
                aria-pressed={twoFactorOn}
                className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${twoFactorOn ? 'bg-emerald-500' : 'bg-gray-300'}`}
              >
                <span className={`inline-block h-4 w-4 transform rounded-full bg-white transition ${twoFactorOn ? 'translate-x-6' : 'translate-x-1'}`} />
              </button>
            </div>
            <div className="flex items-center justify-between py-3 border-b">
              <div>
                <p className="text-sm font-medium">Session Timeout</p>
                <p className="text-xs text-muted-foreground">Auto-logout after inactivity</p>
              </div>
              <select className="h-9 rounded-lg border border-input bg-background px-3 text-sm">
                <option>30 minutes</option><option>1 hour</option><option>2 hours</option><option>4 hours</option>
              </select>
            </div>
            <div className="flex items-center justify-between py-3">
              <div>
                <p className="text-sm font-medium">IP Whitelisting</p>
                <p className="text-xs text-muted-foreground">Restrict admin access to specific IPs</p>
              </div>
              <button
                onClick={handleToggleIpWhitelist}
                aria-label="Toggle IP whitelisting"
                aria-pressed={ipWhitelistOn}
                className={`relative inline-flex h-6 w-11 items-center rounded-full transition-colors ${ipWhitelistOn ? 'bg-emerald-500' : 'bg-gray-300'}`}
              >
                <span className={`inline-block h-4 w-4 transform rounded-full bg-white transition ${ipWhitelistOn ? 'translate-x-6' : 'translate-x-1'}`} />
              </button>
            </div>
          </div>
        </div>
      )}

      <ConfirmModal
        open={!!deleteAdmin}
        onClose={() => setDeleteAdmin(null)}
        onConfirm={handleDeleteAdmin}
        title="Remove Admin User"
        description={`Remove ${deleteAdmin?.firstName ?? ''} ${deleteAdmin?.lastName ?? ''} from the admin team? They will immediately lose access to the admin dashboard.`}
        confirmLabel="Remove"
        variant="danger"
        isLoading={busy}
      />

      {activeTab === 'PDPL Compliance' && (
        <div className="space-y-6">
          <div className="rounded-xl border bg-card p-6 shadow-sm">
            <h3 className="text-sm font-semibold mb-4 flex items-center gap-2">
              <Shield className="h-4 w-4 text-primary" />
              Egypt PDPL Compliance Status
            </h3>
            <div className="grid grid-cols-2 gap-4">
              <div className="flex items-center justify-between py-3 border-b">
                <span className="text-sm">Consent Tracking</span>
                <StatusBadge variant="active" label="Compliant" />
              </div>
              <div className="flex items-center justify-between py-3 border-b">
                <span className="text-sm">Data Export (My Data)</span>
                <StatusBadge variant="active" label="Enabled" />
              </div>
              <div className="flex items-center justify-between py-3 border-b">
                <span className="text-sm">Account Deletion</span>
                <StatusBadge variant="active" label="Enabled" />
              </div>
              <div className="flex items-center justify-between py-3 border-b">
                <span className="text-sm">Encryption at Rest (TDE)</span>
                <StatusBadge variant="active" label="Active" />
              </div>
              <div className="flex items-center justify-between py-3 border-b">
                <span className="text-sm">Data Retention Policy</span>
                <StatusBadge variant="active" label="Configured" />
              </div>
              <div className="flex items-center justify-between py-3 border-b">
                <span className="text-sm">Audit Logging</span>
                <StatusBadge variant="active" label="Active" />
              </div>
            </div>
          </div>
          <div className="rounded-xl border bg-card p-6 shadow-sm">
            <h3 className="text-sm font-semibold mb-4">Data Retention Settings</h3>
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium mb-1.5">Deleted Account Data Retention</label>
                <select className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm">
                  <option>30 days</option><option>60 days</option><option>90 days</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1.5">Audit Log Retention</label>
                <select className="w-full h-9 rounded-lg border border-input bg-background px-3 text-sm">
                  <option>1 year</option><option>2 years</option><option>5 years</option>
                </select>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
