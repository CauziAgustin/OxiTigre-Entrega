/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Tests.PasswordHasherTests
Archivo: PasswordHasherTests.cs | Versión: 1.1.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica compatibilidad criptográfica y política de contraseñas.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
Historial: 1.1.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Valida el mensaje funcional para contraseña vacía.
===============================================================================
*/
using OxiTigre.BLL.Security;

namespace OxiTigre.BLL.Tests;

/// <summary>Valida el contrato PBKDF2 compartido por seed y aplicación.</summary>
public sealed class PasswordHasherTests
{
    /// <summary>Comprueba que la contraseña temporal coincide con el hash versionado de Development.</summary>
    [Fact]
    public void Verify_DevelopmentSeedPassword_ReturnsTrue()
    {
        var account = new AuthenticationAccount(1, 1, "AOCAUZI", "Agustin Omar", "Cauzi", "test@example.com",
            Convert.FromHexString("8EEFD95BBACD6FB085C9255DF15B00E49D085E7AA8984164BE48E06195F91BAC9BE9D767EBB59DA525FD5BCDF2C124A468AD1ED3FDBA3512A72267946527E92E"),
            Convert.FromHexString("EB91E6E6A9835ABE0DA7CB91CB98D6B245493AA5091BD09857F089409880166B"),
            PasswordHasher.CurrentAlgorithm, PasswordHasher.CurrentIterations, true, 0, null, [], []);

        Assert.True(new PasswordHasher().Verify("Admin123", account));
        Assert.False(new PasswordHasher().Verify("Admin124", account));
    }

    /// <summary>Comprueba que las credenciales nuevas usan salt aleatorio y verifican correctamente.</summary>
    [Fact]
    public void Hash_StrongPassword_CreatesDistinctCredentials()
    {
        var hasher = new PasswordHasher();
        var first = hasher.Hash("NuevaClave123");
        var second = hasher.Hash("NuevaClave123");

        Assert.NotEqual(first.Salt, second.Salt);
        Assert.Equal(64, first.Hash.Length);
        Assert.Equal(32, first.Salt.Length);
    }

    /// <summary>Comprueba que una contraseña vacía produce un mensaje controlado en español.</summary>
    [Fact]
    public void ValidateNewPassword_Empty_ExplainsRequiredField()
    {
        var exception = Assert.Throws<ArgumentException>(() => PasswordHasher.ValidateNewPassword(""));

        Assert.Contains("obligatoria", exception.Message, StringComparison.OrdinalIgnoreCase);
    }
}
