import { Component, OnInit, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { PlanningWeekService } from '../../core/services/planning-week.service';
import { ToastService } from '../../core/services/toast.service';
import { ConfirmModalComponent } from '../../shared/components/confirm-modal/confirm-modal.component';
import { PlanningWeek } from '../../core/models/planning-week.model';

@Component({
  selector: 'app-hub',
  standalone: true,
  imports: [CommonModule, ConfirmModalComponent],
  templateUrl: './hub.component.html',
  styleUrls: ['./hub.component.scss'],
})
export class HubComponent implements OnInit {
  member = computed(() => this.auth.currentMember());
  isLead = computed(() => this.auth.isLead());
  activeWeek: PlanningWeek | null = null;
  loading = true;
  showFinishModal = false;
  showCancelModal = false;

  constructor(
    private auth: AuthService,
    private weekService: PlanningWeekService,
    private toast: ToastService,
    public router: Router,
  ) {}

  ngOnInit(): void {
    this.weekService.getActive().subscribe({
      next: (week) => {
        this.activeWeek = week;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  get state(): string {
    return this.activeWeek?.state ?? '';
  }
  get isParticipating(): boolean {
    return (
      this.activeWeek?.participatingMemberIds.includes(
        this.member()?.id ?? '',
      ) ?? false
    );
  }

  finishWeek(): void {
    if (!this.activeWeek) return;
    this.weekService.finish(this.activeWeek.id).subscribe({
      next: () => {
        this.toast.show(
          'This week is done! You can start planning a new week.',
        );
        this.showFinishModal = false;
        this.router.navigate(['/hub']);
        this.ngOnInit();
      },
      error: () => {
        this.toast.show('Failed to finish week', 'error');
        this.showFinishModal = false;
      },
    });
  }

  cancelPlanning(): void {
    if (!this.activeWeek) return;
    this.weekService.cancel(this.activeWeek.id).subscribe({
      next: () => {
        this.toast.show('Planning has been canceled.');
        this.showCancelModal = false;
        this.ngOnInit();
      },
      error: () => {
        this.toast.show('Failed to cancel', 'error');
        this.showCancelModal = false;
      },
    });
  }

  navigate(path: string): void {
    this.router.navigate([path]);
  }
}
