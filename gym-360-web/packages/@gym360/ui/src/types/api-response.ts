/**
 * Backend error entry per docs/01_GYMAPP_BACKEND_COMPLETE.md §"Error Response for Gated Features"
 * and docs/PRD_TRAINEE_ME_ENDPOINTS.md.
 *
 * Examples:
 *   { code: "VALIDATION_ERROR", message: "Email is required", meta: { field: "email" } }
 *   { code: "PLAN.FEATURE_NOT_AVAILABLE", message: "...", meta: { feature, currentPlan, requiredPlan, upgradeUrl } }
 *   { code: "GYM_SUSPENDED", message: "This gym is currently suspended." }
 */
export interface ApiError {
  code: string;
  message: string;
  meta?: Record<string, unknown>;
}

export interface ApiResponse<T> {
  data: T;
  errors: ApiError[];
  meta?: PaginationMeta;
}

export interface PaginationMeta {
  totalCount: number;
  pageSize: number;
  currentPage: number;
  totalPages: number;
}

export interface PagedRequest {
  page: number;
  pageSize: number;
  search?: string;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
}
