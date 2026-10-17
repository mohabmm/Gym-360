'use client';

import { useState } from 'react';
import { PageHeader, ConfirmModal } from '@gym360/ui';
import { useAdminUsers } from '@/lib/hooks';
import { useSecuritySettings, useSettingsActions, useSettingsToasts } from '@/components/settings/use-settings-actions';
import {
  PdplComplianceTab,
  PlatformConfigTab,
  SecurityTab,
  SettingsTabBar,
  type SettingsTab,
} from '@/components/settings/settings-tabs';
import { AdminTeamTab } from '@/components/settings/admin-team-tab';

function Toast({ message, tone }: { message: string | null; tone: string }) {
  if (!message) return null;
  return (
    <div className={`fixed top-4 right-4 z-50 rounded-lg ${tone} px-4 py-2 text-sm font-medium text-white shadow-lg`}>
      {message}
    </div>
  );
}

export default function SettingsPage() {
  const [activeTab, setActiveTab] = useState<SettingsTab>('Platform Config');
  const toasts = useSettingsToasts();
  const security = useSecuritySettings(toasts);
  const actions = useSettingsActions(toasts);
  const { data: adminUsersData } = useAdminUsers();
  const adminUsers = (adminUsersData?.data ?? []).filter(
    (u) => u.role === 'admin' || u.role === 'platform_admin',
  );
  const { deleteAdmin } = actions;

  return (
    <div className="space-y-6">
      <Toast message={toasts.toast} tone="bg-emerald-600" />
      <Toast message={toasts.errorToast} tone="bg-red-600" />
      <PageHeader
        title="Settings"
        description="Platform configuration and administration"
      />

      {/* Tabs */}
      <SettingsTabBar activeTab={activeTab} onChange={setActiveTab} />

      {activeTab === 'Platform Config' && (
        <PlatformConfigTab
          config={actions.platformConfig}
          setConfig={actions.setPlatformConfig}
          busy={actions.busy}
          onSave={actions.savePlatformConfig}
        />
      )}

      {activeTab === 'Admin Team' && <AdminTeamTab adminUsers={adminUsers} actions={actions} />}

      {activeTab === 'Security' && <SecurityTab {...security} />}

      <ConfirmModal
        open={!!deleteAdmin}
        onClose={() => actions.setDeleteAdmin(null)}
        onConfirm={actions.removeAdmin}
        title="Remove Admin User"
        description={`Remove ${deleteAdmin?.firstName ?? ''} ${deleteAdmin?.lastName ?? ''} from the admin team? They will immediately lose access to the admin dashboard.`}
        confirmLabel="Remove"
        variant="danger"
        isLoading={actions.busy}
      />

      {activeTab === 'PDPL Compliance' && <PdplComplianceTab />}
    </div>
  );
}
