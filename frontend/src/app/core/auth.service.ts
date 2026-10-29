import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, map, Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { LoginResponse, UsuarioSesion } from './models';

const CLAVE_TOKEN = 'ingrata_token';
const CLAVE_USUARIO = 'ingrata_usuario';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly estado = new BehaviorSubject<{ token: string | null; usuario: UsuarioSesion | null }>(
    this.leer()
  );

  readonly sesion$ = this.estado.asObservable();
  readonly usuario$ = this.estado.pipe(map((estado) => estado.usuario));

  constructor(private readonly http: HttpClient) {}

  get token(): string | null {
    return this.estado.value.token;
  }

  get usuario(): UsuarioSesion | null {
    return this.estado.value.usuario;
  }

  get esAdministrador(): boolean {
    return this.estado.value.usuario?.rol === 'Administrador';
  }

  get autenticado(): boolean {
    return !!this.estado.value.token;
  }

  login(identificador: string, password: string): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/api/auth/login`, { identificador, password })
      .pipe(
        tap((respuesta) => {
          localStorage.setItem(CLAVE_TOKEN, respuesta.token);
          localStorage.setItem(CLAVE_USUARIO, JSON.stringify(respuesta.usuario));
          this.estado.next({ token: respuesta.token, usuario: respuesta.usuario });
        })
      );
  }

  logout(): void {
    const token = this.token;
    if (token) {
      this.http
        .post(`${environment.apiUrl}/api/auth/logout`, {})
        .subscribe({ error: () => void 0 });
    }
    this.limpiar();
  }

  limpiar(): void {
    localStorage.removeItem(CLAVE_TOKEN);
    localStorage.removeItem(CLAVE_USUARIO);
    this.estado.next({ token: null, usuario: null });
  }

  private leer(): { token: string | null; usuario: UsuarioSesion | null } {
    const token = localStorage.getItem(CLAVE_TOKEN);
    const usuarioTexto = localStorage.getItem(CLAVE_USUARIO);
    let usuario: UsuarioSesion | null = null;
    if (usuarioTexto) {
      try {
        usuario = JSON.parse(usuarioTexto) as UsuarioSesion;
      } catch {
        usuario = null;
      }
    }
    return { token, usuario };
  }
}
