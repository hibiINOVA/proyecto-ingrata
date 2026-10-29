namespace Ingrata.Tests;

public record LoginResponseJson(string Token, DateTime ExpiraUtc, UsuarioSesionJson Usuario);
public record UsuarioSesionJson(int Id, string Identificador, string NombreCompleto, int RolId, string Rol);

public record UsuarioJson(int Id, string Identificador, string NombreCompleto, int RolId,
    string Rol, bool Activo, DateTime CreadoUtc);

public record MarcaJson(int Id, string Nombre, bool Activo);
public record CategoriaJson(int Id, string Nombre, bool Activo);

public record ProductoJson(int Id, string Sku, string Nombre, int MarcaId, string Marca,
    int CategoriaId, string Categoria, string Unidad, bool Activo, int Existencia);

public record MovimientoResultadoJson(long? Id, string Folio, int Tipo, string TipoNombre,
    int Efecto, int? ExistenciaResultante, string Mensaje);

public record MovimientoJson(long Id, string Folio, int Tipo, string TipoNombre,
    int ProductoId, string Sku, string ProductoNombre, string Unidad, int Cantidad,
    int UsuarioId, string UsuarioNombre, DateTime FechaUtc, string? Proveedor,
    string? Referencia, string? Destino, string? Motivo, long? MovimientoOriginalId,
    string? FolioOriginal, bool Anulado);

public record RespuestaPaginadaJson<T>(int Pagina, int Tamano, long Total, List<T> Elementos);

public record DashboardJson(DateTime? Desde, DateTime? Hasta, int TotalExistencias,
    List<ExistenciaProductoJson> Existencias, TotalesJson Totales,
    DateTime AhoraUtc, DateTime AhoraLocal, string ZonaHoraria);

public record ExistenciaProductoJson(int ProductoId, string Sku, string Nombre,
    string Unidad, int Existencia, bool Activo);

public record TotalesJson(int Entradas, int Entregas, int Mermas);

public record ErrorJson(string Codigo, string Mensaje);
