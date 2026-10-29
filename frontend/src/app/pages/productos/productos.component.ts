import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Subscription } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';
import { Categoria, ErrorApi, Marca, Producto } from '../../core/models';

@Component({
  selector: 'app-productos',
  standalone: true,
  imports: [ReactiveFormsModule],
  template: `
    <div class="pagina-cabecera">
      <h1>Productos</h1>
      @if (auth.esAdministrador) {
        <button type="button" class="btn btn-primario" (click)="abrirAlta()">
          + Nuevo producto
        </button>
      }
    </div>

    <div class="card filtros">
      <label class="campo">
        <span>Buscar (SKU o nombre)</span>
        <input type="search" [formGroup]="filtro" formControlName="texto" placeholder="Ej. CER-COR-355" />
      </label>
      <label class="campo">
        <span>Marca</span>
        <select [formGroup]="filtro" formControlName="marcaId">
          <option value="">Todas</option>
          @for (marca of marcas; track marca.id) {
            <option [value]="marca.id">{{ marca.nombre }}</option>
          }
        </select>
      </label>
      <label class="campo">
        <span>Categoría</span>
        <select [formGroup]="filtro" formControlName="categoriaId">
          <option value="">Todas</option>
          @for (categoria of categorias; track categoria.id) {
            <option [value]="categoria.id">{{ categoria.nombre }}</option>
          }
        </select>
      </label>
      <label class="campo">
        <span>Estado</span>
        <select [formGroup]="filtro" formControlName="activo">
          <option value="">Todos</option>
          <option value="true">Activos</option>
          <option value="false">Inactivos</option>
        </select>
      </label>
    </div>

    @if (error) {
      <div class="alerta alerta-error">{{ error.mensaje }}</div>
    }

    @if (formulario.visible) {
      <div class="card">
        <h2>{{ formulario.id ? 'Editar producto' : 'Nuevo producto' }}</h2>
        <form [formGroup]="form" (ngSubmit)="guardar()" class="grid-form">
          <label class="campo">
            <span>SKU *</span>
            <input type="text" formControlName="sku" maxlength="40" />
            @if (campoError('sku')) {
              <small class="error">{{ campoError('sku') }}</small>
            }
          </label>
          <label class="campo">
            <span>Nombre *</span>
            <input type="text" formControlName="nombre" maxlength="120" />
            @if (campoError('nombre')) {
              <small class="error">{{ campoError('nombre') }}</small>
            }
          </label>
          <label class="campo">
            <span>Marca *</span>
            <select formControlName="marcaId">
              <option value="">Seleccione…</option>
              @for (marca of marcasActivas; track marca.id) {
                <option [value]="marca.id">{{ marca.nombre }}</option>
              }
            </select>
            @if (campoError('marcaId')) {
              <small class="error">{{ campoError('marcaId') }}</small>
            }
          </label>
          <label class="campo">
            <span>Categoría *</span>
            <select formControlName="categoriaId">
              <option value="">Seleccione…</option>
              @for (categoria of categoriasActivas; track categoria.id) {
                <option [value]="categoria.id">{{ categoria.nombre }}</option>
              }
            </select>
            @if (campoError('categoriaId')) {
              <small class="error">{{ campoError('categoriaId') }}</small>
            }
          </label>
          <label class="campo">
            <span>Unidad *</span>
            <input type="text" formControlName="unidad" maxlength="20" placeholder="botella, pieza, caja…" />
            @if (campoError('unidad')) {
              <small class="error">{{ campoError('unidad') }}</small>
            }
            @if (formulario.id) {
              <small class="ayuda">Con movimientos registrados la unidad no puede cambiarse.</small>
            }
          </label>

          @if (errorForm) {
            <div class="alerta alerta-error" role="alert">{{ errorForm.mensaje }}</div>
          }

          <div class="acciones">
            <button type="submit" class="btn btn-primario" [disabled]="ocupado">
              {{ ocupado ? 'Guardando…' : 'Guardar' }}
            </button>
            <button type="button" class="btn btn-secundario" (click)="cerrarFormulario()">Cancelar</button>
          </div>
        </form>
      </div>
    }

    <div class="card">
      @if (cargando) {
        <p class="texto-suave">Cargando productos…</p>
      } @else if (productos.length === 0) {
        <div class="estado-vacio">
          <p><strong>No hay productos que coincidan con la búsqueda.</strong></p>
          <p class="texto-suave">Ajuste los filtros o registre un producto nuevo.</p>
        </div>
      } @else {
        <div class="tabla-scroll">
          <table class="tabla">
            <thead>
              <tr>
                <th>SKU</th>
                <th>Nombre</th>
                <th>Marca</th>
                <th>Categoría</th>
                <th class="derecha">Existencia</th>
                <th>Estado</th>
                @if (auth.esAdministrador) {
                  <th></th>
                }
              </tr>
            </thead>
            <tbody>
              @for (producto of productos; track producto.id) {
                <tr [class.fila-inactiva]="!producto.activo">
                  <td class="mono">{{ producto.sku }}</td>
                  <td>{{ producto.nombre }}</td>
                  <td>{{ producto.marca }}</td>
                  <td>{{ producto.categoria }}</td>
                  <td class="derecha">
                    <strong>{{ producto.existencia }}</strong>
                    <span class="texto-suave"> {{ producto.unidad }}</span>
                  </td>
                  <td>
                    <span class="badge" [class.badge-ok]="producto.activo" [class.badge-off]="!producto.activo">
                      {{ producto.activo ? 'Activo' : 'Inactivo' }}
                    </span>
                  </td>
                  @if (auth.esAdministrador) {
                    <td class="acciones-fila">
                      <button type="button" class="btn btn-secundario btn-sm" (click)="abrirEdicion(producto)">Editar</button>
                      <button type="button" class="btn btn-peligro btn-sm" (click)="cambiarEstado(producto)">
                        {{ producto.activo ? 'Desactivar' : 'Activar' }}
                      </button>
                    </td>
                  }
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </div>
  `,
})
export class ProductosComponent {
  private readonly fb = inject(FormBuilder);

