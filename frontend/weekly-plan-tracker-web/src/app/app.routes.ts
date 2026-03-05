import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { leadGuard } from './core/guards/lead.guard';
import { setupGuard } from './core/guards/setup.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'identity', pathMatch: 'full' },
  {
    path: 'setup',
    loadComponent: () =>
      import('./features/setup/setup.component').then((m) => m.SetupComponent),
  },
  {
    path: 'identity',
    canActivate: [setupGuard],
    loadComponent: () =>
      import('./features/identity/identity.component').then(
        (m) => m.IdentityComponent,
      ),
  },
  {
    path: 'hub',
    canActivate: [setupGuard, authGuard],
    loadComponent: () =>
      import('./features/hub/hub.component').then((m) => m.HubComponent),
  },
  {
    path: 'team',
    canActivate: [setupGuard, authGuard, leadGuard],
    loadComponent: () =>
      import('./features/team/team.component').then((m) => m.TeamComponent),
  },
  {
    path: 'backlog',
    canActivate: [setupGuard, authGuard],
    loadComponent: () =>
      import('./features/backlog/backlog-list/backlog-list.component').then(
        (m) => m.BacklogListComponent,
      ),
  },
  {
    path: 'backlog/new',
    canActivate: [setupGuard, authGuard],
    loadComponent: () =>
      import('./features/backlog/backlog-edit/backlog-edit.component').then(
        (m) => m.BacklogEditComponent,
      ),
  },
  {
    path: 'backlog/edit/:id',
    canActivate: [setupGuard, authGuard],
    loadComponent: () =>
      import('./features/backlog/backlog-edit/backlog-edit.component').then(
        (m) => m.BacklogEditComponent,
      ),
  },
  {
    path: 'backlog/pick',
    canActivate: [setupGuard, authGuard],
    loadComponent: () =>
      import('./features/backlog/backlog-pick/backlog-pick.component').then(
        (m) => m.BacklogPickComponent,
      ),
  },
  {
    path: 'week-setup',
    canActivate: [setupGuard, authGuard, leadGuard],
    loadComponent: () =>
      import('./features/planning/week-setup/week-setup.component').then(
        (m) => m.WeekSetupComponent,
      ),
  },
  {
    path: 'plan-my-work',
    canActivate: [setupGuard, authGuard],
    loadComponent: () =>
      import('./features/planning/plan-my-work/plan-my-work.component').then(
        (m) => m.PlanMyWorkComponent,
      ),
  },
  {
    path: 'review-freeze',
    canActivate: [setupGuard, authGuard, leadGuard],
    loadComponent: () =>
      import('./features/planning/review-freeze/review-freeze.component').then(
        (m) => m.ReviewFreezeComponent,
      ),
  },
  {
    path: 'update-progress',
    canActivate: [setupGuard, authGuard],
    loadComponent: () =>
      import('./features/progress/update-progress/update-progress.component').then(
        (m) => m.UpdateProgressComponent,
      ),
  },
  {
    path: 'team-progress',
    canActivate: [setupGuard, authGuard],
    loadComponent: () =>
      import('./features/progress/team-dashboard/team-dashboard.component').then(
        (m) => m.TeamDashboardComponent,
      ),
  },
  {
    path: 'team-progress/category/:category',
    canActivate: [setupGuard, authGuard],
    loadComponent: () =>
      import('./features/progress/team-dashboard/category-drill/category-drill.component').then(
        (m) => m.CategoryDrillComponent,
      ),
  },
  {
    path: 'team-progress/member/:memberId',
    canActivate: [setupGuard, authGuard],
    loadComponent: () =>
      import('./features/progress/team-dashboard/member-drill/member-drill.component').then(
        (m) => m.MemberDrillComponent,
      ),
  },
  {
    path: 'team-progress/task/:assignmentId',
    canActivate: [setupGuard, authGuard],
    loadComponent: () =>
      import('./features/progress/team-dashboard/task-drill/task-drill.component').then(
        (m) => m.TaskDrillComponent,
      ),
  },
  {
    path: 'past-weeks',
    canActivate: [setupGuard, authGuard],
    loadComponent: () =>
      import('./features/past-weeks/past-weeks.component').then(
        (m) => m.PastWeeksComponent,
      ),
  },
  { path: '**', redirectTo: 'identity' },
];
