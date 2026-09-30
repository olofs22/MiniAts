import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./features/auth/login/login').then((m) => m.Login),
  },
  {
    path: '',
    loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard),
    canActivate: [authGuard],
  },
  {
    path: 'jobs',
    loadComponent: () => import('./features/jobs/jobs-list/jobs-list').then((m) => m.JobsList),
    canActivate: [authGuard],
  },
  {
    path: 'jobs/new',
    loadComponent: () => import('./features/jobs/job-form/job-form').then((m) => m.JobForm),
    canActivate: [authGuard],
  },
  {
    path: 'jobs/:id/edit',
    loadComponent: () => import('./features/jobs/job-form/job-form').then((m) => m.JobForm),
    canActivate: [authGuard],
  },
  {
    path: 'jobs/:jobId/board',
    loadComponent: () => import('./features/kanban/board/board').then((m) => m.Board),
    canActivate: [authGuard],
  },
  {
    path: 'candidates',
    loadComponent: () =>
      import('./features/candidates/candidates-list/candidates-list').then(
        (m) => m.CandidatesList,
      ),
    canActivate: [authGuard],
  },
  {
    path: 'candidates/new',
    loadComponent: () =>
      import('./features/candidates/candidate-form/candidate-form').then((m) => m.CandidateForm),
    canActivate: [authGuard],
  },
  {
    path: 'candidates/:id',
    loadComponent: () =>
      import('./features/candidates/candidate-detail/candidate-detail').then(
        (m) => m.CandidateDetail,
      ),
    canActivate: [authGuard],
  },
  {
    path: 'candidates/:id/edit',
    loadComponent: () =>
      import('./features/candidates/candidate-form/candidate-form').then((m) => m.CandidateForm),
    canActivate: [authGuard],
  },
  { path: '**', redirectTo: '' },
];
