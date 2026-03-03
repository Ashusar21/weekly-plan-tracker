import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { PlanningWeekService } from '../../core/services/planning-week.service';
import { ToastService } from '../../core/services/toast.service';
import { PlanningWeek } from '../../core/models/planning-week.model';

@Component({
  selector: 'app-past-weeks',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './past-weeks.component.html',
  styleUrls: ['./past-weeks.component.scss'],
})
export class PastWeeksComponent implements OnInit {
  weeks: PlanningWeek[] = [];
  loading = true;

  constructor(
    private weekService: PlanningWeekService,
    private toast: ToastService,
    public router: Router,
  ) {}

  ngOnInit(): void {
    this.weekService.getAll().subscribe({
      next: (weeks) => {
        this.weeks = weeks
          .filter((w) => w.state === 'Completed' || w.state === 'Frozen')
          .sort((a, b) => b.planningDate.localeCompare(a.planningDate));
        this.loading = false;
      },
      error: () => {
        this.toast.show('Failed to load', 'error');
        this.loading = false;
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
}
