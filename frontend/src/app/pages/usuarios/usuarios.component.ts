import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { ErrorApi, Usuario } from '../../core/models';

@Component({
  selector: 'app-usuarios',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="pagina-cabecera">
      <h1>Usuarios</h1>
      <button type="button" class="btn btn-primario" (click)="abrirAlta()">+ Nuevo usuario</button>
    </div>

    @if (error) {
      <div class="alerta alerta-error">{{ error.mensaje }}</div>
    }

    @if (visible) {
      <div class="card">
        <h2>{{ idEdicion ? 'Editar usuario' : 'Nuevo usuario' }}</h2>
        <form [formGroup]="form" (ngSubmit)="guardar()" class="grid-form">
          <label class="campo">
            <span>Identificador *</span>
            <input type="text" formControlName="identificador" maxlength="50" [readonly]="idEdicion !== null" />
            @if (idEdicion !== null) {
              <small class="ayuda">El identificador no puede modificarse.</small>
            }
            @if (campoError('identificador')) {
              <small class="error">{{ campoError('identificador') }}</small>
            }
          </label>
          <label class="campo">
            <span>Nombre completo *</span>
            <input type="text" formControlName="nombreCompleto" maxlength="120" />
            @if (campoError('nombreCompleto')) {
              <small class="error">{{ campoError('nombreCompleto') }}</small>
            }
          </label>
          <label class="campo">
            <span>{{ idEdicion ? 'Nueva contraseña (opcional)' : 'Contraseña *' }}</span>
            <input type="password" formControlName="password" [required]="idEdicion === null" minlength="8" autocomplete="new-password" />
            @if (campoError('password')) {
              <small class="error">{{ campoError('password') }}</small>
            }
          </label>
          <label class="campo">
            <span>Rol *</span>
            <select formControlName="rolId">
              <option value="">Seleccione…</option>
              <option [value]="1">Administrador</option>
              <option [value]="2">Almacenista</option>
            </select>
            @if (campoError('rolId')) {
              <small class="error">{{ campoError('rolId') }}</small>
            }
          </label>

          @if (errorForm) {
            <div class="alerta alerta-error">{{ errorForm.mensaje }}</div>
          }

          <div class="acciones">
            <button type="submit" class="btn btn-primario" [disabled]="ocupado">Guardar</button>
            <button type="button" class="btn btn-secundario" (click)="cerrar()">Cancelar</button>
          </div>
        </form>
      </div>
    }

    <div class="card">
      @if (usuarios.length === 0) {
        <div class="estado-vacio">
          <p><strong>No hay usuarios registrados.</strong></p>
        </div>
      } @else {
        <div class="tabla-scroll">
          <table class="tabla">
            <thead>
              <tr>
                <th>Identificador</th>
                <th>Nombre</th>
                <th>Rol</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              @for (usuario of usuarios; track usuario.id) {
                <tr [class.fila-inactiva]="!usuario.activo">
                  <td class="mono">{{ usuario.identificador }}</td>
                  <td>{{ usuario.nombreCompleto }}</td>
                  <td>{{ usuario.rol }}</td>
                  <td>
                    <span class="badge" [class.badge-ok]="usuario.activo" [class.badge-off]="!usuario.activo">
                      {{ usuario.activo ? 'Activo' : 'Inactivo' }}
                    </span>
                  </td>
                  <td class="acciones-fila">
                    <button type="button" class="btn btn-secundario btn-sm" (click)="abrirEdicion(usuario)">Editar</button>
                    @if (usuario.id !== auth.usuario?.id) {
                      <button type="button" class="btn btn-peligro btn-sm" (click)="cambiarEstado(usuario)">
                        {{ usuario.activo ? 'Desactivar' : 'Activar' }}
                      </button>
                    }
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
export class UsuariosComponent {
  private readonly fb = inject(FormBuilder);

  usuarios: Usuario[] = [];
  visible = false;
  idEdicion: number | null = null;
  ocupado = false;
  error: ErrorApi | null = null;
  errorForm: ErrorApi | null = null;

  readonly form = this.fb.nonNullable.group({
    identificador: ['', Validators.required],
    nombreCompleto: ['', Validators.required],
    password: ['', idEdicionNulo()],
    rolId: ['', Validators.required],
  });

  constructor(
    private readonly api: ApiService,
    readonly auth: AuthService
  ) {
    this.cargar();
  }

  cargar(): void {
    this.api.getUsuarios().subscribe({
      next: (usuarios) => (this.usuarios = usuarios),
      error: (err: ErrorApi) => (this.error = err),
    });
  }

  abrirAlta(): void {
    this.visible = true;
    this.idEdicion = null;
    this.errorForm = null;
    this.form.reset({ identificador: '', nombreCompleto: '', password: '', rolId: '' });
    this.form.setValidators(null);
    this.form.get('password')?.setValidators([Validators.required, Validators.minLength(8)]);
    this.form.get('password')?.updateValueAndValidity();
    this.form.get('identificador')?.enable();
  }

  abrirEdicion(usuario: Usuario): void {
    this.visible = true;
    this.idEdicion = usuario.id;
    this.errorForm = null;
    this.form.setValue({
      identificador: usuario.identificador,
      nombreCompleto: usuario.nombreCompleto,
      password: '',
      rolId: String(usuario.rolId),
    });
    this.form.get('identificador')?.disable();
    this.form.get('password')?.setValidators([Validators.minLength(8)]);
    this.form.get('password')?.updateValueAndValidity();
  }

  cerrar(): void {
    this.visible = false;
    this.idEdicion = null;
  }

  campoError(campo: string): string | null {
    const control = this.form.get(campo);
    if (!control || !control.invalid || !(control.dirty || control.touched)) {
      return null;
    }
    if (control.errors?.['required']) {
      return 'Este campo es obligatorio.';
    }
    if (control.errors?.['minlength']) {
      return 'La contraseña debe tener al menos 8 caracteres.';
    }
    return 'Valor no válido.';
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const valor = this.form.getRawValue();
    this.ocupado = true;
    this.errorForm = null;

    if (this.idEdicion === null) {
      this.api
        .crearUsuario({
          identificador: valor.identificador.trim(),
          nombreCompleto: valor.nombreCompleto.trim(),
          password: valor.password,
          rolId: Number(valor.rolId),
        })
        .subscribe({
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
      return;
    }

    this.api
      .editarUsuario(this.idEdicion, {
        nombreCompleto: valor.nombreCompleto.trim(),
        password: valor.password ? valor.password : undefined,
        rolId: Number(valor.rolId),
      })
      .subscribe({
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

  cambiarEstado(usuario: Usuario): void {
    this.api.estadoUsuario(usuario.id, !usuario.activo).subscribe({
      next: () => this.cargar(),
      error: (err: ErrorApi) => (this.error = err),
    });
  }
}

function idEdicionNulo() {
  return [Validators.minLength(8)];
}
