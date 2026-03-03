import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PlanningWeekService } from '../../../core/services/planning-week.service';
import { MemberPlanService } from '../../../core/services/member-plan.service';
import { ProgressService } from '../../../core/services/progress.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { PlanningWeek } from '../../../core/models/planning-week.model';
import {
  MemberPlan,
  TaskAssignment,
  ProgressStatus,
} from '../../../core/models/member-plan.model';

@Component({
  selector: 'app-update-progress',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './update-progress.component.html',
  styleUrls: ['./update-progress.component.scss'],
})
export class UpdateProgressComponent implements OnInit {
  week: PlanningWeek | null = null;
  plan: MemberPlan | null = null;
  loading = true;
  saving = false;

  // Modal state
  showModal = false;
  activeTask: TaskAssignment | null = null;
  modalForm: { hours: number; status: ProgressStatus; note: string } = {
    hours: 0,
    status: 'NotStarted',
    note: '',
  };
  modalError = '';

  statuses: { value: ProgressStatus; label: string }[] = [
    { value: 'InProgress', label: 'In Progress' },
    { value: 'Completed', label: 'Completed' },
    { value: 'Blocked', label: 'Blocked' },
  ];

  constructor(
    private weekService: PlanningWeekService,
    private planService: MemberPlanService,
    private progressService: ProgressService,
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
        if (week) {
          this.planService.get(week.id, this.memberId).subscribe({
            next: (plan) => {
              this.plan = plan;
              this.loading = false;
            },
            error: () => {
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

  get sortedTasks(): TaskAssignment[] {
    if (!this.plan) return [];
    const order: Record<string, number> = {
      Blocked: 0,
      InProgress: 1,
      NotStarted: 2,
      Completed: 3,
    };
    return [...this.plan.taskAssignments].sort(
      (a, b) => (order[a.progressStatus] ?? 9) - (order[b.progressStatus] ?? 9),
    );
  }

  get totalCompleted(): number {
    return (
      this.plan?.taskAssignments.reduce((s, t) => s + t.hoursCompleted, 0) ?? 0
    );
  }

  get totalPct(): number {
    return Math.min(100, Math.round((this.totalCompleted / 30) * 100));
  }

  openModal(t: TaskAssignment): void {
    this.activeTask = t;
    this.modalForm = {
      hours: t.hoursCompleted,
      status: t.progressStatus,
      note: '',
    };
    this.modalError = '';
    this.showModal = true;
  }

  allowedStatuses(): { value: ProgressStatus; label: string }[] {
    const s = this.activeTask?.progressStatus;
    const map: Record<string, ProgressStatus[]> = {
      NotStarted: ['NotStarted', 'InProgress', 'Blocked'],
      InProgress: ['InProgress', 'Completed', 'Blocked'],
      Blocked: ['Blocked', 'InProgress'],
      Completed: ['Completed', 'InProgress'],
    };
    const allowed = map[s ?? ''] ?? [];
    return this.statuses
      .filter((x) => allowed.includes(x.value))
      .concat(
        s === 'NotStarted'
          ? [{ value: 'NotStarted', label: 'Not Started' }]
          : [],
      )
      .filter((v, i, a) => a.findIndex((x) => x.value === v.value) === i);
  }

  saveProgress(): void {
    this.modalError = '';
    const f = this.modalForm;
    if (f.hours < 0) {
      this.modalError = "Hours can't be less than zero.";
      return;
    }
    this.saving = true;
    this.progressService
      .submitUpdate(this.activeTask!.id, {
        hoursCompleted: f.hours,
        status: f.status,
        note: f.note,
        updatedBy: this.memberId,
      })
      .subscribe({
        next: () => {
          this.toast.show('Progress saved!');
          this.showModal = false;
          this.saving = false;
          this.loadPlan();
        },
        error: () => {
          this.toast.show('Failed to save', 'error');
          this.saving = false;
        },
      });
  }

  loadPlan(): void {
    this.planService.get(this.week!.id, this.memberId).subscribe({
      next: (plan) => {
        this.plan = plan;
      },
    });
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

  pct(done: number, total: number): number {
    return total > 0 ? Math.min(100, Math.round((done / total) * 100)) : 0;
  }
}
