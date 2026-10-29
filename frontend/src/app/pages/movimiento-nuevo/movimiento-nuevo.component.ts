import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { ErrorApi, Producto, ResultadoMovimiento } from '../../core/models';

type TipoMovimiento = 'entradas' | 'entregas' | 'mermas';

@Component({
  selector: 'app-movimiento-nuevo',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="pagina-cabecera">
      <h1>Nuevo movimiento</h1>
    </div>

    <div class="card">
      <form [formGroup]="form" (ngSubmit)="enviar()">
        <fieldset class="tipo-selector">
          <legend>Tipo de movimiento *</legend>
          <label class="opcion" [class.seleccionada]="tipo === 'entradas'">
            <input type="radio" formControlName="tipo" value="entradas" (change)="cambiarTipo('entradas')" />
            Entrada
          </label>
          <label class="opcion" [class.seleccionada]="tipo === 'entregas'">
            <input type="radio" formControlName="tipo" value="entregas" (change)="cambiarTipo('entregas')" />
            Entrega a barra
          </label>
          <label class="opcion" [class.seleccionada]="tipo === 'mermas'">
            <input type="radio" formControlName="tipo" value="mermas" (change)="cambiarTipo('mermas')" />
            Merma
          </label>
        </fieldset>

        <div class="grid-form">
          <label class="campo">
            <span>Producto *</span>
            <select formControlName="productoId">
              <option value="">Seleccione…</option>
              @for (producto of productosActivos; track producto.id) {
                <option [value]="producto.id">
                  {{ producto.sku }} — {{ producto.nombre }} (existencia: {{ producto.existencia }} {{ producto.unidad }})
                </option>
              }
            </select>
            @if (campoError('productoId')) {
              <small class="error">{{ campoError('productoId') }}</small>
            }
          </label>

          <label class="campo">
            <span>Cantidad *</span>
            <input type="number" formControlName="cantidad" min="1" step="1" inputmode="numeric" />
            @if (campoError('cantidad')) {
              <small class="error">{{ campoError('cantidad') }}</small>
            } @else {
              <small class="ayuda">Solo cantidades enteras positivas.</small>
            }
          </label>

          @if (tipo === 'entradas') {
            <label class="campo">
              <span>Proveedor *</span>
              <input type="text" formControlName="proveedor" maxlength="120" />
              @if (campoError('proveedor')) {
                <small class="error">{{ campoError('proveedor') }}</small>
              }
            </label>
            <label class="campo">
              <span>Referencia</span>
              <input type="text" formControlName="referencia" maxlength="120" placeholder="Folio de compra, OC…" />
            </label>
          }

          @if (tipo === 'entregas') {
            <label class="campo">
              <span>Destino *</span>
              <input type="text" formControlName="destino" maxlength="120" placeholder="Barra principal, etc." />
              @if (campoError('destino')) {
                <small class="error">{{ campoError('destino') }}</small>
              }
            </label>
          }

          @if (tipo === 'mermas') {
            <label class="campo">
              <span>Motivo *</span>
              <input type="text" formControlName="motivo" maxlength="300" />
              @if (campoError('motivo')) {
                <small class="error">{{ campoError('motivo') }}</small>
              }
            </label>
          }
        </div>

        @if (error) {
          <div class="alerta alerta-error" role="alert">{{ error.mensaje }}</div>
        }

        <div class="acciones">
          <button type="submit" class="btn btn-primario" [disabled]="ocupado">
            {{ ocupado ? 'Registrando…' : 'Registrar movimiento' }}
          </button>
          <button type="button" class="btn btn-secundario" (click)="limpiar()">Limpiar</button>
        </div>
      </form>
    </div>

    @if (resultado) {
      <div class="card confirmacion" role="status">
        <h2>Movimiento registrado</h2>
        <p class="folio">{{ resultado.folio }}</p>
        <p>
          {{ resultado.tipoNombre }} · efecto {{ resultado.efecto > 0 ? '+' : '' }}{{ resultado.efecto }}
          @if (resultado.existenciaResultante !== null && resultado.existenciaResultante !== undefined) {
            · existencia resultante: <strong>{{ resultado.existenciaResultante }}</strong>
          }
        </p>
        <button type="button" class="btn btn-secundario" (click)="resultado = null">Registrar otro</button>
      </div>
    }
  `,
})
export class MovimientoNuevoComponent {
  private readonly fb = inject(FormBuilder);

  productos: Producto[] = [];
  tipo: TipoMovimiento = 'entradas';
  ocupado = false;
  error: ErrorApi | null = null;
  resultado: ResultadoMovimiento | null = null;
  requestId = crypto.randomUUID();

  readonly form = this.fb.nonNullable.group({
    tipo: ['entradas', Validators.required],
    productoId: ['', Validators.required],
    cantidad: [null as number | null, [Validators.required, Validators.min(1)]],
    proveedor: [''],
    referencia: [''],
    destino: [''],
    motivo: [''],
  });

  constructor(private readonly api: ApiService) {
    this.api.getProductos({ activo: true }).subscribe((productos) => (this.productos = productos));
  }

  get productosActivos(): Producto[] {
    return this.productos.filter((p) => p.activo);
  }

  cambiarTipo(tipo: TipoMovimiento): void {
    this.tipo = tipo;
    this.error = null;
    this.form.patchValue({ proveedor: '', destino: '', motivo: '', referencia: '' });
  }

  campoError(campo: string): string | null {
    const control = this.form.get(campo);
    if (!control || !control.invalid || !(control.dirty || control.touched)) {
      return null;
    }
    if (control.errors?.['required']) {
      return 'Este campo es obligatorio.';
    }
    if (control.errors?.['min']) {
      return 'La cantidad debe ser al menos 1.';
    }
    return 'Valor no válido.';
  }

  enviar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const valor = this.form.getRawValue();
    const producto = this.productos.find((p) => p.id === Number(valor.productoId));
    const cantidad = Number(valor.cantidad);

    if (producto && this.tipo !== 'entradas' && cantidad > producto.existencia) {
      this.error = {
        codigo: 'SALDO_INSUFICIENTE',
        mensaje: `Saldo insuficiente: la existencia actual es ${producto.existencia} ${producto.unidad}.`,
      };
      return;
    }

    this.ocupado = true;
    this.error = null;

    // El mismo requestId se reutiliza en reintentos para no duplicar el movimiento.
    this.api
      .registrarMovimiento(this.tipo, {
        productoId: Number(valor.productoId),
        cantidad,
        proveedor: valor.proveedor?.trim() || undefined,
        referencia: valor.referencia?.trim() || undefined,
        destino: valor.destino?.trim() || undefined,
        motivo: valor.motivo?.trim() || undefined,
        requestId: this.requestId,
      })
      .subscribe({
        next: (resultado) => {
          this.ocupado = false;
          this.resultado = resultado;
          this.requestId = crypto.randomUUID();
          this.api.getProductos({ activo: true }).subscribe((productos) => (this.productos = productos));
        },
        error: (err: ErrorApi) => {
          // Los datos capturados se conservan ante el error (IU-02)
          this.ocupado = false;
          this.error = err;
        },
      });
  }

  limpiar(): void {
    this.form.patchValue({
      productoId: '',
      cantidad: null,
      proveedor: '',
      referencia: '',
      destino: '',
      motivo: '',
    });
    this.error = null;
    this.requestId = crypto.randomUUID();
  }
}
