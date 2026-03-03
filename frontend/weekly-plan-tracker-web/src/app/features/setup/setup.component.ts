import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TeamMemberService } from '../../core/services/team-member.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-setup',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './setup.component.html',
  styleUrls: ['./setup.component.scss'],
})
export class SetupComponent {
  memberName = '';
  loading = false;

  constructor(
    private teamService: TeamMemberService,
    private toast: ToastService,
    private router: Router,
  ) {}

  create(): void {
    if (!this.memberName.trim()) return;
    this.loading = true;
    this.teamService.create({ name: this.memberName.trim() }).subscribe({
      next: () => {
        this.toast.show('Team created! Welcome aboard.', 'success');
        this.router.navigate(['/identity']);
      },
      error: () => {
        this.toast.show('Failed to create team. Is the API running?', 'error');
        this.loading = false;
      },
    });
  }
}
