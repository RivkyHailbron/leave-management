import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { finalize } from 'rxjs';
import {
  LEAVE_STATUS_LABELS,
  LEAVE_TYPE_LABELS,
  LeaveRequest,
  LeaveStatus
} from '../models/leave-request.model';
import { LeaveRequestsService } from '../services/leave-requests.service';
import { apiErrorMessage } from '../services/api-error';
import { LeaveRequestFormComponent } from '../leave-request-form/leave-request-form.component';
import { Feedback, FeedbackKind } from '../models/feedback.model';

@Component({
  selector: 'app-leave-requests',
  standalone: true,
  imports: [DatePipe, LeaveRequestFormComponent],
  templateUrl: './leave-requests.component.html',
  styleUrls: ['./leave-requests.component.css']
})
export class LeaveRequestsComponent implements OnInit {
  private readonly leaveRequests = inject(LeaveRequestsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly requests = signal<LeaveRequest[]>([]);
  readonly loading = signal(false);
  readonly loadError = signal<string | null>(null);
  readonly feedback = signal<Feedback | null>(null);
  private readonly approvingIds = signal<ReadonlySet<number>>(new Set());

  readonly LeaveStatus = LeaveStatus;
  readonly FeedbackKind = FeedbackKind;
  readonly typeLabels = LEAVE_TYPE_LABELS;
  readonly statusLabels = LEAVE_STATUS_LABELS;

  ngOnInit(): void {
    this.load();
  }

  load(showSpinner = true): void {
    this.loading.set(showSpinner);
    this.loadError.set(null);

    this.leaveRequests
      .getAll()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (requests) => this.requests.set(requests),
        error: (error) => this.loadError.set(apiErrorMessage(error))
      });
  }

  isApproving(id: number): boolean {
    return this.approvingIds().has(id);
  }

  approve(request: LeaveRequest): void {
    this.setApproving(request.id, true);
    this.feedback.set(null);

    this.leaveRequests
      .approve(request.id)
      .pipe(
        finalize(() => this.setApproving(request.id, false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (approved) => {
          this.replaceRequest(approved);
          this.feedback.set({
            kind: FeedbackKind.Success,
            message: `Request #${approved.id} for ${approved.employeeName} was approved.`
          });
        },
        error: (error: unknown) => {
          this.feedback.set({ kind: FeedbackKind.Error, message: apiErrorMessage(error) });
          // 409: someone else already processed it, so our copy of the list is stale.
          if (error instanceof HttpErrorResponse && error.status === HttpStatusCode.Conflict) {
            this.load(false);
          }
        }
      });
  }

  onCreated(request: LeaveRequest): void {
    this.requests.update((requests) => [request, ...requests]);
    this.feedback.set({
      kind: FeedbackKind.Success,
      message: `Request #${request.id} for ${request.employeeName} was submitted.`
    });
  }

  dismissFeedback(): void {
    this.feedback.set(null);
  }

  private replaceRequest(updated: LeaveRequest): void {
    this.requests.update((requests) => requests.map((r) => (r.id === updated.id ? updated : r)));
  }

  private setApproving(id: number, approving: boolean): void {
    this.approvingIds.update((ids) => {
      const next = new Set(ids);
      if (approving) next.add(id);
      else next.delete(id);
      return next;
    });
  }
}
