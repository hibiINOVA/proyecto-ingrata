import { AsyncPipe } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, AsyncPipe],
  template: `
    <header class="cabecera">
      <div class="cabecera-inner">
        <div class="marca">
          <span class="marca-logo">LI</span>
          <div>
            <strong>Inventario Ingrata</strong>
            <small>La Ingrata</small>
          </div>
        </div>

        @if (auth.usuario$ | async; as usuario) {
          <div class="sesion">
            <span class="sesion-usuario">
              {{ usuario.nombreCompleto }}
              <em class="badge" [class.badge-admin]="usuario.rol === 'Administrador'">{{ usuario.rol }}</em>
            </span>
            <button type="button" class="btn btn-secundario btn-sm" (click)="cerrarSesion()">Salir</button>
          </div>
        }
      </div>

      <nav class="nav">
        <a routerLink="/dashboard" routerLinkActive="activo">Dashboard</a>
        <a routerLink="/productos" routerLinkActive="activo">Productos</a>
        <a routerLink="/movimientos/nuevo" routerLinkActive="activo">Nuevo movimiento</a>
        <a routerLink="/historial" routerLinkActive="activo">Historial</a>
        @if (auth.esAdministrador) {
          <a routerLink="/marcas" routerLinkActive="activo">Marcas</a>
          <a routerLink="/categorias" routerLinkActive="activo">Categorías</a>
          <a routerLink="/usuarios" routerLinkActive="activo">Usuarios</a>
        }
      </nav>
    </header>

    <main class="contenido">
      <router-outlet />
    </main>

    <footer class="pie">
      Plataforma web de control de inventarios — La Ingrata · Bajío Stack (ITSES)
    </footer>
  `,
})
export class LayoutComponent {
  constructor(readonly auth: AuthService) {}

  cerrarSesion(): void {
    this.auth.logout();
    window.location.href = '/login';
  }
}
