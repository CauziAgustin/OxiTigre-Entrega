/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Security.PasswordHasher
Archivo: PasswordHasher.cs | Versión: 1.1.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Deriva y verifica contraseñas con PBKDF2-SHA512.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Mensaje controlado para contraseña vacía.
===============================================================================
*/
using System.Security.Cryptography;

namespace OxiTigre.BLL.Security;

/// <summary>Implementa el contrato criptográfico de contraseñas de la aplicación.</summary>
public sealed class PasswordHasher
{
    /// <summary>Nombre persistido del algoritmo vigente.</summary>
    public const string CurrentAlgorithm = "PBKDF2-SHA512";

    /// <summary>Cantidad de iteraciones utilizada para credenciales nuevas.</summary>
    public const int CurrentIterations = 210_000;

    /// <summary>Genera una credencial derivada con un salt aleatorio.</summary>
    /// <param name="password">Nueva contraseña que debe satisfacer la política de longitud y complejidad.</param>
    /// <returns>Hash PBKDF2-SHA512, salt aleatorio, algoritmo e iteraciones que se persistirán juntos.</returns>
    /// <exception cref="ArgumentException">La contraseña no cumple la política vigente.</exception>
    public PasswordHash Hash(string password)
    {
        ValidateNewPassword(password);
        var salt = RandomNumberGenerator.GetBytes(32);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            CurrentIterations,
            HashAlgorithmName.SHA512,
            64
        );
        return new PasswordHash(hash, salt, CurrentAlgorithm, CurrentIterations);
    }

    /// <summary>Compara una contraseña sin diferencias temporales observables.</summary>
    /// <param name="password">Contraseña presentada por quien intenta acceder.</param>
    /// <param name="account">Cuenta que aporta hash, salt, algoritmo e iteraciones persistidos.</param>
    /// <returns><see langword="true"/> si el algoritmo es compatible y el hash derivado coincide en tiempo constante.</returns>
    public bool Verify(string password, AuthenticationAccount account)
    {
        if (
            !string.Equals(account.PasswordAlgorithm, CurrentAlgorithm, StringComparison.Ordinal)
            || account.PasswordIterations < 100_000
        )
        {
            return false;
        }

        var candidate = Rfc2898DeriveBytes.Pbkdf2(
            password,
            account.PasswordSalt,
            account.PasswordIterations,
            HashAlgorithmName.SHA512,
            account.PasswordHash.Length
        );
        return CryptographicOperations.FixedTimeEquals(candidate, account.PasswordHash);
    }

    /// <summary>Valida la política mínima de una contraseña nueva.</summary>
    /// <param name="password">Contraseña nueva; requiere al menos diez caracteres, mayúscula, minúscula y número.</param>
    /// <exception cref="ArgumentException">Falta la contraseña o incumple longitud o complejidad.</exception>
    public static void ValidateNewPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("La contraseña es obligatoria.");
        if (
            password.Length < 10
            || !password.Any(char.IsUpper)
            || !password.Any(char.IsLower)
            || !password.Any(char.IsDigit)
        )
        {
            throw new ArgumentException(
                "La contraseña debe tener al menos 10 caracteres, mayúscula, minúscula y número."
            );
        }
    }
}
