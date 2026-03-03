import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  TeamMember,
  CreateTeamMemberDto,
  UpdateTeamMemberDto,
} from '../models/team-member.model';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class TeamMemberService {
  constructor(private api: ApiService) {}
  getAll(): Observable<TeamMember[]> {
    return this.api.get<TeamMember[]>('/team-members');
  }
  getById(id: string): Observable<TeamMember> {
    return this.api.get<TeamMember>(`/team-members/${id}`);
  }
  create(dto: CreateTeamMemberDto): Observable<TeamMember> {
    return this.api.post<TeamMember>('/team-members', dto);
  }
  update(id: string, dto: UpdateTeamMemberDto): Observable<TeamMember> {
    return this.api.put<TeamMember>(`/team-members/${id}`, dto);
  }
  makeLead(id: string): Observable<void> {
    return this.api.patch<void>(`/team-members/${id}/make-lead`);
  }
  deactivate(id: string): Observable<void> {
    return this.api.patch<void>(`/team-members/${id}/deactivate`);
  }
  reactivate(id: string): Observable<void> {
    return this.api.patch<void>(`/team-members/${id}/reactivate`);
  }
  anyExists(): Observable<{ exists: boolean }> {
    return this.api.get<{ exists: boolean }>('/team-members/any');
  }
}
