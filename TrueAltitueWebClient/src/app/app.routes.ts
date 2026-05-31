import { Routes } from '@angular/router';

export const routes: Routes = [
	{ path: '', loadChildren: () => import('./modules/public/public.routes').then((m) => m.PUBLIC_ROUTES) },
	{ path: 'admin', loadChildren: () => import('./modules/admin/admin.routes').then((m) => m.ADMIN_ROUTES) },
	{ path: 'learning', loadChildren: () => import('./modules/learning/learning.routes').then((m) => m.LEARNING_ROUTES) },
	{ path: '**', redirectTo: '' },
];
