import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'setup', pathMatch: 'full' },
  {
    path: 'setup',
    loadComponent: () =>
      import('./features/setup/setup.component').then((m) => m.SetupComponent),
  },
  {
    path: 'identity',
    loadComponent: () =>
      import('./features/identity/identity.component').then(
        (m) => m.IdentityComponent,
      ),
  },
  {
    path: 'hub',
    loadComponent: () =>
      import('./features/hub/hub.component').then((m) => m.HubComponent),
  },
  {
    path: 'team',
    loadComponent: () =>
      import('./features/team/team.component').then((m) => m.TeamComponent),
  },
  {
    path: 'backlog',
    loadComponent: () =>
      import('./features/backlog/backlog-list/backlog-list.component').then(
        (m) => m.BacklogListComponent,
      ),
  },
  {
    path: 'backlog/new',
    loadComponent: () =>
      import('./features/backlog/backlog-edit/backlog-edit.component').then(
        (m) => m.BacklogEditComponent,
      ),
  },
  {
    path: 'backlog/edit/:id',
    loadComponent: () =>
      import('./features/backlog/backlog-edit/backlog-edit.component').then(
        (m) => m.BacklogEditComponent,
      ),
  },
  {
    path: 'backlog/pick',
    loadComponent: () =>
      import('./features/backlog/backlog-pick/backlog-pick.component').then(
        (m) => m.BacklogPickComponent,
      ),
  },
  {
    path: 'week-setup',
    loadComponent: () =>
      import('./features/planning/week-setup/week-setup.component').then(
        (m) => m.WeekSetupComponent,
      ),
  },
  {
    path: 'plan-my-work',
    loadComponent: () =>
      import('./features/planning/plan-my-work/plan-my-work.component').then(
        (m) => m.PlanMyWorkComponent,
      ),
  },
  {
    path: 'review-freeze',
    loadComponent: () =>
      import('./features/planning/review-freeze/review-freeze.component').then(
        (m) => m.ReviewFreezeComponent,
      ),
  },
  {
    path: 'update-progress',
    loadComponent: () =>
      import('./features/progress/update-progress/update-progress.component').then(
        (m) => m.UpdateProgressComponent,
      ),
  },
  {
    path: 'team-progress',
    loadComponent: () =>
      import('./features/progress/team-dashboard/team-dashboard.component').then(
        (m) => m.TeamDashboardComponent,
      ),
  },
  {
    path: 'team-progress/category/:category',
    loadComponent: () =>
      import('./features/progress/team-dashboard/category-drill/category-drill.component').then(
        (m) => m.CategoryDrillComponent,
      ),
  },
  {
    path: 'team-progress/member/:memberId',
    loadComponent: () =>
      import('./features/progress/team-dashboard/member-drill/member-drill.component').then(
        (m) => m.MemberDrillComponent,
      ),
  },
  {
    path: 'team-progress/task/:assignmentId',
    loadComponent: () =>
      import('./features/progress/team-dashboard/task-drill/task-drill.component').then(
        (m) => m.TaskDrillComponent,
      ),
  },
  {
    path: 'past-weeks',
    loadComponent: () =>
      import('./features/past-weeks/past-weeks.component').then(
        (m) => m.PastWeeksComponent,
      ),
  },
  { path: '**', redirectTo: 'setup' },
];
