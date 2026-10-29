import { environment } from '../../environments/environment';

const formatoFechaHora = new Intl.DateTimeFormat('es-MX', {
  timeZone: environment.timeZone,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
  second: '2-digit',
  hour12: false,
});

const formatoFecha = new Intl.DateTimeFormat('es-MX', {
  timeZone: environment.timeZone,
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
});

/** Convierte una fecha UTC (ISO) a hora local del establecimiento con Intl.DateTimeFormat. */
export function formatoLocal(fechaUtc: string | Date | null | undefined): string {
  if (!fechaUtc) {
    return '—';
  }
  const fecha = typeof fechaUtc === 'string' ? new Date(fechaUtc) : fechaUtc;
  if (Number.isNaN(fecha.getTime())) {
    return '—';
  }
  return formatoFechaHora.format(fecha);
}

export function formatoSoloFecha(fechaUtc: string | Date | null | undefined): string {
  if (!fechaUtc) {
    return '—';
  }
  const fecha = typeof fechaUtc === 'string' ? new Date(fechaUtc) : fechaUtc;
  if (Number.isNaN(fecha.getTime())) {
    return '—';
  }
  return formatoFecha.format(fecha);
}

/** Fecha local en formato YYYY-MM-DD (para inputs date). */
export function hoyLocal(): string {
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: environment.timeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(new Date());
}

export function restarDiasLocal(dias: number): string {
  const fecha = new Date();
  fecha.setDate(fecha.getDate() - dias);
  return new Intl.DateTimeFormat('en-CA', {
    timeZone: environment.timeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
  }).format(fecha);
}
