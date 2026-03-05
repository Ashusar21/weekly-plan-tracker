import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { forkJoin } from 'rxjs';
import { ToastService } from '../../../core/services/toast.service';
import { TeamMemberService } from '../../../core/services/team-member.service';
import { BacklogService } from '../../../core/services/backlog.service';
import { ApiService } from '../../../core/services/api.service';
import { AuthService } from '../../../core/services/auth.service';
import { PlanningWeekService } from '../../../core/services/planning-week.service';
import { ConfirmModalComponent } from '../confirm-modal/confirm-modal.component';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, ConfirmModalComponent],
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.scss'],
})
export class FooterComponent {
  resetting = false;
  seeding = false;
  importing = false;
  importFileName = '';

  showConfirm = false;
  confirmTitle = '';
  confirmMessage = '';
  confirmLabel = 'Yes';
  confirmDanger = true;
  private pendingAction: (() => void) | null = null;

  constructor(
    private toast: ToastService,
    private teamService: TeamMemberService,
    private backlogService: BacklogService,
    private planningWeekService: PlanningWeekService,
    private api: ApiService,
    private auth: AuthService,
    private router: Router,
  ) {}

  private ask(title: string, message: string, label: string, danger: boolean, action: () => void): void {
    this.confirmTitle = title;
    this.confirmMessage = message;
    this.confirmLabel = label;
    this.confirmDanger = danger;
    this.pendingAction = action;
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
    this.importFileName = '';
  }

  downloadData(): void {
    forkJoin({
      teamMembers: this.teamService.getAll(),
      backlogItems: this.backlogService.getAll(),
      planningWeeks: this.planningWeekService.getAll(),
    }).subscribe({
      next: ({ teamMembers, backlogItems, planningWeeks }) => {
        // For each planning week, fetch full member plans with task assignments
        const weekIds = planningWeeks.map((w: any) => w.id);

        if (weekIds.length === 0) {
          this.saveExport({ teamMembers, backlogItems, planningWeeks: [] });
          return;
        }

        // Fetch progress data for each week to get task assignments + progress updates
        const progressCalls = weekIds.map((id: string) =>
          this.api.get<any>(`/progress/${id}`)
        );

        forkJoin(progressCalls).subscribe({
          next: (progressResults: any[]) => {
            // Build enriched planning weeks with member plans and task assignments
            const enrichedWeeks = planningWeeks.map((week: any, i: number) => {
              const progress = progressResults[i];
              return {
                ...week,
                memberPlans: (progress?.byMember ?? []).map((mp: any) => ({
                  id: mp.memberPlanId ?? mp.memberId,
                  memberId: mp.memberId,
                  totalPlannedHours: mp.committedHours,
                  isReady: true,
                  taskAssignments: (mp.tasks ?? []).map((ta: any) => ({
                    id: ta.id,
                    backlogItemId: ta.backlogItemId,
                    committedHours: ta.committedHours,
                    hoursCompleted: ta.hoursCompleted,
                    progressStatus: ta.progressStatus,
                    createdAt: ta.createdAt ?? new Date().toISOString(),
                    progressUpdates: []
                  }))
                }))
              };
            });
            this.saveExport({ teamMembers, backlogItems, planningWeeks: enrichedWeeks });
          },
          error: () => {
            // Fall back to weeks without task detail
            this.saveExport({ teamMembers, backlogItems, planningWeeks });
          }
        });
      },
      error: () => this.toast.show('Failed to export data. Is the API running?', 'error'),
    });
  }

