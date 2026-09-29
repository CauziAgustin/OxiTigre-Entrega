/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.DAL.Tests.StoredProcedureRequestTests
Archivo: StoredProcedureRequestTests.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Verifica la construcción segura del nombre de un SP.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
using OxiTigre.DAL.StoredProcedures;

namespace OxiTigre.DAL.Tests;

/// <summary>Contiene pruebas de la solicitud de ejecución de Stored Procedures.</summary>
public sealed class StoredProcedureRequestTests
{
    /// <summary>Comprueba que schema y SP se delimiten correctamente.</summary>
    [Fact]
    public void QualifiedName_QuotesSchemaAndProcedure()
    {
        var request = new StoredProcedureRequest("AUDITORIA", "SP_ERROR_GET_BY_CODE");
        Assert.Equal("[AUDITORIA].[SP_ERROR_GET_BY_CODE]", request.QualifiedName);
    }
}
