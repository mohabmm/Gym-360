'use client';

import { useState } from 'react';
import { useRouter } from 'next/navigation';
import { apiPost, apiPut, apiDelete, GYMS as GYMS_ENDPOINTS, BILLING, useInvalidateQueries } from '@gym360/ui';
import { useSuspendGym, useDeleteGym } from '@/lib/hooks';
import { errorMessage } from '@/lib/gym-detail';

export interface EditGymForm {
  name: string;
  ownerName: string;
}

export interface ChangePlanInput {
  planId: string;
  reason: string;
}

export interface ToggleOverrideInput {
  featureId: string;
  featureCode: string;
  existingOverrideId: string | undefined;
  planDefault: boolean;
}

/** Shared busy/error state for the gym-detail header and modal actions. */
function useBusyAction() {
  const [busyAction, setBusyAction] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  /** Runs `task`, surfacing any thrown error as `actionError` (falls back to `fallback`). */
  const runGuarded = async (task: () => Promise<void>, fallback: string) => {
    setBusyAction(true);
    setActionError(null);
    try {
      await task();
    } catch (err: unknown) {
      setActionError(errorMessage(err, fallback));
    } finally {
      setBusyAction(false);
    }
  };

  /** Runs `task` with the busy flag only — errors propagate to the caller. */
  const runBusy = async (task: () => Promise<void>) => {
    setBusyAction(true);
    try {
      await task();
    } finally {
      setBusyAction(false);
    }
  };

  return { busyAction, actionError, setActionError, runGuarded, runBusy };
}

export function useGymDetailActions(gymId: string) {
  const router = useRouter();
  const { invalidate } = useInvalidateQueries();
  const suspendGym = useSuspendGym();
  const deleteGym = useDeleteGym();
  const busy = useBusyAction();

  const invalidateGym = () => {
    invalidate(['gyms', gymId]);
    invalidate(['gyms']);
  };

  const saveEdit = (form: EditGymForm, onDone: () => void) =>
    busy.runGuarded(async () => {
      await apiPut(GYMS_ENDPOINTS.UPDATE(gymId), form);
      invalidateGym();
      onDone();
    }, 'Failed to save gym details');

  const reactivate = (onDone: () => void) =>
    busy.runGuarded(async () => {
      await apiPost(GYMS_ENDPOINTS.REACTIVATE(gymId), {});
      invalidateGym();
      onDone();
    }, 'Failed to reactivate gym');

  const changePlan = ({ planId, reason }: ChangePlanInput, onDone: () => void) =>
    busy.runGuarded(async () => {
      await apiPut(BILLING.CHANGE_PLAN(gymId), { planId, reason });
      invalidate(['subscriptions']);
      invalidate(['gyms', gymId]);
      invalidate(['plan-change-log']);
      onDone();
    }, 'Failed to change plan');

  const remove = (onDone: () => void) =>
    busy.runGuarded(async () => {
      await deleteGym.mutateAsync({ gymId });
      invalidate(['gyms']);
      onDone();
      router.push('/gyms');
    }, 'Failed to delete gym');

  const suspend = (reason: string, onDone: () => void) =>
    busy.runBusy(async () => {
      await suspendGym.mutateAsync({ gymId, reason });
      invalidate(['gyms', gymId]);
      onDone();
    });

  return {
    busyAction: busy.busyAction,
    actionError: busy.actionError,
    clearActionError: () => busy.setActionError(null),
    saveEdit,
    reactivate,
    changePlan,
    remove,
    suspend,
  };
}

export type GymDetailActions = ReturnType<typeof useGymDetailActions>;

/** Per-gym feature override toggle (add when absent, remove when present). */
export function useFeatureOverrideToggle(gymId: string) {
  const { invalidate } = useInvalidateQueries();
  const [busyOverrideCode, setBusyOverrideCode] = useState<string | null>(null);

  const toggleOverride = async ({ featureId, featureCode, existingOverrideId, planDefault }: ToggleOverrideInput) => {
    setBusyOverrideCode(featureCode);
    try {
      if (existingOverrideId) {
        await apiDelete(BILLING.GYM_OVERRIDE(gymId, featureId));
      } else {
        // Backend expects { featureId, enabled: bool, reason? }. SetBy is derived from JWT.
        // If plan default is ON we force OFF, and vice versa.
        await apiPost(BILLING.GYM_OVERRIDES(gymId), {
          featureId,
          enabled: !planDefault,
          reason: null,
        });
      }
      invalidate(['overrides', gymId]);
    } finally {
      setBusyOverrideCode(null);
    }
  };

  return { busyOverrideCode, toggleOverride };
}
