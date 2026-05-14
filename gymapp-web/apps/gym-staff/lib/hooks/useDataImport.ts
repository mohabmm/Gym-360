import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { getSession } from 'next-auth/react';
import apiClient from '@/lib/api-client';

export type DataImportSource = 'Excel' | 'Odoo' | 'Daftra';

export type DataImportJob = {
  id: string;
  gymId: string;
  uploadedByUserId: string;
  source: DataImportSource;
  status: 'Pending' | 'Processing' | 'Completed' | 'CompletedWithErrors' | 'Failed';
  originalFileName: string;
  fileSizeBytes: number;
  totalRowCount: number;
  successCount: number;
  failureCount: number;
  hasErrorReport: boolean;
  failureReason: string | null;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
};

function gymScopedPath(gymId: string, suffix = '') {
  return `/gyms/${gymId}/imports${suffix}`;
}

export function useDataImportJobs(gymId: string | undefined) {
  return useQuery<DataImportJob[]>({
    queryKey: ['data-imports', gymId],
    enabled: !!gymId,
    refetchInterval: (query) => {
      const data = query.state.data;
      return data?.some((j) => j.status === 'Pending' || j.status === 'Processing') ? 3000 : false;
    },
    queryFn: async () => {
      const res = await apiClient.get(gymScopedPath(gymId!));
      return res.data.data ?? [];
    },
  });
}

export function useDataImportJob(gymId: string | undefined, jobId: string | undefined) {
  return useQuery<DataImportJob>({
    queryKey: ['data-imports', gymId, jobId],
    enabled: !!gymId && !!jobId,
    refetchInterval: (query) => {
      const status = query.state.data?.status;
      return status === 'Pending' || status === 'Processing' ? 2000 : false;
    },
    queryFn: async () => {
      const res = await apiClient.get(gymScopedPath(gymId!, `/${jobId}`));
      return res.data.data;
    },
  });
}

export function useStartDataImport(gymId: string | undefined) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (input: { file: File; source: DataImportSource }) => {
      if (!gymId) throw new Error('gymId is required');
      const form = new FormData();
      form.append('file', input.file);
      form.append('source', input.source);
      const res = await apiClient.post(gymScopedPath(gymId), form, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      return res.data.data as string;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['data-imports', gymId] }),
  });
}

export async function downloadImportTemplate() {
  const res = await apiClient.get('/gyms/imports/template.xlsx', { responseType: 'blob' });
  triggerDownload(res.data, 'gymapp-import-template.xlsx');
}

export async function downloadImportErrors(gymId: string, jobId: string) {
  const res = await apiClient.get(`/gyms/${gymId}/imports/${jobId}/errors.csv`, { responseType: 'blob' });
  triggerDownload(res.data, `import-${jobId}-errors.csv`);
}

function triggerDownload(blob: Blob, filename: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}
