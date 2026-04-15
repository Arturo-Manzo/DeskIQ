import { Routes } from '@angular/router';
import { authGuard, adminGuard, metricsGuard, departmentsGuard, ticketCreateGuard } from './core/auth/auth.guard';

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
				canActivate: [metricsGuard],
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
				canActivate: [ticketCreateGuard],
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
				canActivate: [departmentsGuard],
				loadComponent: () =>
					import('./features/departments/departments-page.component').then((m) => m.DepartmentsPageComponent),
			},
			{
				path: 'departments/new',
				canActivate: [departmentsGuard],
				loadComponent: () =>
					import('./features/departments/department-create-page.component').then((m) => m.DepartmentCreatePageComponent),
			},
			{
				path: 'departments/:id/edit',
				canActivate: [departmentsGuard],
				loadComponent: () =>
					import('./features/departments/department-create-page.component').then((m) => m.DepartmentCreatePageComponent),
			},
			{
				path: 'users',
				canActivate: [adminGuard],
				loadComponent: () =>
					import('./features/users/users-page.component').then((m) => m.UsersPageComponent),
			},
			{
				path: 'users/new',
				canActivate: [adminGuard],
				loadComponent: () =>
					import('./features/users/user-create-page.component').then((m) => m.UserCreatePageComponent),
			},
			{
				path: 'users/:id',
				canActivate: [adminGuard],
				loadComponent: () =>
					import('./features/users/user-detail-page.component').then((m) => m.UserDetailPageComponent),
			},
			{
				path: 'users/:id/edit',
				canActivate: [adminGuard],
				loadComponent: () =>
					import('./features/users/user-edit-page.component').then((m) => m.UserEditPageComponent),
			},
		],
	},
	{
		path: '**',
		redirectTo: '',
	},
];
