import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { PlanningWeekService } from '../../../core/services/planning-week.service';
import { MemberPlanService } from '../../../core/services/member-plan.service';
import { BacklogService } from '../../../core/services/backlog.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { ConfirmModalComponent } from '../../../shared/components/confirm-modal/confirm-modal.component';
import { PlanningWeek } from '../../../core/models/planning-week.model';
import {
  MemberPlan,
  TaskAssignment,
} from '../../../core/models/member-plan.model';
import { BacklogItem } from '../../../core/models/backlog-item.model';

@Component({
  selector: 'app-plan-my-work',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmModalComponent],
  templateUrl: './plan-my-work.component.html',
  styleUrls: ['./plan-my-work.component.scss'],
})
export class PlanMyWorkComponent implements OnInit {
  week: PlanningWeek | null = null;
  plan: MemberPlan | null = null;
  allPlans: MemberPlan[] = [];
  backlog: BacklogItem[] = [];
  loading = true;

  showPicker = false;
  selectedItem: BacklogItem | null = null;
  selectedItemHours = 0;

  editingTask: string | null = null;
  editingHoursValue = 0;
  confirmModal: {
    show: boolean;
    title: string;
    message: string;
    action: () => void;
  } = { show: false, title: '', message: '', action: () => {} };

  constructor(
    private weekService: PlanningWeekService,
    private planService: MemberPlanService,
    private backlogService: BacklogService,
    private auth: AuthService,
    private toast: ToastService,
    public router: Router,
  ) {}

  get memberId(): string {
    return this.auth.currentMember()?.id ?? '';
  }

  get totalPlanned(): number {
    return this.plan?.totalPlannedHours ?? 0;
  }

  get remainingHours(): number {
    return 30 - this.totalPlanned;
  }

  get plannedIds(): Set<string> {
    return new Set(
      this.plan?.taskAssignments.map((t) => t.backlogItemId) ?? [],
    );
  }

  whoPickedItem(backlogItemId: string): string | null {
    const names: string[] = [];
    for (const p of this.allPlans) {
      if (p.memberId === this.memberId) continue;
      const found = p.taskAssignments.find(
        (t) => t.backlogItemId === backlogItemId,
      );
      if (found) names.push(p.memberName);
    }
    return names.length > 0 ? names.join(', ') : null;
  }

  getCategoryBudget(category: string): {
    budget: number;
    claimed: number;
    left: number;
  } {
    if (!this.week) return { budget: 0, claimed: 0, left: 0 };
    const alloc = this.week.categoryAllocations.find(
      (a) => a.category === category,
    );
    const budget = alloc?.budgetHours ?? 0;
    const claimed = this.allPlans.reduce((sum, p) => {
      return (
        sum +
        p.taskAssignments
          .filter((t) => t.category === category)
          .reduce((s, t) => s + t.committedHours, 0)
      );
    }, 0);
    return { budget, claimed, left: budget - claimed };
  }

  get categoryOrder(): string[] {
    return ['ClientFocused', 'TechDebt', 'RAndD'];
  }

  categoryLabel(cat: string): string {
    const map: Record<string, string> = {
      ClientFocused: 'Client Focused',
      TechDebt: 'Tech Debt',
      RAndD: 'R&D',
    };
    return map[cat] ?? cat;
  }

  categoryClass(cat: string): string {
    const map: Record<string, string> = {
      ClientFocused: 'cat-client',
      TechDebt: 'cat-tech',
      RAndD: 'cat-rnd',
    };
    return map[cat] ?? '';
  }

  ngOnInit(): void {
    this.weekService.getActive().subscribe({
      next: (week) => {
        this.week = week;
        if (week) this.loadAllPlans(week);
        else this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  loadAllPlans(week: PlanningWeek): void {
    const calls = week.participatingMemberIds.map((id) =>
      this.planService.get(week.id, id).pipe(catchError(() => of(null))),
    );
    forkJoin(calls).subscribe({
      next: (results) => {
        this.allPlans = results.filter((p) => p !== null) as MemberPlan[];
        this.plan =
          this.allPlans.find((p) => p.memberId === this.memberId) ?? null;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  loadPlan(weekId: string): void {
    if (!this.week) return;
    this.loadAllPlans(this.week);
  }

  openPicker(): void {
    this.showPicker = true;
    this.backlogService.getAll('Available').subscribe({
      next: (items) => {
        this.backlog = items;
      },
    });
  }

  pickItem(item: BacklogItem): void {
    this.selectedItem = item;
    this.selectedItemHours = 0;
  }

  confirmAddItem(): void {
    const item = this.selectedItem;
    if (!item) return;
    const hrs = this.selectedItemHours;
    if (!hrs || hrs <= 0) {
      this.toast.show('Enter valid hours', 'error');
      return;
    }
    if (hrs > this.remainingHours) {
      this.toast.show(`Only ${this.remainingHours}h remaining`, 'error');
      return;
    }
    const catLeft = this.getCategoryBudget(item.category).left;
    if (hrs > catLeft) return; // blocked silently — inline warning shows in HTML

    this.planService
      .claimItem(this.week!.id, this.memberId, {
        backlogItemId: item.id,
        committedHours: hrs,
      })
      .subscribe({
        next: () => {
          this.toast.show(`Added! ${item.title} — ${hrs}h`);
          this.selectedItem = null;
          this.selectedItemHours = 0;
          this.showPicker = false;
          this.loadPlan(this.week!.id);
        },
        error: () => this.toast.show('Failed to add item', 'error'),
      });
  }

  cancelPickDetail(): void {
    this.selectedItem = null;
    this.selectedItemHours = 0;
  }

  removeItem(t: TaskAssignment): void {
    this.confirmModal = {
      show: true,
      title: 'Remove Item',
      message: `Remove "${t.backlogItemTitle}" from your plan?`,
      action: () => {
        this.planService
          .removeItem(this.week!.id, this.memberId, t.id)
          .subscribe({
            next: () => {
              this.toast.show('Item removed');
              this.confirmModal.show = false;
              this.loadPlan(this.week!.id);
            },
            error: () => {
              this.toast.show('Failed', 'error');
              this.confirmModal.show = false;
            },
          });
      },
    };
  }

  startEditHours(t: TaskAssignment): void {
    this.editingTask = t.id;
    this.editingHoursValue = t.committedHours;
  }

  saveHours(t: TaskAssignment): void {
    if (!this.editingHoursValue || this.editingHoursValue <= 0) return;
    this.planService
      .updateHours(this.week!.id, this.memberId, t.id, {
        committedHours: this.editingHoursValue,
      })
      .subscribe({
        next: () => {
          this.toast.show('Hours updated!');
          this.editingTask = null;
          this.loadPlan(this.week!.id);
        },
        error: () => this.toast.show('Failed', 'error'),
      });
  }

  cancelEdit(): void {
    this.editingTask = null;
  }

  toggleReady(): void {
    this.planService.toggleReady(this.week!.id, this.memberId).subscribe({
      next: () => {
        this.toast.show(
          this.plan?.isReady ? 'Marked not ready' : '✅ Marked as ready!',
        );
        this.loadPlan(this.week!.id);
      },
      error: () => this.toast.show('Failed', 'error'),
    });
  }
}
