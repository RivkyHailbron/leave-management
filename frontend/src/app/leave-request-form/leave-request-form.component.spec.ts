import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { LeaveRequest, LeaveStatus, LeaveType } from '../models/leave-request.model';
import { LeaveRequestFormComponent } from './leave-request-form.component';

describe('LeaveRequestFormComponent', () => {
  let fixture: ComponentFixture<LeaveRequestFormComponent>;
  let component: LeaveRequestFormComponent;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [LeaveRequestFormComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    fixture = TestBed.createComponent(LeaveRequestFormComponent);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);

    fixture.detectChanges();
    http.expectOne('/api/employees').flush([{ id: 1, name: 'Dana Levi', annualQuota: 20 }]);
  });

  afterEach(() => http.verify());

  const fill = (overrides: Partial<{ employeeId: number | null; type: LeaveType | null; startDate: string; endDate: string }> = {}) =>
    component.form.setValue({
      employeeId: 1,
      type: LeaveType.Vacation,
      startDate: '2026-08-02',
      endDate: '2026-08-04',
      ...overrides
    });

  it('requires an employee and a leave type', () => {
    fill({ employeeId: null, type: null });

    expect(component.form.controls.employeeId.hasError('required')).toBeTrue();
    expect(component.form.controls.type.hasError('required')).toBeTrue();
  });

  it('rejects an end date before the start date and shows no day count', () => {
    fill({ startDate: '2026-08-06', endDate: '2026-08-02' });

    expect(component.form.hasError('dateRange')).toBeTrue();
    expect(component.days()).toBeNull();
  });

  it('does not call the API when the form is invalid', () => {
    fill({ type: null });

    component.submit();

    http.expectNone('/api/leave-requests');
    expect(component.form.controls.type.touched).toBeTrue();
  });

  it('submits a valid form and emits the created request', () => {
    const created: LeaveRequest = {
      id: 9, employeeId: 1, employeeName: 'Dana Levi', type: LeaveType.Vacation,
      startDate: '2026-08-02', endDate: '2026-08-04', days: 3, status: LeaveStatus.Pending
    };
    let emitted: LeaveRequest | undefined;
    component.created.subscribe((r) => (emitted = r));
    fill();

    expect(component.days()).toBe(3);
    component.submit();
    const req = http.expectOne('/api/leave-requests');
    expect(req.request.body).toEqual({ employeeId: 1, type: LeaveType.Vacation, startDate: '2026-08-02', endDate: '2026-08-04' });
    req.flush(created);

    expect(emitted).toEqual(created);
  });

  it('shows the server error when the balance is exceeded', () => {
    fill();

    component.submit();
    http
      .expectOne('/api/leave-requests')
      .flush({ status: 422, detail: 'Not enough vacation balance.' }, { status: 422, statusText: 'Unprocessable Entity' });

    expect(component.serverError()).toBe('Not enough vacation balance.');
    expect(component.submitting()).toBeFalse();
  });
});
