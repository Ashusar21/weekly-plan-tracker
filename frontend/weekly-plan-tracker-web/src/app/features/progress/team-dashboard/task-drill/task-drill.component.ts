import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ProgressService } from '../../../../core/services/progress.service';
import { PlanningWeekService } from '../../../../core/services/planning-week.service';
import { ToastService } from '../../../../core/services/toast.service';
import { ProgressUpdateDto } from '../../../../core/models/progress.model';
import { TaskAssignment } from '../../../../core/models/member-plan.model';

@Component({
  selector: 'app-task-drill',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './task-drill.component.html',
  styleUrls: ['./task-drill.component.scss'],
})
export class TaskDrillComponent implements OnInit {
  task: TaskAssignment | null = null;
  allAssignments: TaskAssignment[] = [];
  history: ProgressUpdateDto[] = [];
  loading = true;
  assignmentId = '';

  constructor(
    private progressService: ProgressService,
    private weekService: PlanningWeekService,
    private toast: ToastService,
    private route: ActivatedRoute,
    public router: Router,
  ) {}

  ngOnInit(): void {
    this.assignmentId = this.route.snapshot.paramMap.get('assignmentId') ?? '';
    if (!this.assignmentId) {
      this.router.navigate(['/team-progress']);
      return;
    }

    this.progressService.getTaskHistory(this.assignmentId).subscribe({
      next: (h) => { this.history = h; this.loading = false; },
      error: () => { this.toast.show('Failed to load history', 'error'); this.loading = false; }
    });

    this.weekService.getActive().subscribe({
      next: (week) => {
        if (!week) return;
        this.progressService.getTeamProgress(week.id).subscribe({
          next: (progress) => {
            for (const cat of progress.byCategory) {
              const found = cat.tasks.find(t => t.id === this.assignmentId);
              if (found) {
                this.task = found;
                this.allAssignments = cat.tasks.filter(t => t.backlogItemId === found.backlogItemId);
                break;
              }
            }
          }
        });
      }
    });
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

  categoryClass(cat: string): string {
    const map: Record<string, string> = {
      ClientFocused: 'cat-client',
      TechDebt: 'cat-tech',
      RAndD: 'cat-rnd',
    };
    return map[cat] ?? '';
  }
}
