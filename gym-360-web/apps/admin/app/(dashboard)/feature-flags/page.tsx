'use client';

import { FlagsShell } from '@/components/feature-flags/flags-shell';
import { GymsTable } from '@/components/feature-flags/gyms-table';
import { useFlagsText } from '@/lib/feature-flags-text';

export default function FeatureFlagsGymsPage() {
  const { t } = useFlagsText();
  return (
    <FlagsShell title={t.gymsTitle} description={t.gymsDescription}>
      <GymsTable />
    </FlagsShell>
  );
}
