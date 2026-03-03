import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { PlanningWeek, CreatePlanningWeekDto, UpdateAllocationsDto } from '../models/planning-week.model';

@Injectable({ providedIn: 'root' })
export class PlanningWeekService {
  constructor(private api: ApiService) {}

  getAll(): Observable<PlanningWeek[]> {
    return this.api.get<PlanningWeek[]>('/planning-weeks');
  }

  getActive(): Observable<PlanningWeek | null> {
    return this.api.get<PlanningWeek | null>('/planning-weeks/active');
  }

  getById(id: string): Observable<PlanningWeek> {
    return this.api.get<PlanningWeek>(`/planning-weeks/${id}`);
  }

  create(dto: CreatePlanningWeekDto): Observable<PlanningWeek> {
    return this.api.post<PlanningWeek>('/planning-weeks', dto);
  }

  updateAllocations(id: string, dto: UpdateAllocationsDto): Observable<PlanningWeek> {
    return this.api.put<PlanningWeek>(`/planning-weeks/${id}/allocations`, dto);
  }

  open(id: string): Observable<void> {
    return this.api.patch<void>(`/planning-weeks/${id}/open`);
  }

  freeze(id: string): Observable<void> {
    return this.api.patch<void>(`/planning-weeks/${id}/freeze`);
  }

  finish(id: string): Observable<void> {
    return this.api.patch<void>(`/planning-weeks/${id}/finish`);
  }

  cancel(id: string): Observable<void> {
    return this.api.delete<void>(`/planning-weeks/${id}`);
  }
}
