'use client';

import { useRef, useState } from 'react';
import { useSession } from 'next-auth/react';
import { PageHeader, StatusBadge, FeatureGate, FEATURE_CODES } from '@gym360/ui';
import { Download, FileUp, FileSpreadsheet, AlertCircle, Loader2 } from 'lucide-react';
import {
  useDataImportJobs,
  useStartDataImport,
  downloadImportTemplate,
  downloadImportErrors,
  type DataImportSource,
  type DataImportJob,
} from '@/lib/hooks/useDataImport';

export default function DataImportPage() {
  return (
    <div className="space-y-6">
      <PageHeader
        title="Data Import"
        description="Import members, payments, packages and InBody records from Odoo, Daftra, or Excel."
      />
      <FeatureGate featureCode={FEATURE_CODES.DATA_IMPORT} requiredPlan="Pro">
        <DataImportContent />
      </FeatureGate>
    </div>
  );
}

function DataImportContent() {
  const { data: session } = useSession();
  const gymId = session?.user?.gymId;
  const { data: jobs, isLoading } = useDataImportJobs(gymId);

  return (
    <div className="space-y-6">
      <TemplateStep />
      <UploadStep gymId={gymId} />
      <JobsSection jobs={jobs} isLoading={isLoading} gymId={gymId!} />
    </div>
  );
}

function TemplateStep() {
  return (
    <section className="rounded-xl border bg-card p-6">
      <h2 className="text-lg font-semibold mb-1">Step 1 — Download the template</h2>
      <p className="text-sm text-muted-foreground mb-4">
        The workbook has four tabs: <b>Members</b>, <b>Payments</b>, <b>Sessions</b>, <b>InBody</b>.
        Every tab uses the gym&apos;s member number as the shared key.
      </p>
      <button
        type="button"
        onClick={() => downloadImportTemplate()}
        className="inline-flex items-center gap-2 rounded-lg border px-4 py-2 text-sm font-medium hover:bg-muted"
      >
        <Download className="h-4 w-4" />
        Download Excel template
      </button>
    </section>
  );
}

function UploadStep({ gymId }: { gymId: string | undefined }) {
  const startImport = useStartDataImport(gymId);
  const [source, setSource] = useState<DataImportSource>('Excel');
  const [file, setFile] = useState<File | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  async function handleUpload() {
    if (!file) return;
    await startImport.mutateAsync({ file, source });
    setFile(null);
    if (fileInputRef.current) fileInputRef.current.value = '';
  }

  return (
    <section className="rounded-xl border bg-card p-6">
      <h2 className="text-lg font-semibold mb-1">Step 2 — Upload</h2>
      <p className="text-sm text-muted-foreground mb-4">
        Export your data from Odoo or Daftra to Excel (or fill in the template) and upload a single
        <code className="mx-1 rounded bg-muted px-1">.xlsx</code> file (max 10&nbsp;MB).
      </p>
      <div className="flex flex-wrap items-end gap-3">
        <label className="flex flex-col text-sm">
          <span className="mb-1 text-muted-foreground">Source</span>
          <select
            value={source}
            onChange={(e) => setSource(e.target.value as DataImportSource)}
            className="rounded-lg border bg-background px-3 py-2"
          >
            <option value="Excel">Excel (template)</option>
            <option value="Odoo">Odoo export</option>
            <option value="Daftra">Daftra export</option>
          </select>
        </label>
        <label className="flex flex-col text-sm flex-1 min-w-[240px]">
          <span className="mb-1 text-muted-foreground">File</span>
          <input
            ref={fileInputRef}
            type="file"
            accept=".xlsx"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
            className="rounded-lg border bg-background px-3 py-2"
          />
        </label>
        <button
          type="button"
          onClick={handleUpload}
          disabled={!file || startImport.isPending}
          className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground disabled:opacity-50"
        >
          {startImport.isPending ? <Loader2 className="h-4 w-4 animate-spin" /> : <FileUp className="h-4 w-4" />}
          Upload & start import
        </button>
      </div>
      {startImport.isError && (
        <p className="mt-3 flex items-center gap-2 text-sm text-destructive">
          <AlertCircle className="h-4 w-4" />
          {(startImport.error as Error)?.message ?? 'Upload failed.'}
        </p>
      )}
    </section>
  );
}

interface JobsProps {
  jobs: DataImportJob[] | undefined;
  isLoading: boolean;
  gymId: string;
}

function JobList({ jobs, isLoading, gymId }: JobsProps) {
  if (isLoading) return <p className="px-6 py-8 text-sm text-muted-foreground">Loading…</p>;
  if (!jobs || jobs.length === 0) {
    return <p className="px-6 py-8 text-sm text-muted-foreground">No imports yet. Upload your first workbook above.</p>;
  }
  return (
    <>
      {jobs.map((job) => (
        <JobRow key={job.id} job={job} gymId={gymId} />
      ))}
    </>
  );
}

function JobsSection(props: JobsProps) {
  return (
    <section className="rounded-xl border bg-card">
      <div className="flex items-center justify-between border-b px-6 py-4">
        <h2 className="text-lg font-semibold">Recent import jobs</h2>
        <FileSpreadsheet className="h-5 w-5 text-muted-foreground" />
      </div>
      <div className="divide-y">
        <JobList {...props} />
      </div>
    </section>
  );
}

function JobRow({ job, gymId }: { job: DataImportJob; gymId: string }) {
  const badge = statusBadge(job.status);
  return (
    <div className="flex flex-wrap items-center gap-4 px-6 py-4">
      <div className="flex-1 min-w-[220px]">
        <p className="font-medium">{job.originalFileName}</p>
        <p className="text-xs text-muted-foreground">
          {job.source} · {(job.fileSizeBytes / 1024).toFixed(0)} KB · uploaded{' '}
          {new Date(job.createdAt).toLocaleString()}
        </p>
      </div>
      <div className="flex items-center gap-3 text-sm">
        <StatusBadge variant={badge.variant} label={badge.label} />
        <span className="text-muted-foreground">
          {job.successCount} OK · {job.failureCount} failed
        </span>
      </div>
      {job.hasErrorReport && (
        <button
          type="button"
          onClick={() => downloadImportErrors(gymId, job.id)}
          className="inline-flex items-center gap-1 rounded-lg border px-3 py-1.5 text-xs font-medium hover:bg-muted"
        >
          <Download className="h-3.5 w-3.5" />
          errors.csv
        </button>
      )}
    </div>
  );
}

function statusBadge(status: DataImportJob['status']): { variant: 'active' | 'warning' | 'suspended' | 'default' | 'pro'; label: string } {
  switch (status) {
    case 'Completed':
      return { variant: 'active', label: 'Completed' };
    case 'CompletedWithErrors':
      return { variant: 'warning', label: 'Completed with errors' };
    case 'Failed':
      return { variant: 'suspended', label: 'Failed' };
    case 'Processing':
      return { variant: 'pro', label: 'Processing' };
    default:
      return { variant: 'default', label: 'Pending' };
  }
}
