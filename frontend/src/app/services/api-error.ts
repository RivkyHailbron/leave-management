import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export function apiErrorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return 'Cannot reach the server. Please try again.';

    const problem = error.error as ProblemDetails | null;
    if (problem?.errors) return Object.values(problem.errors).flat().join(' ');
    if (problem?.detail) return problem.detail;
    if (problem?.title) return problem.title;
  }
  return 'Something went wrong. Please try again.';
}
