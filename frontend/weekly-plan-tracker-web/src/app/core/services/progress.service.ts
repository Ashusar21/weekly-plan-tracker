import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import { TeamProgress, MemberProgress, ProgressUpdateDto, SubmitProgressUpdateDto } from '../models/progress.model';
import { TaskAssignment } from '../models/member-plan.model';

@Injectable({ providedIn: 'root' })
export class ProgressService {
  constructor(private api: ApiService) {}

  getTeamProgress(weekId: string): Observable<TeamProgress> {
    return this.api.get<TeamProgress>(`/progress/${weekId}`);
  }

  getMemberProgress(weekId: string, memberId: string): Observable<MemberProgress> {
    return this.api.get<MemberProgress>(`/progress/${weekId}/${memberId}`);
  }

  getTaskHistory(assignmentId: string): Observable<ProgressUpdateDto[]> {
    return this.api.get<ProgressUpdateDto[]>(`/progress/assignments/${assignmentId}/history`);
  }

  submitUpdate(assignmentId: string, dto: SubmitProgressUpdateDto): Observable<TaskAssignment> {
    return this.api.post<TaskAssignment>(`/progress/assignments/${assignmentId}`, dto);
  }
}
