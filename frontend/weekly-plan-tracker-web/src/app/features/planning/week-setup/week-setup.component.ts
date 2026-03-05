import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PlanningWeekService } from '../../../core/services/planning-week.service';
import { TeamMemberService } from '../../../core/services/team-member.service';
import { ToastService } from '../../../core/services/toast.service';
import { PlanningWeek } from '../../../core/models/planning-week.model';
import { TeamMember } from '../../../core/models/team-member.model';
import { ConfirmModalComponent } from '../../../shared/components/confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-week-setup',
  standalone: true,
  imports: [CommonModule, FormsModule, ConfirmModalComponent],
  templateUrl: './week-setup.component.html',
  styleUrls: ['./week-setup.component.scss'],
})
export class WeekSetupComponent implements OnInit {
  activeWeek: PlanningWeek | null = null;
  members: TeamMember[] = [];
  loading = true;
  saving = false;

  planningDate = '';
  selectedMemberIds: string[] = [];
  clientPercent = 40;
  techPercent = 30;
  rndPercent = 30;

  showConfirm = false;
  confirmTitle = '';
  confirmMessage = '';
  confirmLabel = 'Yes';
  confirmDanger = true;
  private pendingAction: (() => void) | null = null;

  constructor(
    private weekService: PlanningWeekService,
    private teamService: TeamMemberService,
    private toast: ToastService,
    public router: Router,
  ) {}

