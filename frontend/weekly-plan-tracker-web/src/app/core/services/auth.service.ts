import { Injectable, signal, computed } from '@angular/core';
import { TeamMember } from '../models/team-member.model';

const STORAGE_KEY = 'wpt_current_member';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private _member = signal<TeamMember | null>(this.loadFromStorage());

  currentMember = computed(() => this._member());
  isLead = computed(() => this._member()?.isLead ?? false);

  private loadFromStorage(): TeamMember | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  }

  login(member: TeamMember): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(member));
    this._member.set(member);
  }

  logout(): void {
    localStorage.removeItem(STORAGE_KEY);
    this._member.set(null);
  }

  refresh(member: TeamMember): void {
    this.login(member);
  }
}
