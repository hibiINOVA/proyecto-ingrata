import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { ErrorApi } from '../../core/models';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="login-contenedor">
      <form class="card login-tarjeta" [formGroup]="formulario" (ngSubmit)="enviar()">
        <h1>Inventario Ingrata</h1>
        <p class="texto-suave">Inicie sesión para continuar</p>

        <label class="campo">
          <span>Identificador *</span>
          <input type="text" formControlName="identificador" autocomplete="username" />
          @if (mostrarError('identificador')) {
            <small class="error">El identificador es obligatorio.</small>
          }
        </label>

        <label class="campo">
          <span>Contraseña *</span>
          <input type="password" formControlName="password" autocomplete="current-password" />
          @if (mostrarError('password')) {
            <small class="error">La contraseña es obligatoria.</small>
          }
        </label>

        @if (error) {
          <div class="alerta alerta-error" role="alert">{{ error.mensaje }}</div>
        }

        <button type="submit" class="btn btn-primario btn-block" [disabled]="ocupado">
          {{ ocupado ? 'Ingresando…' : 'Ingresar' }}
        </button>
      </form>
    </div>
  `,
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);

  readonly formulario = this.fb.nonNullable.group({
    identificador: ['', Validators.required],
    password: ['', Validators.required],
  });
  ocupado = false;
  error: ErrorApi | null = null;

  constructor(
    private readonly auth: AuthService,
    private readonly router: Router
  ) {}

  mostrarError(campo: string): boolean {
    const control = this.formulario.get(campo);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  enviar(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }

    this.ocupado = true;
    this.error = null;

    const { identificador, password } = this.formulario.getRawValue();
    this.auth.login(identificador.trim(), password).subscribe({
      next: () => {
        this.ocupado = false;
        this.router.navigate(['/dashboard']);
      },
      error: (err: ErrorApi) => {
        this.ocupado = false;
        this.error = err;
      },
    });
  }
}
