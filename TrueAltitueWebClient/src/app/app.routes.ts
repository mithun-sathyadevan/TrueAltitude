import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';
import { HomeModulesPageComponent } from './pages/home-modules/home-modules-page.component';
import { LearningPageComponent } from './pages/learning/learning-page.component';
import { LearningShellPageComponent } from './pages/learning-shell/learning-shell-page.component';
import { LearningSubjectsPageComponent } from './pages/learning-subjects/learning-subjects-page.component';
import { LoginPageComponent } from './pages/login/login-page.component';
import { RegisterPageComponent } from './pages/register/register-page.component';
import { RealtimeExamPageComponent } from './pages/realtime-exam/realtime-exam-page.component';
import { SubscriptionPageComponent } from './pages/subscription/subscription-page.component';
import { VerifyEmailPageComponent } from './pages/verify-email/verify-email-page.component';
import { AdminDashboardComponent } from './pages/admin/admin-dashboard.component';

export const routes: Routes = [
	{ path: '', component: HomeModulesPageComponent },
	{ path: 'login', component: LoginPageComponent },
	{ path: 'register', component: RegisterPageComponent },
	{ path: 'verify-email', component: VerifyEmailPageComponent },
	{ path: 'admin', component: AdminDashboardComponent, canActivate: [adminGuard] },
	{ path: 'subscription', redirectTo: 'subscriptions', pathMatch: 'full' },
	{ path: 'subscriptions', component: SubscriptionPageComponent, canActivate: [authGuard] },
	{
		path: 'learning',
		component: LearningShellPageComponent,
		canActivate: [authGuard],
		children: [
			{ path: '', redirectTo: 'subjects', pathMatch: 'full' },
			{ path: 'subjects', component: LearningSubjectsPageComponent },
			{ path: 'topics/:subjectId', component: LearningPageComponent },
			{ path: 'realtime-exam', component: RealtimeExamPageComponent },
		],
	},
	{ path: '**', redirectTo: '' },
];
