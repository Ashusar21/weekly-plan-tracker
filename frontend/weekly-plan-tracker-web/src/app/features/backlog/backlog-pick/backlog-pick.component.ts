import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { BacklogService } from '../../../core/services/backlog.service';
import { MemberPlanService } from '../../../core/services/member-plan.service';
import { PlanningWeekService } from '../../../core/services/planning-week.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { BacklogItem } from '../../../core/models/backlog-item.model';

@Component({
  selector: 'app-backlog-pick',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './backlog-pick.component.html',
  styleUrls: ['./backlog-pick.component.scss'],
})
export class BacklogPickComponent implements OnInit {
  items: BacklogItem[] = [];
  loading = true;
  hoursMap: Record<string, number> = {};
  weekId = '';
  memberId = '';

  constructor(
    private backlog: BacklogService,
    private planService: MemberPlanService,
    private weekService: PlanningWeekService,
    private auth: AuthService,
    private toast: ToastService,
    public router: Router,
  ) {}

  ngOnInit(): void {
    this.memberId = this.auth.currentMember()?.id ?? '';
    this.weekService.getActive().subscribe({
      next: (week) => {
        if (week) this.weekId = week.id;
        this.backlog.getAll('Available').subscribe({
          next: (items) => {
            this.items = items;
            this.loading = false;
          },
          error: () => {
            this.loading = false;
          },
        });
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  add(item: BacklogItem): void {
    const hrs = this.hoursMap[item.id];
    if (!hrs || hrs <= 0) {
      this.toast.show('Enter valid hours', 'error');
      return;
    }
    this.planService
      .claimItem(this.weekId, this.memberId, {
        backlogItemId: item.id,
        committedHours: hrs,
      })
      .subscribe({
        next: () => {
          this.toast.show('Added to plan!');
          this.router.navigate(['/plan-my-work']);
        },
        error: () => this.toast.show('Failed to add', 'error'),
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