  ngOnInit(): void {
    this.teamService.getAll().subscribe({
      next: (m) => {
        this.members = m.filter((x) => x.isActive);
        if (!this.activeWeek) {
          this.selectedMemberIds = this.members.map((m) => m.id);
        }
      },
    });
    this.weekService.getActive().subscribe({
      next: (week) => {
        this.activeWeek = week;
        if (week) {
          this.planningDate = week.planningDate.split('T')[0];
          this.selectedMemberIds = [...week.participatingMemberIds];
          const cf = week.categoryAllocations.find(
            (a) => a.category === 'ClientFocused',
          );
          const td = week.categoryAllocations.find(
            (a) => a.category === 'TechDebt',
          );
          const rnd = week.categoryAllocations.find(
            (a) => a.category === 'RAndD',
          );
          this.clientPercent = cf?.percentage ?? 40;
          this.techPercent = td?.percentage ?? 30;
          this.rndPercent = rnd?.percentage ?? 30;
        }
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  get totalPercent(): number {
    return this.clientPercent + this.techPercent + this.rndPercent;
  }

  get percentValid(): boolean {
    return this.totalPercent === 100;
  }

  get isTuesday(): boolean {
    if (!this.planningDate) return false;
    const [y, m, d] = this.planningDate.split('-').map(Number);
    return new Date(y, m - 1, d).getDay() === 2;
  }

  get isFutureOrToday(): boolean {
    if (!this.planningDate) return false;
    const [y, m, d] = this.planningDate.split('-').map(Number);
    const selected = new Date(y, m - 1, d);
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return selected >= today;
  }

  get dateError(): string {
    if (!this.planningDate) return '';
    if (!this.isFutureOrToday)
      return `${this.planningDate} is in the past. Please pick a future Tuesday.`;
    if (!this.isTuesday)
      return `${this.planningDate} is not a Tuesday. Please pick a Tuesday.`;
    return '';
  }

  get workPeriod(): string {
    if (!this.planningDate || !this.isTuesday || !this.isFutureOrToday)
      return '';
    const [y, m, d] = this.planningDate.split('-').map(Number);
    const start = new Date(y, m - 1, d + 1);
    const end = new Date(y, m - 1, d + 6);
    const fmt = (dt: Date) =>
      `${dt.getFullYear()}-${String(dt.getMonth() + 1).padStart(2, '0')}-${String(dt.getDate()).padStart(2, '0')}`;
    return `Work period: ${fmt(start)} to ${fmt(end)}`;
  }

  get budgetHours(): { client: number; tech: number; rnd: number } {
    const cap = this.selectedMemberIds.length * 30;
    return {
      client: Math.round((cap * this.clientPercent) / 100),
      tech: Math.round((cap * this.techPercent) / 100),
      rnd: Math.round((cap * this.rndPercent) / 100),
    };
  }

  getTuesdayDates(): string[] {
    const dates: string[] = [];
    const now = new Date();
    now.setHours(0, 0, 0, 0);
    for (let i = 0; i <= 28; i++) {
      const d = new Date(now);
      d.setDate(now.getDate() + i);
      if (d.getDay() === 2) {
        const yyyy = d.getFullYear();
        const mm = String(d.getMonth() + 1).padStart(2, '0');
        const dd = String(d.getDate()).padStart(2, '0');
        dates.push(`${yyyy}-${mm}-${dd}`);
        if (dates.length === 4) break;
      }
    }
    return dates;
  }

  toggleMember(id: string): void {
    const idx = this.selectedMemberIds.indexOf(id);
    if (idx >= 0) this.selectedMemberIds.splice(idx, 1);
    else this.selectedMemberIds.push(id);
  }

  isMemberSelected(id: string): boolean {
    return this.selectedMemberIds.includes(id);
  }

  openPlanningNew(): void {
    if (!this.planningDate) {
      this.toast.show('Select a planning date', 'error');
      return;
    }
    if (!this.isTuesday) {
      this.toast.show(
        `${this.planningDate} is not a Tuesday. Please pick a Tuesday.`,
        'error',
      );
      return;
    }
    if (!this.isFutureOrToday) {
      this.toast.show('Please pick a future date.', 'error');
      return;
    }
    if (this.selectedMemberIds.length === 0) {
      this.toast.show('Select at least one member', 'error');
      return;
    }
    if (!this.percentValid) {
      this.toast.show('Percentages must total 100%', 'error');
      return;
    }

    this.saving = true;
    this.weekService
      .create({
        planningDate: this.planningDate,
        participatingMemberIds: this.selectedMemberIds,
        clientFocusedPercent: this.clientPercent,
        techDebtPercent: this.techPercent,
        rAndDPercent: this.rndPercent,
      })
      .subscribe({
        next: (week) => {
          this.weekService.open(week.id).subscribe({
            next: () => {
              this.toast.show(
                'Planning is open! Team members can now plan their work.',
              );
              this.router.navigate(['/hub']);
            },
            error: () => {
              this.toast.show(
                'Week created but failed to open planning',
                'error',
              );
              this.saving = false;
            },
          });
        },
        error: () => {
          this.toast.show('Failed to create week', 'error');
          this.saving = false;
        },
      });
  }

  saveAndOpenPlanning(): void {
    if (!this.activeWeek) return;
    if (!this.percentValid) {
      this.toast.show('Percentages must total 100%', 'error');
      return;
    }
    if (this.selectedMemberIds.length === 0) {
      this.toast.show('Select at least one member', 'error');
      return;
    }
    this.saving = true;
    this.weekService
      .updateAllocations(this.activeWeek.id, {
        clientFocusedPercent: this.clientPercent,
        techDebtPercent: this.techPercent,
        rAndDPercent: this.rndPercent,
        participatingMemberIds: this.selectedMemberIds,
      })
      .subscribe({
        next: () => {
          this.weekService.open(this.activeWeek!.id).subscribe({
            next: () => {
              this.toast.show(
                'Planning is open! Team members can now plan their work.',
              );
              this.router.navigate(['/hub']);
            },
            error: () => {
              this.toast.show('Saved but failed to open planning', 'error');
              this.saving = false;
            },
          });
        },
        error: () => {
          this.toast.show('Failed to update', 'error');
          this.saving = false;
        },
      });
  }

  cancelWeek(): void {
    if (!this.activeWeek) return;
    this.confirmTitle = 'Cancel This Week?';
    this.confirmMessage = 'This will erase all plans. This cannot be undone.';
    this.confirmLabel = 'Yes, Cancel Planning';
    this.confirmDanger = true;
    this.pendingAction = () => {
      this.weekService.cancel(this.activeWeek!.id).subscribe({
        next: () => {
          this.toast.show('Planning has been canceled.');
          this.router.navigate(['/hub']);
        },
        error: () => this.toast.show('Failed to cancel week', 'error'),
      });
    };
    this.showConfirm = true;
  }

  onConfirmed(): void {
    this.showConfirm = false;
    this.pendingAction?.();
    this.pendingAction = null;
  }

  onCancelled(): void {
    this.showConfirm = false;
    this.pendingAction = null;
  }
}
