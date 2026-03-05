import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TeamMemberService } from '../../core/services/team-member.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { TeamMember } from '../../core/models/team-member.model';
import { ConfirmModalComponent } from '../../shared/components/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-team',
  standalone: true,
  imports: [FormsModule, ConfirmModalComponent],
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
  teamError = '';

  // Confirm modal state
  showConfirm = false;
  confirmTitle = '';
  confirmMessage = '';
  confirmLabel = '';
  confirmDanger = false;
  pendingAction: (() => void) | null = null;

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
      next: (members) => { this.members = members; this.loading = false; },
      error: () => { this.toast.show('Failed to load members', 'error'); this.loading = false; },
    });
  }

  addMember(): void {
    this.teamError = '';
    const name = this.newName.trim();
    if (!name) return;
    if (name.length > 100) { this.teamError = 'Name is too long.'; return; }
    if (this.members.some(m => m.name.toLowerCase() === name.toLowerCase())) {
      this.teamError = 'This name is already used.'; return;
    }
    this.adding = true;
    this.teamService.create({ name }).subscribe({
      next: () => {
        this.toast.show('Team member added!');
        this.newName = '';
        this.adding = false;
        this.load();
      },
      error: () => { this.toast.show('Failed to add member', 'error'); this.adding = false; },
    });
  }

  startEdit(member: TeamMember): void {
    this.editingId = member.id;
    this.editName = member.name;
    this.teamError = '';
  }

  saveEdit(member: TeamMember): void {
    this.teamError = '';
    const name = this.editName.trim();
    if (!name) return;
    if (this.members.some(m => m.id !== member.id && m.name.toLowerCase() === name.toLowerCase())) {
      this.teamError = 'This name is already used.'; return;
    }
    this.teamService.update(member.id, { name }).subscribe({
      next: () => { this.toast.show('Name updated!'); this.editingId = null; this.load(); },
      error: () => this.toast.show('Failed to update', 'error'),
    });
  }

  cancelEdit(): void { this.editingId = null; this.editName = ''; }

  makeLead(member: TeamMember): void {
    this.confirmTitle = 'Change Team Lead?';
    this.confirmMessage = `Make ${member.name} the new Team Lead?`;
    this.confirmLabel = 'Yes, Make Lead';
    this.confirmDanger = false;
    this.pendingAction = () => {
      this.teamService.makeLead(member.id).subscribe({
        next: () => { this.toast.show('Team Lead changed!'); this.load(); },
        error: () => this.toast.show('Failed to update lead', 'error'),
      });
    };
    this.showConfirm = true;
  }

  deactivate(member: TeamMember): void {
    this.confirmTitle = `Remove ${member.name}?`;
    this.confirmMessage = `They won't be available for future plans. Their past work will still be saved.`;
    this.confirmLabel = 'Yes, Remove Them';
    this.confirmDanger = true;
    this.pendingAction = () => {
      this.teamService.deactivate(member.id).subscribe({
        next: () => { this.toast.show('Member deactivated.'); this.load(); },
        error: () => this.toast.show('Failed to deactivate', 'error'),
      });
    };
    this.showConfirm = true;
  }

  reactivate(member: TeamMember): void {
    this.teamService.reactivate(member.id).subscribe({
      next: () => { this.toast.show('Member reactivated!'); this.load(); },
      error: () => this.toast.show('Failed to reactivate', 'error'),
    });
  }

  onConfirmed(): void {
    this.pendingAction?.();
    this.showConfirm = false;
    this.pendingAction = null;
  }

  onCancelled(): void {
    this.showConfirm = false;
    this.pendingAction = null;
  }

  isCurrentUser(member: TeamMember): boolean {
    return this.auth.currentMember()?.id === member.id;
  }

  get isLead(): boolean { return this.auth.isLead(); }
}
