import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, catchError, of } from 'rxjs';
import { TeamMemberService } from '../services/team-member.service';

/**
 * Redirects to /setup if no team members exist in the database.
 * Used on the identity and hub routes.
 */
export const setupGuard: CanActivateFn = () => {
  const teamService = inject(TeamMemberService);
  const router = inject(Router);

  return teamService.anyExists().pipe(
    map(({ exists }) => {
      if (!exists) {
        return router.createUrlTree(['/setup']);
      }
      return true;
    }),
    catchError(() => of(true)) // If API is down, let it through
  );
};
