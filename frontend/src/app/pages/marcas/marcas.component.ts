import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { ErrorApi, Marca } from '../../core/models';

@Component({
  selector: 'app-marcas',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="pagina-cabecera">
      <h1>Marcas</h1>
      <button type="button" class="btn btn-primario" (click)="abrirAlta()">+ Nueva marca</button>
    </div>

    @if (error) {
      <div class="alerta alerta-error">{{ error.mensaje }}</div>
    }

    @if (visible) {
      <div class="card">
        <h2>{{ idEdicion ? 'Editar marca' : 'Nueva marca' }}</h2>
        <form [formGroup]="form" (ngSubmit)="guardar()" class="fila-form">
          <label class="campo crece">
            <span>Nombre *</span>
            <input type="text" formControlName="nombre" maxlength="80" />
            @if (form.get('nombre')?.invalid && (form.get('nombre')?.dirty || form.get('nombre')?.touched)) {
              <small class="error">El nombre es obligatorio.</small>
            }
          </label>
          <div class="acciones">
            <button type="submit" class="btn btn-primario" [disabled]="ocupado">Guardar</button>
            <button type="button" class="btn btn-secundario" (click)="cerrar()">Cancelar</button>
          </div>
        </form>
        @if (errorForm) {
          <div class="alerta alerta-error">{{ errorForm.mensaje }}</div>
        }
      </div>
    }

    <div class="card">
      @if (marcas.length === 0) {
        <div class="estado-vacio">
          <p><strong>No hay marcas registradas.</strong></p>
        </div>
      } @else {
        <div class="tabla-scroll">
          <table class="tabla">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (marca of marcas; track marca.id) {
                <tr [class.fila-inactiva]="!marca.activo">
                  <td>{{ marca.nombre }}</td>
                  <td>
                    <span class="badge" [class.badge-ok]="marca.activo" [class.badge-off]="!marca.activo">
                      {{ marca.activo ? 'Activa' : 'Inactiva' }}
                    </span>
                  </td>
                  <td class="acciones-fila">
                    <button type="button" class="btn btn-secundario btn-sm" (click)="abrirEdicion(marca)">Editar</button>
                    <button type="button" class="btn btn-peligro btn-sm" (click)="cambiarEstado(marca)">
                      {{ marca.activo ? 'Desactivar' : 'Activar' }}
                    </button>
                  </td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class MarcasComponent {
  private readonly fb = inject(FormBuilder);

  marcas: Marca[] = [];
  visible = false;
  idEdicion: number | null = null;
  ocupado = false;
  error: ErrorApi | null = null;
  errorForm: ErrorApi | null = null;

  readonly form = this.fb.nonNullable.group({ nombre: ['', Validators.required] });

  constructor(private readonly api: ApiService) {
    this.cargar();
  }

  cargar(): void {
    this.api.getMarcas().subscribe({
      next: (marcas) => (this.marcas = marcas),
      error: (err: ErrorApi) => (this.error = err),
    });
  }

  abrirAlta(): void {
    this.visible = true;
    this.idEdicion = null;
    this.errorForm = null;
    this.form.reset({ nombre: '' });
  }

  abrirEdicion(marca: Marca): void {
    this.visible = true;
    this.idEdicion = marca.id;
    this.errorForm = null;
    this.form.setValue({ nombre: marca.nombre });
  }

  cerrar(): void {
    this.visible = false;
    this.idEdicion = null;
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    const nombre = this.form.getRawValue().nombre.trim();
    this.ocupado = true;
    this.errorForm = null;

    const peticion =
      this.idEdicion === null
        ? this.api.crearMarca(nombre)
        : this.api.editarMarca(this.idEdicion, nombre);

    peticion.subscribe({
      next: () => {
        this.ocupado = false;
        this.cerrar();
        this.cargar();
      },
      error: (err: ErrorApi) => {
        this.ocupado = false;
        this.errorForm = err;
      },
    });
  }

  cambiarEstado(marca: Marca): void {
    this.api.estadoMarca(marca.id, !marca.activo).subscribe({
      next: () => this.cargar(),
      error: (err: ErrorApi) => (this.error = err),
    });
  }
}
