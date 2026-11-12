'use client';

import { useParams } from 'next/navigation';
import { FlagsShell } from '@/components/feature-flags/flags-shell';
import { GymFlagsPanel } from '@/components/feature-flags/gym-flags-panel';
import { useFlagsText } from '@/lib/feature-flags-text';

export default function GymFeatureFlagsPage() {
  const { t } = useFlagsText();
  const { gymId } = useParams<{ gymId: string }>();
  return (
    <FlagsShell title={t.gymsTitle} description={t.gymsDescription}>
      <GymFlagsPanel gymId={gymId} />
    </FlagsShell>
  );
}
