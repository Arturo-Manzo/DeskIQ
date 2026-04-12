import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
	{
		path: 'login',
		loadComponent: () =>
			import('./features/auth/login-page.component').then((m) => m.LoginPageComponent),
	},
	{
		path: '',
		canActivate: [authGuard],
		loadComponent: () =>
			import('./shared/shell/shell.component').then((m) => m.ShellComponent),
		children: [
			{
				path: '',
				pathMatch: 'full',
				redirectTo: 'dashboard',
			},
			{
				path: 'dashboard',
				loadComponent: () =>
					import('./features/dashboard/dashboard-page.component').then(
						(m) => m.DashboardPageComponent,
					),
			},
			{
				path: 'tickets',
				loadComponent: () =>
					import('./features/tickets/tickets-page.component').then((m) => m.TicketsPageComponent),
			},
			{
				path: 'tickets/new',
				loadComponent: () =>
					import('./features/tickets/ticket-create-page.component').then((m) => m.TicketCreatePageComponent),
			},
			{
				path: 'tickets/:id',
				loadComponent: () =>
					import('./features/tickets/ticket-detail-page.component').then((m) => m.TicketDetailPageComponent),
			},
			{
				path: 'departments',
				loadComponent: () =>
					import('./features/departments/departments-page.component').then((m) => m.DepartmentsPageComponent),
			},
			{
				path: 'departments/new',
				loadComponent: () =>
					import('./features/departments/department-create-page.component').then((m) => m.DepartmentCreatePageComponent),
			},
			{
				path: 'departments/:id/edit',
				loadComponent: () =>
					import('./features/departments/department-create-page.component').then((m) => m.DepartmentCreatePageComponent),
			},
		],
	},
	{
		path: '**',
		redirectTo: '',
	},
];
