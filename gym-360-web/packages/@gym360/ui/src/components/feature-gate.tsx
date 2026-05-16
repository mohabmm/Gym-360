'use client';

import { type ReactNode } from 'react';
import { usePlanFeatures } from '../hooks/use-plan-features';
import { Lock, ArrowUpCircle } from 'lucide-react';

interface FeatureGateProps {
  featureCode: string;
  children: ReactNode;
  fallback?: ReactNode;
  requiredPlan?: string;
}

export function FeatureGate({ featureCode, children, fallback, requiredPlan }: FeatureGateProps) {
  const { isFeatureEnabled, isLoading } = usePlanFeatures();

  if (isLoading) {
    return <div className="animate-pulse rounded-lg bg-muted h-32" />;
  }

  if (!isFeatureEnabled(featureCode)) {
    return fallback ?? <UpgradePrompt featureName={featureCode} requiredPlan={requiredPlan} />;
  }

  return <>{children}</>;
}

interface UpgradePromptProps {
  featureName: string;
  requiredPlan?: string;
  className?: string;
}

export function UpgradePrompt({ featureName, requiredPlan, className }: UpgradePromptProps) {
  const formattedName = featureName.replace(/_/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase());

  return (
    <div
      className={`flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-muted-foreground/20 bg-muted/30 py-12 px-6 text-center ${className ?? ''}`}
    >
      <div className="flex h-14 w-14 items-center justify-center rounded-full bg-primary/10 mb-4">
        <Lock className="h-6 w-6 text-primary" />
      </div>
      <h3 className="text-lg font-semibold mb-1">{formattedName}</h3>
      <p className="text-sm text-muted-foreground max-w-sm mb-4">
        Not available on your current tier.
        {requiredPlan && ` Available on the ${requiredPlan} tier.`}
      </p>
      <button className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground hover:bg-primary/90 transition-colors">
        <ArrowUpCircle className="h-4 w-4" />
        Upgrade Plan
      </button>
    </div>
  );
}
