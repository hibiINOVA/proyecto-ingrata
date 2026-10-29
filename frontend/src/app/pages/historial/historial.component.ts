import { Component, inject } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { formatoLocal, hoyLocal, restarDiasLocal } from '../../core/fecha.util';
import { ErrorApi, Movimiento, Usuario } from '../../core/models';

@Component({
  selector: 'app-historial',
  standalone: true,
  imports: [ReactiveFormsModule, FormsModule],
  template: `
    <div class="pagina-cabecera">
      <h1>Historial de movimientos</h1>
    </div>

    <div class="card filtros">
      <label class="campo">
        <span>Desde</span>
        <input type="date" [formGroup]="filtro" formControlName="desde" />
      </label>
      <label class="campo">
        <span>Hasta</span>
        <input type="date" [formGroup]="filtro" formControlName="hasta" />
      </label>
      <label class="campo">
        <span>Tipo</span>
        <select [formGroup]="filtro" formControlName="tipo">
          <option value="">Todos</option>
          <option value="1">Entrada</option>
          <option value="2">Entrega</option>
          <option value="3">Merma</option>
          <option value="4">Reversión</option>
        </select>
      </label>
      @if (auth.esAdministrador) {
        <label class="campo">
          <span>Usuario</span>
          <select [formGroup]="filtro" formControlName="usuarioId">
            <option value="">Todos</option>
            @for (usuario of usuarios; track usuario.id) {
              <option [value]="usuario.id">{{ usuario.nombreCompleto }}</option>
            }
          </select>
        </label>
      }
      <div class="acciones">
        <button type="button" class="btn btn-secundario" (click)="limpiarFiltros()">Limpiar</button>
      </div>
    </div>

    @if (error) {
      <div class="alerta alerta-error">{{ error.mensaje }}</div>
    }

    @if (anulando) {
      <div class="card">
        <h2>Anular movimiento {{ anulando.folio }}</h2>
        <p class="texto-suave">
          {{ anulando.tipoNombre }} de {{ anulando.cantidad }} {{ anulando.unidad }} —
          {{ anulando.productoNombre }}. Se registrará un movimiento de reversión; el original no se modifica.
        </p>
        <label class="campo">
          <span>Motivo de la anulación *</span>
          <input type="text" maxlength="300" [(ngModel)]="motivoAnulacion" />
        </label>
        @if (errorAnulacion) {
          <div class="alerta alerta-error">{{ errorAnulacion.mensaje }}</div>
        }
        <div class="acciones">
          <button type="button" class="btn btn-peligro" [disabled]="ocupadoAnulacion" (click)="confirmarAnulacion()">
            {{ ocupadoAnulacion ? 'Anulando…' : 'Confirmar anulación' }}
          </button>
          <button type="button" class="btn btn-secundario" (click)="cerrarAnulacion()">Cancelar</button>
        </div>
      </div>
    }

    <div class="card">
      @if (movimientos.length === 0) {
        <div class="estado-vacio">
          <p><strong>No hay movimientos con los filtros seleccionados.</strong></p>
          <p class="texto-suave">Registre entradas, entregas o mermas desde «Nuevo movimiento».</p>
        </div>
      } @else {
        <div class="tabla-scroll">
          <table class="tabla">
            <thead>
              <tr>
                <th>Folio</th>
                <th>Tipo</th>
                <th>Producto</th>
                <th class="derecha">Cantidad</th>
                <th>Usuario</th>
                <th>Fecha (hora local)</th>
                <th>Detalle</th>
                @if (auth.esAdministrador) {
                  <th></th>
                }
              </tr>
            </thead>
            <tbody>
              @for (mov of movimientos; track mov.id) {
                <tr [class.anulado]="mov.anulado">
                  <td class="mono">{{ mov.folio }}</td>
                  <td>
                    <span class="badge" [class]="'badge-tipo-' + tipoClase(mov)">{{ mov.tipoNombre }}</span>
                  </td>
                  <td>
                    {{ mov.productoNombre }}
                    <br /><small class="mono texto-suave">{{ mov.sku }}</small>
                  </td>
                  <td class="derecha">
                    {{ mov.tipo === 1 ? '+' : mov.tipo === 4 ? '' : '-' }}{{ mov.cantidad }}
                    <span class="texto-suave"> {{ mov.unidad }}</span>
                  </td>
                  <td>{{ mov.usuarioNombre }}</td>
                  <td>{{ fecha(mov.fechaUtc) }}</td>
                  <td class="detalle">
                    @if (mov.proveedor) { <div>Proveedor: {{ mov.proveedor }}</div> }
                    @if (mov.referencia) { <div>Ref.: {{ mov.referencia }}</div> }
                    @if (mov.destino) { <div>Destino: {{ mov.destino }}</div> }
                    @if (mov.motivo) { <div>Motivo: {{ mov.motivo }}</div> }
                    @if (mov.folioOriginal) { <div class="texto-suave">Original: {{ mov.folioOriginal }}</div> }
                    @if (mov.anulado) {
                      <div><span class="badge badge-off">Anulado</span></div>
                    }
                  </td>
                  @if (auth.esAdministrador) {
                    <td class="acciones-fila">
                      @if (!mov.anulado && mov.tipo !== 4) {
                        <button type="button" class="btn btn-peligro btn-sm" (click)="abrirAnulacion(mov)">Anular</button>
                      }
                    </td>
                  }
                </tr>
              }
            </tbody>
          </table>
        </div>

        <div class="paginacion">
          <button type="button" class="btn btn-secundario btn-sm" [disabled]="pagina <= 1" (click)="irPagina(pagina - 1)">
            Anterior
          </button>
          <span>Página {{ pagina }} de {{ totalPaginas }} ({{ total }} movimientos)</span>
          <button type="button" class="btn btn-secundario btn-sm" [disabled]="pagina >= totalPaginas" (click)="irPagina(pagina + 1)">
            Siguiente
          </button>
        </div>
      }
    </div>
  `,
})
export class HistorialComponent {
  private readonly fb = inject(FormBuilder);