  private saveExport(data: any): void {
    const exportObj = {
      appName: 'WeeklyPlanTracker',
      dataVersion: 2,
      exportedAt: new Date().toISOString(),
      data,
    };
    const blob = new Blob([JSON.stringify(exportObj, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    const d = new Date();
    a.download = `weeklyplantracker-backup-${d.toISOString().slice(0, 10)}-${d.toTimeString().slice(0, 8).replace(/:/g, '')}.json`;
    a.click();
    URL.revokeObjectURL(url);
    this.toast.show('Your data was saved to a file.');
  }

  loadData(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.importFileName = file.name;

    const reader = new FileReader();
    reader.onload = (e) => {
      try {
        const parsed = JSON.parse(e.target?.result as string);
        if (parsed.appName !== 'WeeklyPlanTracker') {
          this.toast.show("This file doesn't look like a backup from this app.", 'error');
          return;
        }
        if (!parsed.data?.teamMembers) {
          this.toast.show('Backup file is missing required data.', 'error');
          return;
        }
        this.ask(
          'Load Data from File?',
          `Load data from "${file.name}"? This will replace ALL current data. This cannot be undone.`,
          'Yes, Replace My Data',
          true,
          () => this.executeImport(parsed),
        );
      } catch {
        this.toast.show("This file can't be read.", 'error');
      }
    };
    reader.readAsText(file);
  }

  private executeImport(parsed: any): void {
    this.importing = true;
    const { teamMembers, backlogItems, backlogEntries, planningWeeks } = parsed.data;
    const backlog = backlogItems ?? backlogEntries ?? [];

    // Build restore payload matching backend RestorePayload DTO
    const payload = {
      teamMembers: teamMembers.map((m: any) => ({
        id: m.id,
        name: m.name,
        isLead: m.isLead ?? false,
        isActive: m.isActive ?? true,
        createdAt: m.createdAt ?? new Date().toISOString(),
      })),
      backlogItems: backlog.map((b: any) => ({
        id: b.id,
        title: b.title,
        description: b.description ?? '',
        category: this.mapCategory(b.category),
        estimatedEffort: b.estimatedEffort ?? null,
        status: this.mapStatus(b.status),
        createdAt: b.createdAt ?? new Date().toISOString(),
      })),
      planningWeeks: (planningWeeks ?? []).map((w: any) => ({
        id: w.id,
        planningDate: w.planningDate,
        executionStartDate: w.executionStartDate,
        executionEndDate: w.executionEndDate,
        teamCapacity: w.teamCapacity,
        state: this.mapWeekState(w.state),
        createdAt: w.createdAt ?? new Date().toISOString(),
        categoryAllocations: (w.categoryAllocations ?? []).map((a: any) => ({
          id: a.id ?? this.newGuid(),
          category: this.mapCategory(a.category),
          percentage: a.percentage,
          budgetHours: a.budgetHours,
        })),
        memberPlans: (w.memberPlans ?? []).map((mp: any) => ({
          id: mp.id ?? this.newGuid(),
          memberId: mp.memberId,
          totalPlannedHours: mp.totalPlannedHours ?? mp.committedHours ?? 0,
          isReady: mp.isReady ?? true,
          taskAssignments: (mp.taskAssignments ?? []).map((ta: any) => ({
            id: ta.id ?? this.newGuid(),
            backlogItemId: ta.backlogItemId,
            committedHours: ta.committedHours,
            hoursCompleted: ta.hoursCompleted ?? 0,
            progressStatus: this.mapProgressStatus(ta.progressStatus),
            createdAt: ta.createdAt ?? new Date().toISOString(),
            progressUpdates: (ta.progressUpdates ?? []).map((pu: any) => ({
              id: pu.id ?? this.newGuid(),
              updatedBy: pu.updatedBy,
              previousHoursCompleted: pu.previousHoursCompleted ?? 0,
              newHoursCompleted: pu.newHoursCompleted ?? 0,
              previousStatus: this.mapProgressStatus(pu.previousStatus),
              newStatus: this.mapProgressStatus(pu.newStatus),
              note: pu.note ?? '',
              timestamp: pu.timestamp ?? new Date().toISOString(),
            }))
          }))
        }))
      }))
    };

    this.api.post<void>('/restore', payload).subscribe({
      next: () => {
        this.auth.logout();
        localStorage.clear();
        this.importing = false;
        this.importFileName = '';
        this.toast.show('Data loaded! Redirecting...');
        setTimeout(() => (window.location.href = '/identity'), 800);
      },
      error: () => {
        this.toast.show('Failed to import data. Is the API running?', 'error');
        this.importing = false;
      }
    });
  }

  private mapCategory(cat: any): number {
    if (typeof cat === 'number') return cat;
    const map: Record<string, number> = { ClientFocused: 1, TechDebt: 2, RAndD: 3 };
    return map[cat] ?? 1;
  }

  private mapStatus(status: any): number {
    if (typeof status === 'number') return status;
    const map: Record<string, number> = { Available: 1, Archived: 2, Completed: 3 };
    return map[status] ?? 1;
  }

  private mapWeekState(state: any): number {
    if (typeof state === 'number') return state;
    const map: Record<string, number> = { Setup: 1, Planning: 2, Frozen: 3, Completed: 4 };
    return map[state] ?? 1;
  }

  private mapProgressStatus(status: any): number {
    if (typeof status === 'number') return status;
    const map: Record<string, number> = { NotStarted: 1, InProgress: 2, Completed: 3, Blocked: 4 };
    return map[status] ?? 1;
  }

  private newGuid(): string {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = Math.random() * 16 | 0;
      return (c === 'x' ? r : (r & 0x3 | 0x8)).toString(16);
    });
  }

  seedData(): void {
    this.ask(
      'Seed Sample Data?',
      'This will reset all data and load sample team members and backlog items. Are you sure?',
      'Yes, Load Sample Data',
      false,
      () => this.executeSeed(),
    );
  }

  private executeSeed(): void {
    this.seeding = true;
    const members = ['Alice Chen', 'Bob Martinez', 'Carol Singh', 'Dave Kim'];
    const backlog = [
      { title: 'Customer onboarding redesign', description: 'Revamp the onboarding flow.', category: 'ClientFocused' as const, estimatedEffort: 12 },
      { title: 'Fix billing invoice formatting', description: 'Some invoices show wrong currency format.', category: 'ClientFocused' as const, estimatedEffort: 4 },
      { title: 'Customer feedback dashboard', description: 'Build a dashboard showing NPS scores.', category: 'ClientFocused' as const, estimatedEffort: 16 },
      { title: 'Migrate database to PostgreSQL 16', description: 'Upgrade from PG 14 to PG 16.', category: 'TechDebt' as const, estimatedEffort: 20 },
      { title: 'Remove deprecated API endpoints', description: 'Clean up v1 API routes.', category: 'TechDebt' as const, estimatedEffort: 8 },
      { title: 'Add unit tests for payment module', description: 'Coverage is below 50%.', category: 'TechDebt' as const, estimatedEffort: 10 },
      { title: 'Experiment with LLM-based search', description: 'Prototype semantic search using embeddings.', category: 'RAndD' as const, estimatedEffort: 15 },
      { title: 'Evaluate new caching strategy', description: 'Compare Redis Cluster vs Memcached.', category: 'RAndD' as const, estimatedEffort: 6 },
      { title: 'Build internal CLI tool', description: 'A command-line tool for common dev tasks.', category: 'RAndD' as const, estimatedEffort: 8 },
      { title: 'Client SSO integration', description: 'Support SAML-based SSO for enterprise clients.', category: 'ClientFocused' as const, estimatedEffort: 18 },
    ];

    const createMembersSequentially = (index: number): Promise<void> => {
      if (index >= members.length) return Promise.resolve();
      return this.teamService.create({ name: members[index] }).toPromise()
        .then(() => createMembersSequentially(index + 1));
    };

    const createBacklogSequentially = (index: number): Promise<void> => {
      if (index >= backlog.length) return Promise.resolve();
      return this.backlogService.create(backlog[index]).toPromise()
        .then(() => createBacklogSequentially(index + 1));
    };

    this.api.delete<void>('/reset').toPromise()
      .then(() => createMembersSequentially(0))
      .then(() => createBacklogSequentially(0))
      .then(() => {
        this.auth.logout();
        localStorage.clear();
        this.seeding = false;
        this.toast.show('Sample data loaded! Pick a person to get started.');
        setTimeout(() => (window.location.href = '/identity'), 800);
      })
      .catch(() => {
        this.toast.show('Failed to seed data. Is the API running?', 'error');
        this.seeding = false;
      });
  }

  resetApp(): void {
    this.ask(
      'Reset Everything?',
      'This will erase all your data. This cannot be undone.',
      'Yes, Erase Everything',
      true,
      () => this.executeReset(),
    );
  }

  private executeReset(): void {
    this.resetting = true;
    this.api.delete<void>('/reset').subscribe({
      next: () => {
        this.auth.logout();
          localStorage.clear();
          this.resetting = false;
          this.toast.show('App reset!');
          setTimeout(() => (window.location.href = '/setup'), 800);
        },
        error: () => {
          this.toast.show('Reset failed. Is the API running?', 'error');
          this.resetting = false;
        },
      });
    }
  }
