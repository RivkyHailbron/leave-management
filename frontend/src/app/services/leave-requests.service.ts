import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateLeaveRequest, LeaveRequest } from '../models/leave-request.model';

@Injectable({ providedIn: 'root' })
export class LeaveRequestsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/leave-requests';

  getAll(): Observable<LeaveRequest[]> {
    return this.http.get<LeaveRequest[]>(this.baseUrl);
  }

  create(request: CreateLeaveRequest): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(this.baseUrl, request);
  }

  approve(id: number): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(`${this.baseUrl}/${id}/approve`, null);
  }
}
