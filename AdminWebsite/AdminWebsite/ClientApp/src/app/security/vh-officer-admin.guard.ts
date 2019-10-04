import { Injectable } from '@angular/core';
import { CanActivate, Router, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { UserIdentityService } from '../services/user-identity.service';
import { Observable, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { Logger } from '../services/logger';

@Injectable()
export class VhOfficerAdminGuard implements CanActivate {

  constructor(private userIdentityService: UserIdentityService,
    private router: Router,
    private logger: Logger) { }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> {
    return this.userIdentityService.getUserInformation().pipe(
      map(userProfile => {
        if (userProfile && userProfile.is_vh_officer_administrator_role) {
          return true;
        } else {
          this.logger.warn('User is not the VH officer administrator');
          this.router.navigate(['/login']);
          return false;
        }
      }),
      catchError((err) => {
        this.logger.error('Could not get user identity.', err);
        this.router.navigate(['/login']);
        return of(false);
      })
    );
  }
}
