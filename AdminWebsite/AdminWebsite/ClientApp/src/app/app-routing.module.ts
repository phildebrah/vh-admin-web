import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';

import { DashboardComponent } from './dashboard/dashboard.component';
import { LoginComponent } from './security/login.component';
import { LogoutComponent } from './security/logout.component';
import { UnauthorisedComponent } from './error/unauthorised.component';
import { ErrorComponent } from './error/error.component';
import { AdminGuard } from './security/admin.guard';
import { UnsupportedBrowserComponent } from './shared/unsupported-browser/unsupported-browser.component';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent, canActivate: [AdminGuard] },
  { path: 'login', component: LoginComponent },
  { path: 'logout', component: LogoutComponent },
  { path: 'unauthorised', component: UnauthorisedComponent },
  { path: 'error', component: ErrorComponent },
  { path: 'unsupported-browser', component: UnsupportedBrowserComponent },
  { path: '**', redirectTo: 'dashboard', pathMatch: 'full', canActivate: [AdminGuard] }
];

@NgModule({
  exports: [
    RouterModule
  ],
  imports: [
    RouterModule.forRoot(routes)],
})

export class AppRoutingModule { }
