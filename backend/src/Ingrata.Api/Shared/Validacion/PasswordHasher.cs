using System.Security.Cryptography;

namespace Ingrata.Api.Shared.Validacion;

/// <summary>PBKDF2 con sal individual por usuario (RNF-04).</summary>
public static class PasswordHasher
{
    private const string Prefijo = "PBKDF2";
    private const int Iteraciones = 100_000;
    private const int TamanoSal = 16;
    private const int TamanoClave = 32;

    public static string Hash(string password)
    {
        var sal = RandomNumberGenerator.GetBytes(TamanoSal);
        var clave = Rfc2898DeriveBytes.Pbkdf2(password, sal, Iteraciones,
            HashAlgorithmName.SHA256, TamanoClave);
        return $"{Prefijo}|{Iteraciones}|{Convert.ToBase64String(sal)}|{Convert.ToBase64String(clave)}";
    }

    public static bool Verify(string password, string almacenado)
    {
        if (string.IsNullOrWhiteSpace(almacenado))
            return false;

        var partes = almacenado.Split('|');
        if (partes.Length != 4 || partes[0] != Prefijo)
            return false;

        if (!int.TryParse(partes[1], out var iteraciones))
            return false;

        byte[] sal, esperada;
        try
        {
            sal = Convert.FromBase64String(partes[2]);
            esperada = Convert.FromBase64String(partes[3]);
        }
        catch (FormatException)
        {
            return false;
        }

        var obtenida = Rfc2898DeriveBytes.Pbkdf2(password, sal, iteraciones,
            HashAlgorithmName.SHA256, esperada.Length);
        return CryptographicOperations.FixedTimeEquals(esperada, obtenida);
    }
}
