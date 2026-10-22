import { Component, DestroyRef, OnInit, computed, inject, output, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import {
  CreateLeaveRequest,
  Employee,
  LEAVE_TYPE_LABELS,
  LeaveRequest,
  LeaveType
} from '../models/leave-request.model';
import { EmployeesService } from '../services/employees.service';
import { LeaveRequestsService } from '../services/leave-requests.service';
import { apiErrorMessage } from '../services/api-error';
import { dateRangeValidator, inclusiveDays } from './date-range.validator';

@Component({
  selector: 'app-leave-request-form',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './leave-request-form.component.html',
  styleUrls: ['./leave-request-form.component.css']
})
export class LeaveRequestFormComponent implements OnInit {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly employeesService = inject(EmployeesService);
  private readonly leaveRequests = inject(LeaveRequestsService);
  private readonly destroyRef = inject(DestroyRef);

  readonly created = output<LeaveRequest>();

  readonly form = this.fb.group(
    {
      employeeId: this.fb.control<number | null>(null, Validators.required),
      type: this.fb.control<LeaveType | null>(null, Validators.required),
      startDate: this.fb.control('', Validators.required),
      endDate: this.fb.control('', Validators.required)
    },
    { validators: dateRangeValidator('startDate', 'endDate') }
  );

  readonly employees = signal<Employee[]>([]);
  readonly submitting = signal(false);
  readonly serverError = signal<string | null>(null);

  readonly leaveTypes = Object.entries(LEAVE_TYPE_LABELS).map(([value, label]) => ({
    value: Number(value) as LeaveType,
    label
  }));

  private readonly formValue = toSignal(this.form.valueChanges, { initialValue: this.form.getRawValue() });

  readonly days = computed(() => {
    const { startDate, endDate } = this.formValue();
    if (!startDate || !endDate || startDate > endDate) return null;
    return inclusiveDays(startDate, endDate);
  });

  ngOnInit(): void {
    this.employeesService
      .getAll()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (employees) => this.employees.set(employees),
        error: (error) => this.serverError.set(apiErrorMessage(error))
      });
  }

  showError(controlName: keyof typeof this.form.controls): boolean {
    const control = this.form.controls[controlName];
    return control.invalid && (control.touched || control.dirty);
  }

  get showDateRangeError(): boolean {
    const end = this.form.controls.endDate;
    return this.form.hasError('dateRange') && (end.touched || end.dirty);
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.serverError.set(null);

    this.leaveRequests
      .create(this.form.getRawValue() as CreateLeaveRequest)
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (request) => {
          this.form.reset();
          this.created.emit(request);
        },
        error: (error) => this.serverError.set(apiErrorMessage(error))
      });
  }
}