  productos: Producto[] = [];
  marcas: Marca[] = [];
  categorias: Categoria[] = [];
  cargando = false;
  ocupado = false;
  error: ErrorApi | null = null;
  errorForm: ErrorApi | null = null;

  readonly filtro = this.fb.nonNullable.group({
    texto: [''],
    marcaId: [''],
    categoriaId: [''],
    activo: [''],
  });

  readonly form = this.fb.nonNullable.group({
    sku: ['', Validators.required],
    nombre: ['', Validators.required],
    marcaId: ['', Validators.required],
    categoriaId: ['', Validators.required],
    unidad: ['', Validators.required],
  });

  formulario: { visible: boolean; id: number | null } = { visible: false, id: null };

  private suscripcion?: Subscription;

  constructor(
    private readonly api: ApiService,
    readonly auth: AuthService
  ) {
    this.suscripcion = this.filtro.valueChanges.subscribe(() => this.cargar());
  }

  get marcasActivas(): Marca[] {
    return this.marcas.filter((m) => m.activo);
  }

  get categoriasActivas(): Categoria[] {
    return this.categorias.filter((c) => c.activo);
  }

  ngOnInit(): void {
    this.api.getMarcas().subscribe((marcas) => (this.marcas = marcas));
    this.api.getCategorias().subscribe((categorias) => (this.categorias = categorias));
    this.cargar();
  }

  ngOnDestroy(): void {
    this.suscripcion?.unsubscribe();
  }

  cargar(): void {
    const { texto, marcaId, categoriaId, activo } = this.filtro.getRawValue();
    this.cargando = true;
    this.error = null;
    this.api
      .getProductos({
        texto: texto.trim() || undefined,
        marcaId: marcaId ? Number(marcaId) : undefined,
        categoriaId: categoriaId ? Number(categoriaId) : undefined,
        activo: activo === '' ? undefined : activo === 'true',
      })
      .subscribe({
        next: (productos) => {
          this.productos = productos;
          this.cargando = false;
        },
        error: (err: ErrorApi) => {
          this.error = err;
          this.cargando = false;
        },
      });
  }

  abrirAlta(): void {
    this.formulario = { visible: true, id: null };
    this.errorForm = null;
    this.form.reset({ sku: '', nombre: '', marcaId: '', categoriaId: '', unidad: '' });
  }

  abrirEdicion(producto: Producto): void {
    this.formulario = { visible: true, id: producto.id };
    this.errorForm = null;
    this.form.setValue({
      sku: producto.sku,
      nombre: producto.nombre,
      marcaId: String(producto.marcaId),
      categoriaId: String(producto.categoriaId),
      unidad: producto.unidad,
    });
  }

  cerrarFormulario(): void {
    this.formulario = { visible: false, id: null };
    this.errorForm = null;
  }

  campoError(campo: string): string | null {
    const control = this.form.get(campo);
    if (!control || !control.invalid || !(control.dirty || control.touched)) {
      return null;
    }
    if (control.errors?.['required']) {
      return 'Este campo es obligatorio.';
    }
    return 'Valor no válido.';
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const valor = this.form.getRawValue();
    const cuerpo = {
      sku: valor.sku.trim(),
      nombre: valor.nombre.trim(),
      marcaId: Number(valor.marcaId),
      categoriaId: Number(valor.categoriaId),
      unidad: valor.unidad.trim(),
    };

    this.ocupado = true;
    this.errorForm = null;

    const peticion = this.formulario.id
      ? this.api.editarProducto(this.formulario.id, cuerpo)
      : this.api.crearProducto(cuerpo);

    peticion.subscribe({
      next: () => {
        this.ocupado = false;
        this.cerrarFormulario();
        this.cargar();
      },
      error: (err: ErrorApi) => {
        // Los datos capturados se conservan ante un error (IU-02)
        this.ocupado = false;
        this.errorForm = err;
      },
    });
  }

  cambiarEstado(producto: Producto): void {
    this.api.estadoProducto(producto.id, !producto.activo).subscribe({
      next: () => this.cargar(),
      error: (err: ErrorApi) => (this.error = err),
    });
  }
}
