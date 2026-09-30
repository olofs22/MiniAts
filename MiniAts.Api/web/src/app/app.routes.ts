import { Routes } from '@angular/router';
import { adminGuard } from './core/auth/admin.guard';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'accept-invite',
    loadComponent: () =>
      import('./features/auth/accept-invite/accept-invite').then((m) => m.AcceptInvite),
  },
  {
    path: '',
    loadComponent: () => import('./core/layout/shell/shell').then((m) => m.Shell),
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
      },
      {
        path: 'jobs',
        loadComponent: () =>
          import('./features/jobs/jobs-list/jobs-list').then((m) => m.JobsList),
      },
      {
        path: 'jobs/new',
        loadComponent: () => import('./features/jobs/job-form/job-form').then((m) => m.JobForm),
      },
      {
        path: 'jobs/:id/edit',
        loadComponent: () => import('./features/jobs/job-form/job-form').then((m) => m.JobForm),
      },
      {
        path: 'jobs/:jobId/board',
        loadComponent: () => import('./features/kanban/board/board').then((m) => m.Board),
      },
      {
        path: 'candidates',
        loadComponent: () =>
          import('./features/candidates/candidates-list/candidates-list').then(
            (m) => m.CandidatesList,
          ),
      },
      {
        path: 'candidates/new',
        loadComponent: () =>
          import('./features/candidates/candidate-form/candidate-form').then(
            (m) => m.CandidateForm,
          ),
      },
      {
        path: 'candidates/:id',
        loadComponent: () =>
          import('./features/candidates/candidate-detail/candidate-detail').then(
            (m) => m.CandidateDetail,
          ),
      },
      {
        path: 'candidates/:id/edit',
        loadComponent: () =>
          import('./features/candidates/candidate-form/candidate-form').then(
            (m) => m.CandidateForm,
          ),
      },
      {
        path: 'admin',
        loadComponent: () => import('./features/admin/org-list/org-list').then((m) => m.OrgList),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/organizations/new',
        loadComponent: () =>
          import('./features/admin/org-form/org-form').then((m) => m.OrgForm),
        canActivate: [adminGuard],
      },
      {
        path: 'admin/users/new',
        loadComponent: () =>
          import('./features/admin/user-invite-form/user-invite-form').then(
            (m) => m.UserInviteForm,
          ),
        canActivate: [adminGuard],
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
