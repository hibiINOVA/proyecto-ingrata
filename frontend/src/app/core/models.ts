export interface UsuarioSesion {
  id: number;
  identificador: string;
  nombreCompleto: string;
  rolId: number;
  rol: string;
}

export interface LoginResponse {
  token: string;
  expiraUtc: string;
  usuario: UsuarioSesion;
}

export interface Usuario {
  id: number;
  identificador: string;
  nombreCompleto: string;
  rolId: number;
  rol: string;
  activo: boolean;
  creadoUtc: string;
}

export interface Marca {
  id: number;
  nombre: string;
  activo: boolean;
}

export interface Categoria {
  id: number;
  nombre: string;
  activo: boolean;
}

export interface Producto {
  id: number;
  sku: string;
  nombre: string;
  marcaId: number;
  marca: string;
  categoriaId: number;
  categoria: string;
  unidad: string;
  activo: boolean;
  existencia: number;
}

export interface Movimiento {
  id: number;
  folio: string;
  tipo: number;
  tipoNombre: string;
  productoId: number;
  sku: string;
  productoNombre: string;
  unidad: string;
  cantidad: number;
  usuarioId: number;
  usuarioNombre: string;
  fechaUtc: string;
  proveedor?: string | null;
  referencia?: string | null;
  destino?: string | null;
  motivo?: string | null;
  movimientoOriginalId?: number | null;
  folioOriginal?: string | null;
  anulado: boolean;
}

export interface ResultadoMovimiento {
  id?: number | null;
  folio: string;
  tipo: number;
  tipoNombre: string;
  efecto: number;
  existenciaResultante?: number | null;
  mensaje: string;
}

export interface RespuestaPaginada<T> {
  pagina: number;
  tamano: number;
  total: number;
  elementos: T[];
}

export interface ExistenciaProducto {
  productoId: number;
  sku: string;
  nombre: string;
  unidad: string;
  existencia: number;
  activo: boolean;
}

export interface Dashboard {
  desde?: string | null;
  hasta?: string | null;
  totalExistencias: number;
  existencias: ExistenciaProducto[];
  totales: { entradas: number; entregas: number; mermas: number };
  ahoraUtc: string;
  ahoraLocal: string;
  zonaHoraria: string;
}

export interface ErrorApi {
  codigo: string;
  mensaje: string;
  errores?: Record<string, string[]>;
}
