import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { PlanningWeekService } from '../../../core/services/planning-week.service';
import { MemberPlanService } from '../../../core/services/member-plan.service';
import { TeamMemberService } from '../../../core/services/team-member.service';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmModalComponent } from '../../../shared/components/confirm-modal/confirm-modal.component';
import { PlanningWeek } from '../../../core/models/planning-week.model';
import { MemberPlan } from '../../../core/models/member-plan.model';
import { TeamMember } from '../../../core/models/team-member.model';

@Component({
  selector: 'app-review-freeze',
  standalone: true,
  imports: [CommonModule, ConfirmModalComponent],
  templateUrl: './review-freeze.component.html',
  styleUrls: ['./review-freeze.component.scss'],
})
export class ReviewFreezeComponent implements OnInit {
  week: PlanningWeek | null = null;
  plans: MemberPlan[] = [];
  members: TeamMember[] = [];
  loading = true;
  showFreezeModal = false;

  constructor(
    private weekService: PlanningWeekService,
    private planService: MemberPlanService,
    private teamService: TeamMemberService,
    private toast: ToastService,
    public router: Router,
  ) {}

  ngOnInit(): void {
    this.teamService.getAll().subscribe({
      next: (m) => {
        this.members = m;
      },
    });

    this.weekService.getActive().subscribe({
      next: (week) => {
        this.week = week;

        if (week && week.participatingMemberIds.length > 0) {
          const calls = week.participatingMemberIds.map((id) =>
            this.planService.get(week.id, id).pipe(catchError(() => of(null))),
          );

          forkJoin(calls).subscribe({
            next: (results) => {
              this.plans = results.filter((p) => p !== null) as MemberPlan[];
              this.loading = false;
            },
          });
        } else {
          this.loading = false;
        }
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  // ✅ All members ready?
  get allReady(): boolean {
    return this.plans.length > 0 && this.plans.every((p) => p.isReady);
  }

  // ✅ Total participating members
  get totalCount(): number {
    return this.week?.participatingMemberIds.length ?? 0;
  }

  // ✅ Number of ready members
  get readyCount(): number {
    return this.plans.filter((p) => p.isReady).length;
  }

  memberName(id: string): string {
    return this.members.find((m) => m.id === id)?.name ?? id;
  }

  categoryClass(cat: string): string {
    const map: Record<string, string> = {
      ClientFocused: 'cat-client',
      TechDebt: 'cat-tech',
      RAndD: 'cat-rnd',
    };
    return map[cat] ?? '';
  }

  freeze(): void {
    if (!this.week) return;

    this.weekService.freeze(this.week.id).subscribe({
      next: () => {
        this.toast.show('🧊 Plan frozen! Work begins now.');
        this.showFreezeModal = false;
        this.router.navigate(['/hub']);
      },
      error: () => {
        this.toast.show('Failed to freeze', 'error');
        this.showFreezeModal = false;
      },
    });
  }
}
