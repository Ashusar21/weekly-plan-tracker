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
  showCancelModal = false;

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

  get allReady(): boolean {
    return this.plans.length > 0 && this.plans.every((p) => p.isReady);
  }

  get totalCount(): number {
    return this.week?.participatingMemberIds.length ?? 0;
  }

  get readyCount(): number {
    return this.plans.filter((p) => p.isReady).length;
  }

  // Category allocations sorted: highest budget first
  get sortedAllocations() {
    return [...(this.week?.categoryAllocations ?? [])].sort(
      (a, b) => b.budgetHours - a.budgetHours,
    );
  }

  // Members sorted: lead first, then rest in original order
  get sortedMemberIds(): string[] {
    if (!this.week) return [];
    return [...this.week.participatingMemberIds].sort((a, b) => {
      const aLead = this.members.find((m) => m.id === a)?.isLead ? 1 : 0;
      const bLead = this.members.find((m) => m.id === b)?.isLead ? 1 : 0;
      return bLead - aLead;
    });
  }

  getPlanHours(memberId: string): number {
    return (
      this.plans.find((p) => p.memberId === memberId)?.totalPlannedHours ?? 0
    );
  }

  getPlanReady(memberId: string): boolean {
    return this.plans.find((p) => p.memberId === memberId)?.isReady ?? false;
  }

  categoryPlanned(category: string): number {
    return this.plans.reduce((sum, plan) => {
      return (
        sum +
        plan.taskAssignments
          .filter((t) => t.category === category)
          .reduce((s, t) => s + t.committedHours, 0)
      );
    }, 0);
  }

  get blockingReasons(): string[] {
    const reasons: string[] = [];
    if (!this.week) return reasons;

    for (const memberId of this.sortedMemberIds) {
      const hours = this.getPlanHours(memberId);
      const diff = 30 - hours;
      if (diff > 0) {
        reasons.push(
          `${this.memberName(memberId)} has ${hours} hours (needs ${diff} more).`,
        );
      }
    }

    for (const alloc of this.sortedAllocations) {
      const planned = this.categoryPlanned(alloc.category);
      if (planned < alloc.budgetHours) {
        reasons.push(
          `${alloc.categoryLabel} has ${planned}h planned but budget is ${alloc.budgetHours}h.`,
        );
      }
    }

    return reasons;
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
        this.toast.show('Plan frozen! Work begins now.');
        this.showFreezeModal = false;
        this.router.navigate(['/hub']);
      },
      error: () => {
        this.toast.show('Failed to freeze', 'error');
        this.showFreezeModal = false;
      },
    });
  }

  cancelPlanning(): void {
    if (!this.week) return;
    this.weekService.cancel(this.week.id).subscribe({
      next: () => {
        this.toast.show('Planning has been canceled.');
        this.showCancelModal = false;
        this.router.navigate(['/hub']);
      },
      error: () => {
        this.toast.show('Failed to cancel', 'error');
        this.showCancelModal = false;
      },
    });
  }
}
