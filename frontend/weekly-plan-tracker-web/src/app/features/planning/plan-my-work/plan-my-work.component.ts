import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
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
  backlog: BacklogItem[] = [];
  loading = true;
  showPicker = false;
  addingHours: Record<string, number> = {};
  editingHours: Record<string, number> = {};
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

  ngOnInit(): void {
    this.weekService.getActive().subscribe({
      next: (week) => {
        this.week = week;
        if (week) this.loadPlan(week.id);
        else this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  loadPlan(weekId: string): void {
    this.planService.get(weekId, this.memberId).subscribe({
      next: (plan) => {
        this.plan = plan;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  openPicker(): void {
    this.showPicker = true;
    this.backlogService.getAll('Available').subscribe({
      next: (items) => {
        this.backlog = items;
      },
    });
  }

  get plannedIds(): Set<string> {
    return new Set(
      this.plan?.taskAssignments.map((t) => t.backlogItemId) ?? [],
    );
  }

  get remainingHours(): number {
    return 30 - (this.plan?.totalPlannedHours ?? 0);
  }

  categoryClass(cat: string): string {
    const map: Record<string, string> = {
      ClientFocused: 'cat-client',
      TechDebt: 'cat-tech',
      RAndD: 'cat-rnd',
    };
    return map[cat] ?? '';
  }

  statusClass(s: string): string {
    const map: Record<string, string> = {
      NotStarted: 'status-notstarted',
      InProgress: 'status-inprogress',
      Completed: 'status-completed',
      Blocked: 'status-blocked',
    };
    return map[s] ?? '';
  }

  addItem(item: BacklogItem): void {
    const hrs = this.addingHours[item.id];
    if (!hrs || hrs <= 0) {
      this.toast.show('Enter valid hours', 'error');
      return;
    }
    if (hrs > this.remainingHours) {
      this.toast.show(`Only ${this.remainingHours}h remaining`, 'error');
      return;
    }
    this.planService
      .claimItem(this.week!.id, this.memberId, {
        backlogItemId: item.id,
        committedHours: hrs,
      })
      .subscribe({
        next: () => {
          this.toast.show('Item added!');
          this.showPicker = false;
          this.loadPlan(this.week!.id);
        },
        error: () => this.toast.show('Failed to add item', 'error'),
      });
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
    this.editingHours[t.id] = t.committedHours;
  }

  saveHours(t: TaskAssignment): void {
    const hrs = this.editingHours[t.id];
    if (!hrs || hrs <= 0) return;
    this.planService
      .updateHours(this.week!.id, this.memberId, t.id, { committedHours: hrs })
      .subscribe({
        next: () => {
          this.toast.show('Hours updated!');
          delete this.editingHours[t.id];
          this.loadPlan(this.week!.id);
        },
        error: () => this.toast.show('Failed', 'error'),
      });
  }

  cancelEditHours(id: string): void {
    delete this.editingHours[id];
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
