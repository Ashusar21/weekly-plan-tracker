import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { ToastService } from '../services/toast.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const toast = inject(ToastService);
  return next(req).pipe(
    catchError((err) => {
      const message =
        err?.error?.message ?? err?.message ?? 'Something went wrong';
      if (err.status === 0)
        toast.show('Cannot reach server. Is the API running?', 'error');
      else if (err.status >= 500)
        toast.show(`Server error: ${message}`, 'error');
      else if (err.status === 404) toast.show('Resource not found', 'error');
      else if (err.status === 400)
        toast.show(`Bad request: ${message}`, 'error');
      return throwError(() => err);
    }),
  );
};
