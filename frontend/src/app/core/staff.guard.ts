import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { SessionService } from './session.service';

export const staffGuard: CanActivateFn = async () => {
  const session = inject(SessionService);
  const router = inject(Router);
  if (!session.isInitialized()) await session.loadSession();
  return session.isAuthenticated() || router.createUrlTree(['/entrar']);
};
