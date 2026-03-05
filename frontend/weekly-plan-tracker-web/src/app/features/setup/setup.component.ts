import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TeamMemberService } from '../../core/services/team-member.service';
import { ToastService } from '../../core/services/toast.service';

interface SetupMember {
  name: string;
  isLead: boolean;
}

@Component({
  selector: 'app-setup',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './setup.component.html',
  styleUrls: ['./setup.component.scss'],
})
export class SetupComponent {
  memberName = '';
  members: SetupMember[] = [];
  error = '';
  loading = false;

  constructor(
    private teamService: TeamMemberService,
    private toast: ToastService,
    private router: Router,
  ) {}

  addMember(): void {
    this.error = '';
    const name = this.memberName.trim();
    if (!name) { this.error = 'Please type a name.'; return; }
    if (name.length > 100) { this.error = 'Name is too long.'; return; }
    if (this.members.some(m => m.name.toLowerCase() === name.toLowerCase())) {
      this.error = 'This name is already added.'; return;
    }
    // First member becomes lead automatically
    this.members.push({ name, isLead: this.members.length === 0 });
    this.memberName = '';
  }

  makeLead(index: number): void {
    this.members.forEach((m, i) => m.isLead = i === index);
  }

  removeMember(index: number): void {
    const wasLead = this.members[index].isLead;
    this.members.splice(index, 1);
    // If removed person was lead, make first person lead
    if (wasLead && this.members.length > 0) {
      this.members[0].isLead = true;
    }
  }

  get canFinish(): boolean {
    return this.members.length > 0 && this.members.some(m => m.isLead);
  }

  finish(): void {
    if (!this.canFinish) return;
    this.loading = true;

    // Create members sequentially, first member becomes lead via backend
    const createNext = (index: number): void => {
      if (index >= this.members.length) {
        // Now set the correct lead if it's not the first person
        const leadIndex = this.members.findIndex(m => m.isLead);
        this.toast.show('Team created! Welcome aboard.', 'success');
        this.router.navigate(['/identity']);
        return;
      }
      this.teamService.create({ name: this.members[index].name }).subscribe({
        next: (created) => {
          // If this member is the lead and it's not the first (first is auto-lead)
          if (this.members[index].isLead && index > 0) {
            this.teamService.makeLead(created.id).subscribe({
              next: () => createNext(index + 1),
              error: () => createNext(index + 1),
            });
          } else {
            createNext(index + 1);
          }
        },
        error: () => {
          this.toast.show('Failed to create team. Is the API running?', 'error');
          this.loading = false;
        },
      });
    };

    createNext(0);
  }
}
