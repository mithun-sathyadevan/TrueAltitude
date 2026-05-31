import { Routes } from '@angular/router';

import { adminGuard } from '../../guards/admin.guard';
import { AdminDashboardComponent } from '../../pages/admin/admin-dashboard.component';

export const ADMIN_ROUTES: Routes = [
  { path: '', component: AdminDashboardComponent, canActivate: [adminGuard], pathMatch: 'full' },
];