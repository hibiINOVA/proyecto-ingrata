import { Routes } from '@angular/router';
import { adminGuard, authGuard } from './core/auth.guard';
import { DashboardComponent } from './pages/dashboard/dashboard.component';
import { HistorialComponent } from './pages/historial/historial.component';
import { LoginComponent } from './pages/login/login.component';
import { MarcasComponent } from './pages/marcas/marcas.component';
import { CategoriasComponent } from './pages/categorias/categorias.component';
import { MovimientoNuevoComponent } from './pages/movimiento-nuevo/movimiento-nuevo.component';
import { ProductosComponent } from './pages/productos/productos.component';
import { UsuariosComponent } from './pages/usuarios/usuarios.component';
import { LayoutComponent } from './layout/layout.component';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  {
    path: '',
    component: LayoutComponent,
    canActivate: [authGuard],
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      { path: 'dashboard', component: DashboardComponent },
      { path: 'productos', component: ProductosComponent },
      { path: 'movimientos/nuevo', component: MovimientoNuevoComponent },
      { path: 'historial', component: HistorialComponent },
      { path: 'marcas', component: MarcasComponent, canActivate: [adminGuard] },
      { path: 'categorias', component: CategoriasComponent, canActivate: [adminGuard] },
      { path: 'usuarios', component: UsuariosComponent, canActivate: [adminGuard] },
    ],
  },
  { path: '**', redirectTo: '' },
];
