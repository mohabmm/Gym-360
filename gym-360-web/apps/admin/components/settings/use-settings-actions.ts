'use client';

import { useState, useEffect } from 'react';
import {
  apiPost,
  apiPatch,
  apiDelete,
  ADMIN,
  IDENTITY,
  useInvalidateQueries,
  useApiQuery,
} from '@gym360/ui';
import type { AdminUser } from '@gym360/ui';

export type AdminRole = 'super_admin' | 'admin' | 'support' | 'analyst';

export interface AddAdminForm {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  adminRole: AdminRole;
}

export const EMPTY_ADD_ADMIN_FORM: AddAdminForm = { email: '', password: '', firstName: '', lastName: '', adminRole: 'admin' };

export interface PlatformConfig {
  platformName: string;
  defaultLanguage: string;
  defaultCurrency: string;
  supportEmail: string;
}

interface SecurityToggle {
  current: boolean;
  set: (value: boolean) => void;
  field: 'twoFactorRequired' | 'ipWhitelistEnabled';
  label: string;
  errorMessage: string;
}

export function isAdminFormComplete(form: { email: string; firstName: string; lastName: string }): boolean {
  return [form.email, form.firstName, form.lastName].every((value) => value.trim() !== '');
}

function errorMessage(err: unknown, fallback: string): string {
  return err instanceof Error ? err.message : fallback;
}

/** Success + error toasts with independent auto-dismiss timers. */
export function useSettingsToasts() {
  const [toast, setToast] = useState<string | null>(null);
  const [errorToast, setErrorToast] = useState<string | null>(null);

  const flash = (msg: string) => {
    setToast(msg);
    setTimeout(() => setToast(null), 2500);
  };

  const flashError = (msg: string) => {
    setErrorToast(msg);
    setTimeout(() => setErrorToast(null), 4000);
  };

  return { toast, errorToast, flash, flashError };
}

type Toasts = Pick<ReturnType<typeof useSettingsToasts>, 'flash' | 'flashError'>;

/** 2FA / IP-whitelist flags, seeded from the server and toggled optimistically. */
export function useSecuritySettings({ flash, flashError }: Toasts) {
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

  /** Optimistically flips a security flag, PATCHes it, and rolls back on failure. */
  const toggleSecuritySetting = async (toggle: SecurityToggle) => {
    const next = !toggle.current;
    toggle.set(next);
    try {
      await apiPatch(ADMIN.SECURITY_SETTINGS, { [toggle.field]: next });
      flash(`${toggle.label} ${next ? 'enabled' : 'disabled'}`);
    } catch {
      toggle.set(!next);
      flashError(toggle.errorMessage);
    }
  };

  const toggleTwoFactor = () =>
    toggleSecuritySetting({
      current: twoFactorOn,
      set: setTwoFactorOn,
      field: 'twoFactorRequired',
      label: '2FA',
      errorMessage: 'Failed to update 2FA setting',
    });

  const toggleIpWhitelist = () =>
    toggleSecuritySetting({
      current: ipWhitelistOn,
      set: setIpWhitelistOn,
      field: 'ipWhitelistEnabled',
      label: 'IP whitelist',
      errorMessage: 'Failed to update IP whitelist setting',
    });

  return { twoFactorOn, ipWhitelistOn, toggleTwoFactor, toggleIpWhitelist };
}

/** Platform config + admin-team mutations sharing one busy flag. */
export function useSettingsActions({ flash, flashError }: Toasts) {
  const { invalidate } = useInvalidateQueries();
  const [busy, setBusy] = useState(false);
  const [showAddAdmin, setShowAddAdmin] = useState(false);
  const [addAdminForm, setAddAdminForm] = useState<AddAdminForm>(EMPTY_ADD_ADMIN_FORM);
  const [deleteAdmin, setDeleteAdmin] = useState<AdminUser | null>(null);
  const [platformConfig, setPlatformConfig] = useState<PlatformConfig>({
    platformName: 'Gym 360',
    defaultLanguage: 'ar',
    defaultCurrency: 'EGP',
    supportEmail: 'support@gym360.com',
  });

  /** Runs `task` under the busy flag, flashing `success` or the error message. */
  const runWithToast = async (task: () => Promise<void>, success: string, fallback: string) => {
    setBusy(true);
    try {
      await task();
      flash(success);
    } catch (err: unknown) {
      flashError(errorMessage(err, fallback));
    } finally {
      setBusy(false);
    }
  };

  const savePlatformConfig = () =>
    runWithToast(
      () => apiPatch(ADMIN.PLATFORM_CONFIG, platformConfig).then(() => undefined),
      'Platform config saved',
      'Failed to save platform config',
    );

  const removeAdmin = async () => {
    if (!deleteAdmin) return;
    await runWithToast(async () => {
      await apiDelete(IDENTITY.ADMIN_USER(deleteAdmin.id));
      invalidate(['admin-users']);
      setDeleteAdmin(null);
    }, 'Admin user removed', 'Failed to remove admin user');
  };

  const addAdmin = async () => {
    if (!isAdminFormComplete(addAdminForm)) return;
    await runWithToast(async () => {
      await apiPost(IDENTITY.ADMIN_USERS, addAdminForm);
      invalidate(['admin-users']);
      setShowAddAdmin(false);
      setAddAdminForm(EMPTY_ADD_ADMIN_FORM);
    }, 'Admin user added', 'Failed to add admin user');
  };

  return {
    busy,
    platformConfig,
    setPlatformConfig,
    savePlatformConfig,
    showAddAdmin,
    setShowAddAdmin,
    addAdminForm,
    setAddAdminForm,
    addAdmin,
    deleteAdmin,
    setDeleteAdmin,
    removeAdmin,
  };
}

export type SettingsActions = ReturnType<typeof useSettingsActions>;