  movimientos: Movimiento[] = [];
  usuarios: Usuario[] = [];
  pagina = 1;
  total = 0;
  tamano = 20;
  error: ErrorApi | null = null;

  anulando: Movimiento | null = null;
  motivoAnulacion = '';
  ocupadoAnulacion = false;
  errorAnulacion: ErrorApi | null = null;

  readonly filtro = this.fb.nonNullable.group({
    desde: [restarDiasLocal(30)],
    hasta: [hoyLocal()],
    tipo: [''],
    usuarioId: [''],
  });

  constructor(
    private readonly api: ApiService,
    readonly auth: AuthService
  ) {
    this.filtro.valueChanges.subscribe(() => {
      this.pagina = 1;
      this.cargar();
    });
    if (auth.esAdministrador) {
      this.api.getUsuarios().subscribe((usuarios) => (this.usuarios = usuarios));
    }
    this.cargar();
  }

  get totalPaginas(): number {
    return Math.max(1, Math.ceil(this.total / this.tamano));
  }

  fecha(fechaUtc: string): string {
    return formatoLocal(fechaUtc);
  }

  tipoClase(mov: Movimiento): string {
    return mov.tipo === 1
      ? 'entrada'
      : mov.tipo === 2
        ? 'entrega'
        : mov.tipo === 3
          ? 'merma'
          : 'reversion';
  }

  cargar(): void {
    const { desde, hasta, tipo, usuarioId } = this.filtro.getRawValue();
    this.error = null;
    this.api
      .getMovimientos({
        desde: desde || undefined,
        hasta: hasta || undefined,
        tipo: tipo ? Number(tipo) : undefined,
        usuarioId: usuarioId ? Number(usuarioId) : undefined,
        pagina: this.pagina,
      })
      .subscribe({
        next: (respuesta) => {
          this.movimientos = respuesta.elementos;
          this.total = respuesta.total;
          this.tamano = respuesta.tamano;
          this.pagina = respuesta.pagina;
        },
        error: (err: ErrorApi) => (this.error = err),
      });
  }

  limpiarFiltros(): void {
    this.filtro.setValue({ desde: '', hasta: '', tipo: '', usuarioId: '' });
  }

  irPagina(pagina: number): void {
    this.pagina = pagina;
    this.cargar();
  }

  abrirAnulacion(mov: Movimiento): void {
    this.anulando = mov;
    this.motivoAnulacion = '';
    this.errorAnulacion = null;
  }

  cerrarAnulacion(): void {
    this.anulando = null;
    this.motivoAnulacion = '';
  }

  confirmarAnulacion(): void {
    if (!this.anulando) {
      return;
    }
    if (!this.motivoAnulacion.trim()) {
      this.errorAnulacion = {
        codigo: 'DATOS_INVALIDOS',
        mensaje: 'El motivo de anulación es obligatorio.',
      };
      return;
    }

    this.ocupadoAnulacion = true;
    this.errorAnulacion = null;

    this.api.anularMovimiento(this.anulando.id, this.motivoAnulacion.trim()).subscribe({
      next: (resultado) => {
        this.ocupadoAnulacion = false;
        this.anulando = null;
        this.motivoAnulacion = '';
        this.cargar();
        alert(`Anulación registrada con folio ${resultado.folio}.`);
      },
      error: (err: ErrorApi) => {
        this.ocupadoAnulacion = false;
        this.errorAnulacion = err;
      },
    });
  }
}
