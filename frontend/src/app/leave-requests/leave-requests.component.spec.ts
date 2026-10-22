import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { LeaveRequest, LeaveStatus, LeaveType } from '../models/leave-request.model';
import { FeedbackKind } from '../models/feedback.model';
import { LeaveRequestsComponent } from './leave-requests.component';

describe('LeaveRequestsComponent', () => {
  let fixture: ComponentFixture<LeaveRequestsComponent>;
  let component: LeaveRequestsComponent;
  let http: HttpTestingController;

  const pending: LeaveRequest = {
    id: 7,
    employeeId: 1,
    employeeName: 'Dana Levi',
    type: LeaveType.Vacation,
    startDate: '2026-08-02',
    endDate: '2026-08-03',
    days: 2,
    status: LeaveStatus.Pending
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [LeaveRequestsComponent],
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    fixture = TestBed.createComponent(LeaveRequestsComponent);
    component = fixture.componentInstance;
    http = TestBed.inject(HttpTestingController);

    fixture.detectChanges();
    http.expectOne('/api/employees').flush([]);
    http.expectOne('/api/leave-requests').flush([pending]);
  });

  afterEach(() => http.verify());

  it('marks only the clicked row as approving while the request is in flight', () => {
    component.approve(pending);

    expect(component.isApproving(pending.id)).toBeTrue();
    http.expectOne(`/api/leave-requests/${pending.id}/approve`).flush({ ...pending, status: LeaveStatus.Approved });
    expect(component.isApproving(pending.id)).toBeFalse();
  });

  it('updates the approved row from the response without reloading the list', () => {
    component.approve(pending);
    http.expectOne(`/api/leave-requests/${pending.id}/approve`).flush({ ...pending, status: LeaveStatus.Approved });

    expect(component.requests()[0].status).toBe(LeaveStatus.Approved);
    expect(component.feedback()?.kind).toBe(FeedbackKind.Success);
    http.expectNone('/api/leave-requests');
  });

  it('shows the server message and refreshes the list when the request was already processed', () => {
    component.approve(pending);
    http
      .expectOne(`/api/leave-requests/${pending.id}/approve`)
      .flush({ status: 409, detail: 'Leave request 7 is already approved.' }, { status: 409, statusText: 'Conflict' });

    expect(component.feedback()).toEqual({ kind: FeedbackKind.Error, message: 'Leave request 7 is already approved.' });
    http.expectOne('/api/leave-requests').flush([{ ...pending, status: LeaveStatus.Approved }]);
    expect(component.requests()[0].status).toBe(LeaveStatus.Approved);
  });

  it('shows the server message without reloading when approval exceeds the quota', () => {
    component.approve(pending);
    http
      .expectOne(`/api/leave-requests/${pending.id}/approve`)
      .flush({ status: 422, detail: 'Approving would exceed the vacation quota.' }, { status: 422, statusText: 'Unprocessable Entity' });

    expect(component.feedback()?.message).toBe('Approving would exceed the vacation quota.');
    expect(component.requests()[0].status).toBe(LeaveStatus.Pending);
    http.expectNone('/api/leave-requests');
  });
});
