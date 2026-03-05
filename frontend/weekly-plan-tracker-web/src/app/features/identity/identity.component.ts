import { Component, OnInit } from '@angular/core';

import { Router } from '@angular/router';
import { TeamMemberService } from '../../core/services/team-member.service';
import { AuthService } from '../../core/services/auth.service';
import { TeamMember } from '../../core/models/team-member.model';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-identity',
  standalone: true,
  imports: [],
  templateUrl: './identity.component.html',
  styleUrls: ['./identity.component.scss'],
})
export class IdentityComponent implements OnInit {
  members: TeamMember[] = [];
  loading = true;

  constructor(
    private teamService: TeamMemberService,
    private auth: AuthService,
    private toast: ToastService,
    private router: Router,
  ) {}

  ngOnInit(): void {
    this.teamService.getAll().subscribe({
      next: (members) => {
        this.members = members.filter((m) => m.isActive);
        this.loading = false;
      },
      error: () => {
        this.toast.show('Failed to load team members', 'error');
        this.loading = false;
      },
    });
  }

  select(member: TeamMember): void {
    this.auth.login(member);
    this.toast.show(`Welcome back, ${member.name}!`, 'success');
    this.router.navigate(['/hub']);
  }
}
