import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { formatoLocal, hoyLocal, restarDiasLocal } from '../../core/fecha.util';
import { Dashboard, ErrorApi } from '../../core/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="pagina-cabecera">
      <h1>Dashboard</h1>
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
      <div class="acciones">
        <button type="button" class="btn btn-secundario" (click)="limpiar()">Todo el historial</button>
      </div>
      @if (dashboard) {
        <p class="texto-suave">
          Hora local del establecimiento ({{ dashboard.zonaHoraria }}): <strong>{{ dashboard.ahoraLocal }}</strong>
        </p>
      }
    </div>

    @if (error) {
      <div class="alerta alerta-error">{{ error.mensaje }}</div>
    }

    @if (dashboard) {
      <div class="tarjetas">
        <div class="card tarjeta">
          <span class="tarjeta-valor">{{ dashboard.totalExistencias }}</span>
          <span class="tarjeta-etiqueta">Existencias actuales (todos los productos)</span>
        </div>
        <div class="card tarjeta tarjeta-entrada">
          <span class="tarjeta-valor">+{{ dashboard.totales.entradas }}</span>
          <span class="tarjeta-etiqueta">Entradas netas del periodo</span>
        </div>
        <div class="card tarjeta tarjeta-entrega">
          <span class="tarjeta-valor">-{{ dashboard.totales.entregas }}</span>
          <span class="tarjeta-etiqueta">Entregas netas del periodo</span>
        </div>
        <div class="card tarjeta tarjeta-merma">
          <span class="tarjeta-valor">-{{ dashboard.totales.mermas }}</span>
          <span class="tarjeta-etiqueta">Mermas netas del periodo</span>
        </div>
      </div>

      <div class="card">
        <h2>Existencias por producto</h2>
        @if (dashboard.existencias.length === 0) {
          <div class="estado-vacio">
            <p><strong>No hay existencias registradas.</strong></p>
            <p class="texto-suave">Cargue el inventario inicial como una entrada con referencia «saldo inicial».</p>
          </div>
        } @else {
          <div class="tabla-scroll">
            <table class="tabla">
              <thead>
                <tr>
                  <th>SKU</th>
                  <th>Producto</th>
                  <th class="derecha">Existencia</th>
                  <th>Estado</th>
                </tr>
              </thead>
              <tbody>
                @for (item of dashboard.existencias; track item.productoId) {
                  <tr [class.fila-inactiva]="!item.activo">
                    <td class="mono">{{ item.sku }}</td>
                    <td>{{ item.nombre }}</td>
                    <td class="derecha"><strong>{{ item.existencia }}</strong> {{ item.unidad }}</td>
                    <td>
                      <span class="badge" [class.badge-ok]="item.activo" [class.badge-off]="!item.activo">
                        {{ item.activo ? 'Activo' : 'Inactivo' }}
                      </span>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </div>
    }
  `,
})
export class DashboardComponent {
  private readonly fb = inject(FormBuilder);

  dashboard: Dashboard | null = null;
  error: ErrorApi | null = null;

  readonly filtro = this.fb.nonNullable.group({
    desde: [restarDiasLocal(30)],
    hasta: [hoyLocal()],
  });

  constructor(private readonly api: ApiService) {
    this.filtro.valueChanges.subscribe(() => this.cargar());
    this.cargar();
  }

  cargar(): void {
    const { desde, hasta } = this.filtro.getRawValue();
    this.error = null;
    this.api.getDashboard(desde || undefined, hasta || undefined).subscribe({
      next: (dashboard) => (this.dashboard = dashboard),
      error: (err: ErrorApi) => (this.error = err),
    });
  }

  limpiar(): void {
    this.filtro.setValue({ desde: '', hasta: '' });
  }
}
