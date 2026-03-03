import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PlanningWeekService } from '../../../core/services/planning-week.service';
import { TeamMemberService } from '../../../core/services/team-member.service';
import { ToastService } from '../../../core/services/toast.service';
import { PlanningWeek } from '../../../core/models/planning-week.model';
import { TeamMember } from '../../../core/models/team-member.model';

@Component({
  selector: 'app-week-setup',
  standalone: true,
  imports: [CommonModule, FormsModule],
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

  toggleMember(id: string): void {
    const idx = this.selectedMemberIds.indexOf(id);
    if (idx >= 0) this.selectedMemberIds.splice(idx, 1);
    else this.selectedMemberIds.push(id);
  }

  isMemberSelected(id: string): boolean {
    return this.selectedMemberIds.includes(id);
  }

  getTuesdayDates(): string[] {
    const dates: string[] = [];
    const now = new Date();
    for (let i = 0; i <= 28; i++) {
      const d = new Date(now);
      d.setDate(now.getDate() + i);
      if (d.getDay() === 2) {
        dates.push(d.toISOString().split('T')[0]);
        if (dates.length === 4) break;
      }
    }
    return dates;
  }

  create(): void {
    if (!this.planningDate) {
      this.toast.show('Select a planning date', 'error');
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
        next: () => {
          this.toast.show('Week created!');
          this.router.navigate(['/hub']);
        },
        error: () => {
          this.toast.show('Failed to create week', 'error');
          this.saving = false;
        },
      });
  }

  updateAllocations(): void {
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
          this.toast.show('Allocations updated!');
          this.saving = false;
        },
        error: () => {
          this.toast.show('Failed to update', 'error');
          this.saving = false;
        },
      });
  }

  openPlanning(): void {
    if (!this.activeWeek) return;
    this.weekService.open(this.activeWeek.id).subscribe({
      next: () => {
        this.toast.show('Planning opened! Members can now plan their work.');
        this.router.navigate(['/hub']);
      },
      error: () => this.toast.show('Failed to open planning', 'error'),
    });
  }

  cancelWeek(): void {
    if (
      !this.activeWeek ||
      !confirm('Cancel this week? This cannot be undone.')
    )
      return;
    this.weekService.cancel(this.activeWeek.id).subscribe({
      next: () => {
        this.toast.show('Week cancelled');
        this.router.navigate(['/hub']);
      },
      error: () => this.toast.show('Failed to cancel week', 'error'),
    });
  }
}
