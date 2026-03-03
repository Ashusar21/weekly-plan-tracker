import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { MemberPlan, TaskAssignment, ClaimBacklogItemDto, UpdateCommittedHoursDto } from '../models/member-plan.model';

@Injectable({ providedIn: 'root' })
export class MemberPlanService {
  constructor(private api: ApiService) {}

  get(weekId: string, memberId: string): Observable<MemberPlan> {
    return this.api.get<MemberPlan>(`/member-plans/${weekId}/${memberId}`);
  }

  claimItem(weekId: string, memberId: string, dto: ClaimBacklogItemDto): Observable<TaskAssignment> {
    return this.api.post<TaskAssignment>(`/member-plans/${weekId}/${memberId}/assignments`, dto);
  }

  updateHours(weekId: string, memberId: string, assignmentId: string, dto: UpdateCommittedHoursDto): Observable<TaskAssignment> {
    return this.api.put<TaskAssignment>(`/member-plans/${weekId}/${memberId}/assignments/${assignmentId}`, dto);
  }

  removeItem(weekId: string, memberId: string, assignmentId: string): Observable<void> {
    return this.api.delete<void>(`/member-plans/${weekId}/${memberId}/assignments/${assignmentId}`);
  }

  toggleReady(weekId: string, memberId: string): Observable<void> {
    return this.api.patch<void>(`/member-plans/${weekId}/${memberId}/ready`);
  }
}
