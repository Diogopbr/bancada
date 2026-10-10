import { inject, Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, BaseRouteReuseStrategy } from '@angular/router';
import { SessionService } from './session.service';

@Injectable()
export class SessionRoutes extends BaseRouteReuseStrategy {
  private readonly session = inject(SessionService);

  override shouldReuseRoute(future: ActivatedRouteSnapshot, current: ActivatedRouteSnapshot): boolean {
    return !this.session.refreshRoutes && super.shouldReuseRoute(future, current);
  }
}
