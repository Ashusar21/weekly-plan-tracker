import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TeamMemberService } from '../../core/services/team-member.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { TeamMember } from '../../core/models/team-member.model';

@Component({
  selector: 'app-team',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './team.component.html',
  styleUrls: ['./team.component.scss'],
})
export class TeamComponent implements OnInit {
  members: TeamMember[] = [];
  loading = true;
  newName = '';
  adding = false;
  editingId: string | null = null;
  editName = '';

  constructor(
    private teamService: TeamMemberService,
    private auth: AuthService,
    private toast: ToastService,
    public router: Router,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.teamService.getAll().subscribe({
      next: (members) => {
        this.members = members;
        this.loading = false;
      },
      error: () => {
        this.toast.show('Failed to load members', 'error');
        this.loading = false;
      },
    });
  }

  addMember(): void {
    if (!this.newName.trim()) return;
    this.adding = true;
    this.teamService.create({ name: this.newName.trim() }).subscribe({
      next: () => {
        this.toast.show(`${this.newName} added!`);
        this.newName = '';
        this.adding = false;
        this.load();
      },
      error: () => {
        this.toast.show('Failed to add member', 'error');
        this.adding = false;
      },
    });
  }

  startEdit(member: TeamMember): void {
    this.editingId = member.id;
    this.editName = member.name;
  }

  saveEdit(member: TeamMember): void {
    if (!this.editName.trim()) return;
    this.teamService
      .update(member.id, { name: this.editName.trim() })
      .subscribe({
        next: () => {
          this.toast.show('Name updated!');
          this.editingId = null;
          this.load();
        },
        error: () => this.toast.show('Failed to update', 'error'),
      });
  }

  cancelEdit(): void {
    this.editingId = null;
    this.editName = '';
  }

  makeLead(member: TeamMember): void {
    if (!confirm(`Make ${member.name} the team lead?`)) return;
    this.teamService.makeLead(member.id).subscribe({
      next: () => {
        this.toast.show(`${member.name} is now lead!`);
        this.load();
      },
      error: () => this.toast.show('Failed to update lead', 'error'),
    });
  }

  deactivate(member: TeamMember): void {
    if (!confirm(`Deactivate ${member.name}?`)) return;
    this.teamService.deactivate(member.id).subscribe({
      next: () => {
        this.toast.show(`${member.name} deactivated`);
        this.load();
      },
      error: () => this.toast.show('Failed to deactivate', 'error'),
    });
  }

  reactivate(member: TeamMember): void {
    this.teamService.reactivate(member.id).subscribe({
      next: () => {
        this.toast.show(`${member.name} reactivated!`);
        this.load();
      },
      error: () => this.toast.show('Failed to reactivate', 'error'),
    });
  }

  isCurrentUser(member: TeamMember): boolean {
    return this.auth.currentMember()?.id === member.id;
  }

  get isLead(): boolean {
    return this.auth.isLead();
  }
}
