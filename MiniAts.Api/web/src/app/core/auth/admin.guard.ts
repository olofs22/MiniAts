import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { MeService } from '../me.service';

export const adminGuard: CanActivateFn = async () => {
  const meService = inject(MeService);
  const router = inject(Router);

  const me = meService.me() ?? (await meService.load().catch(() => null));

  return me?.role === 'Admin' ? true : router.parseUrl('/');
};
