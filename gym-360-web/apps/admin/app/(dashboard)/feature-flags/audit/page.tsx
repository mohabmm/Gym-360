'use client';

import { FlagsShell } from '@/components/feature-flags/flags-shell';
import { AuditTable } from '@/components/feature-flags/audit-table';
import { useFlagsText } from '@/lib/feature-flags-text';

export default function FeatureFlagAuditPage() {
  const { t } = useFlagsText();
  return (
    <FlagsShell title={t.auditTitle} description={t.auditDescription}>
      <AuditTable />
    </FlagsShell>
  );
}
