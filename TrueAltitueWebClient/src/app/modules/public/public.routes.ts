import { Routes } from '@angular/router';

import { authGuard } from '../../guards/auth.guard';
import { subscriptionPageGuard } from '../../guards/subscription-page.guard';
import { HomeModulesPageComponent } from '../../pages/home-modules/home-modules-page.component';
import { LoginPageComponent } from '../../pages/login/login-page.component';
import { RegisterPageComponent } from '../../pages/register/register-page.component';
import { SubscriptionPageComponent } from '../../pages/subscription/subscription-page.component';
import { VerifyEmailPageComponent } from '../../pages/verify-email/verify-email-page.component';

export const PUBLIC_ROUTES: Routes = [
  { path: '', component: HomeModulesPageComponent, pathMatch: 'full' },
  { path: 'login', component: LoginPageComponent },
  { path: 'register', component: RegisterPageComponent },
  { path: 'verify-email', component: VerifyEmailPageComponent },
  { path: 'subscription', redirectTo: 'subscriptions', pathMatch: 'full' },
  { path: 'subscriptions', component: SubscriptionPageComponent, canActivate: [authGuard, subscriptionPageGuard] },
];