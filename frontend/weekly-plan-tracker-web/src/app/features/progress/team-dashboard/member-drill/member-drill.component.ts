import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { PlanningWeekService } from '../../../../core/services/planning-week.service';
import { ProgressService } from '../../../../core/services/progress.service';
import { ToastService } from '../../../../core/services/toast.service';
import { MemberProgress } from '../../../../core/models/progress.model';

@Component({
  selector: 'app-member-drill',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './member-drill.component.html',
  styleUrls: ['./member-drill.component.scss'],
})
export class MemberDrillComponent implements OnInit {
  memberProgress: MemberProgress | null = null;
  loading = true;

  constructor(
    private weekService: PlanningWeekService,
    private progressService: ProgressService,
    private route: ActivatedRoute,
    private toast: ToastService,
    public router: Router,
  ) {}

  ngOnInit(): void {
    const memberId = this.route.snapshot.paramMap.get('memberId')!;
    this.weekService.getActive().subscribe({
      next: (week) => {
        if (!week) {
          this.loading = false;
          return;
        }
        this.progressService.getMemberProgress(week.id, memberId).subscribe({
          next: (p) => {
            this.memberProgress = p;
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

  statusClass(s: string): string {
    const map: Record<string, string> = {
      NotStarted: 'status-notstarted',
      InProgress: 'status-inprogress',
      Completed: 'status-completed',
      Blocked: 'status-blocked',
    };
    return map[s] ?? '';
  }
}
