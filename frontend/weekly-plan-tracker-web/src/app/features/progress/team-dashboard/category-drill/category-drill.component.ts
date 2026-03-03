import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { PlanningWeekService } from '../../../../core/services/planning-week.service';
import { ProgressService } from '../../../../core/services/progress.service';
import { ToastService } from '../../../../core/services/toast.service';
import { CategoryProgress } from '../../../../core/models/progress.model';

@Component({
  selector: 'app-category-drill',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './category-drill.component.html',
  styleUrls: ['./category-drill.component.scss'],
})
export class CategoryDrillComponent implements OnInit {
  category: CategoryProgress | null = null;
  loading = true;
  categoryName = '';

  constructor(
    private weekService: PlanningWeekService,
    private progressService: ProgressService,
    private route: ActivatedRoute,
    private toast: ToastService,
    public router: Router,
  ) {}

  ngOnInit(): void {
    this.categoryName = this.route.snapshot.paramMap.get('category') ?? '';
    if (!this.categoryName) {
      this.toast.show('Invalid category', 'error');
      this.router.navigate(['/team-progress']);
      return;
    }
    this.weekService.getActive().subscribe({
      next: (week) => {
        if (!week) {
          this.loading = false;
          return;
        }
        this.progressService.getTeamProgress(week.id).subscribe({
          next: (progress) => {
            this.category =
              progress.byCategory.find(
                (c) => c.category === this.categoryName,
              ) ?? null;
            this.loading = false;
          },
          error: () => {
            this.toast.show('Failed to load', 'error');
            this.loading = false;
          },
        });
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  pct(done: number, total: number): number {
    return total > 0 ? Math.round((done / total) * 100) : 0;
  }

  get catClass(): string {
    const map: Record<string, string> = {
      ClientFocused: 'cat-client',
      TechDebt: 'cat-tech',
      RAndD: 'cat-rnd',
    };
    return map[this.categoryName] ?? '';
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

  statusColor(s: string): string {
    const map: Record<string, string> = {
      NotStarted: 'var(--text-muted)',
      InProgress: 'var(--primary)',
      Completed: 'var(--success)',
      Blocked: 'var(--danger)',
    };
    return map[s] ?? 'var(--text-muted)';
  }
}
