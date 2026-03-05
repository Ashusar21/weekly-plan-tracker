import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { PlanningWeekService } from '../../../core/services/planning-week.service';
import { ProgressService } from '../../../core/services/progress.service';
import { ToastService } from '../../../core/services/toast.service';
import { TeamProgress } from '../../../core/models/progress.model';
import { PlanningWeek } from '../../../core/models/planning-week.model';

@Component({
  selector: 'app-team-dashboard',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './team-dashboard.component.html',
  styleUrls: ['./team-dashboard.component.scss'],
})
export class TeamDashboardComponent implements OnInit {
  week: PlanningWeek | null = null;
  progress: TeamProgress | null = null;
  loading = true;
  weekId: string | null = null; // null = active week

  constructor(
    private weekService: PlanningWeekService,
    private progressService: ProgressService,
    private route: ActivatedRoute,
    private toast: ToastService,
    public router: Router,
  ) {}

  ngOnInit(): void {
    this.weekId = this.route.snapshot.queryParamMap.get('weekId');
    const week$ = this.weekId
      ? this.weekService.getById(this.weekId)
      : this.weekService.getActive();

    week$.subscribe({
      next: (week) => {
        this.week = week;
        if (week) {
          this.progressService.getTeamProgress(week.id).subscribe({
            next: (p) => {
              this.progress = p;
              this.loading = false;
            },
            error: () => {
              this.toast.show('Failed to load progress', 'error');
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

  get isPastWeek(): boolean {
    return !!this.weekId;
  }

  pct(done: number, total: number): number {
    return total > 0 ? Math.min(100, Math.round((done / total) * 100)) : 0;
  }

  categoryClass(cat: string): string {
    const map: Record<string, string> = {
      ClientFocused: 'cat-client',
      TechDebt: 'cat-tech',
      RAndD: 'cat-rnd',
    };
    return map[cat] ?? '';
  }

  navigateToCategory(category: string): void {
    const extras = this.weekId ? { queryParams: { weekId: this.weekId } } : {};
    this.router.navigate(['/team-progress/category', category], extras);
  }

  navigateToMember(memberId: string): void {
    const extras = this.weekId ? { queryParams: { weekId: this.weekId } } : {};
    this.router.navigate(['/team-progress/member', memberId], extras);
  }
}
