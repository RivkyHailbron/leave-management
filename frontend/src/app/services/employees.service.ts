import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Employee } from '../models/leave-request.model';

@Injectable({ providedIn: 'root' })
export class EmployeesService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<Employee[]> {
    return this.http.get<Employee[]>('/api/employees');
  }
}
