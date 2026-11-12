'use client';

import { FlagsShell } from '@/components/feature-flags/flags-shell';
import { PresetEditor } from '@/components/feature-flags/preset-editor';
import { useFlagsText } from '@/lib/feature-flags-text';

export default function FeatureFlagPresetsPage() {
  const { t } = useFlagsText();
  return (
    <FlagsShell title={t.presetsTitle} description={t.presetsDescription}>
      <PresetEditor />
    </FlagsShell>
  );
}
