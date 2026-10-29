import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import {
  Categoria,
  Dashboard,
  Marca,
  Movimiento,
  Producto,
  RespuestaPaginada,
  ResultadoMovimiento,
  Usuario,
} from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly base = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  // ---------- Catálogo ----------
  getMarcas(activo?: boolean): Observable<Marca[]> {
    let params = new HttpParams();
    if (activo !== undefined) {
      params = params.set('activo', String(activo));
    }
    return this.http.get<Marca[]>(`${this.base}/api/marcas`, { params });
  }

  crearMarca(nombre: string): Observable<Marca> {
    return this.http.post<Marca>(`${this.base}/api/marcas`, { nombre });
  }

  editarMarca(id: number, nombre: string): Observable<Marca> {
    return this.http.put<Marca>(`${this.base}/api/marcas/${id}`, { nombre });
  }

  estadoMarca(id: number, activo: boolean): Observable<Marca> {
    return this.http.patch<Marca>(`${this.base}/api/marcas/${id}/estado`, { activo });
  }

  getCategorias(activo?: boolean): Observable<Categoria[]> {
    let params = new HttpParams();
    if (activo !== undefined) {
      params = params.set('activo', String(activo));
    }
    return this.http.get<Categoria[]>(`${this.base}/api/categorias`, { params });
  }

  crearCategoria(nombre: string): Observable<Categoria> {
    return this.http.post<Categoria>(`${this.base}/api/categorias`, { nombre });
  }

  editarCategoria(id: number, nombre: string): Observable<Categoria> {
    return this.http.put<Categoria>(`${this.base}/api/categorias/${id}`, { nombre });
  }

  estadoCategoria(id: number, activo: boolean): Observable<Categoria> {
    return this.http.patch<Categoria>(`${this.base}/api/categorias/${id}/estado`, { activo });
  }

  getProductos(filtros: { texto?: string; marcaId?: number; categoriaId?: number; activo?: boolean } = {}): Observable<Producto[]> {
    let params = new HttpParams();
    if (filtros.texto) {
      params = params.set('texto', filtros.texto);
    }
    if (filtros.marcaId !== undefined) {
      params = params.set('marcaId', String(filtros.marcaId));
    }
    if (filtros.categoriaId !== undefined) {
      params = params.set('categoriaId', String(filtros.categoriaId));
    }
    if (filtros.activo !== undefined) {
      params = params.set('activo', String(filtros.activo));
    }
    return this.http.get<Producto[]>(`${this.base}/api/productos`, { params });
  }

  crearProducto(producto: Partial<Producto>): Observable<Producto> {
    return this.http.post<Producto>(`${this.base}/api/productos`, producto);
  }

  editarProducto(id: number, producto: Partial<Producto>): Observable<Producto> {
    return this.http.put<Producto>(`${this.base}/api/productos/${id}`, producto);
  }

  estadoProducto(id: number, activo: boolean): Observable<Producto> {
    return this.http.patch<Producto>(`${this.base}/api/productos/${id}/estado`, { activo });
  }

  // ---------- Usuarios ----------
  getUsuarios(): Observable<Usuario[]> {
    return this.http.get<Usuario[]>(`${this.base}/api/usuarios`);
  }

  crearUsuario(usuario: { identificador: string; nombreCompleto: string; password: string; rolId: number }): Observable<Usuario> {
    return this.http.post<Usuario>(`${this.base}/api/usuarios`, usuario);
  }

  editarUsuario(id: number, usuario: { nombreCompleto: string; password?: string; rolId: number }): Observable<Usuario> {
    return this.http.put<Usuario>(`${this.base}/api/usuarios/${id}`, usuario);
  }

  estadoUsuario(id: number, activo: boolean): Observable<Usuario> {
    return this.http.patch<Usuario>(`${this.base}/api/usuarios/${id}/estado`, { activo });
  }

  // ---------- Movimientos ----------
  registrarMovimiento(
    tipo: 'entradas' | 'entregas' | 'mermas',
    movimiento: {
      productoId: number;
      cantidad: number;
      proveedor?: string;
      referencia?: string;
      destino?: string;
      motivo?: string;
      requestId: string;
    }
  ): Observable<ResultadoMovimiento> {
    return this.http.post<ResultadoMovimiento>(`${this.base}/api/movimientos/${tipo}`, movimiento);
  }

  anularMovimiento(id: number, motivo: string): Observable<ResultadoMovimiento> {
    return this.http.post<ResultadoMovimiento>(`${this.base}/api/movimientos/${id}/anulacion`, { motivo });
  }

  getMovimientos(filtros: {
    desde?: string;
    hasta?: string;
    productoId?: number;
    tipo?: number;
    usuarioId?: number;
    pagina?: number;
  } = {}): Observable<RespuestaPaginada<Movimiento>> {
    let params = new HttpParams();
    Object.entries(filtros).forEach(([clave, valor]) => {
      if (valor !== undefined && valor !== null && valor !== '') {
        params = params.set(clave, String(valor));
      }
    });
    return this.http.get<RespuestaPaginada<Movimiento>>(`${this.base}/api/movimientos`, { params });
  }

  // ---------- Dashboard ----------
  getDashboard(desde?: string, hasta?: string): Observable<Dashboard> {
    let params = new HttpParams();
    if (desde) {
      params = params.set('desde', desde);
    }
    if (hasta) {
      params = params.set('hasta', hasta);
    }
    return this.http.get<Dashboard>(`${this.base}/api/dashboard`, { params });
  }
}
