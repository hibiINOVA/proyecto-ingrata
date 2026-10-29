import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';
import { ErrorApi } from './models';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const token = auth.token;
  const peticion = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(peticion).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !req.url.includes('/api/auth/login')) {
        auth.limpiar();
        router.navigate(['/login']);
      }

      let cuerpo: ErrorApi | null = null;
      if (error.error && typeof error.error === 'object' && 'mensaje' in error.error) {
        cuerpo = error.error as ErrorApi;
      }

      if (error.status === 0) {
        return throwError(() => ({
          codigo: 'SIN_CONEXION',
          mensaje: 'No se pudo conectar con el servidor. Verifique su conexión e intente de nuevo.',
          errores: undefined,
        } satisfies ErrorApi));
      }

      return throwError(() =>
        cuerpo ??
          ({
            codigo: 'ERROR',
            mensaje: `Ocurrió un error inesperado (${error.status}).`,
            errores: undefined,
          } satisfies ErrorApi)
      );
    })
  );
};
