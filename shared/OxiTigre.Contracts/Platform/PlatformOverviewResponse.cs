/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.Platform.PlatformOverviewResponse
Archivo: PlatformOverviewResponse.cs | Versión: 1.0.0 | Fecha: 2026-08-27 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Expone indicadores globales sin mezclar registros operativos entre empresas.
Historial: 1.0.0 | 2026-08-27 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.Platform;

/// <summary>Resume una empresa registrada para supervisión administrativa global.</summary>
public sealed record CompanyOverviewResponse(
    string CompanyCode,
    string CompanyName,
    string SchemaVersion,
    string Status,
    int ActiveClients,
    int Products,
    decimal PhysicalStock,
    int OpenOrders,
    int Sales);

/// <summary>Agrupa los indicadores autorizados de todas las bases operativas.</summary>
public sealed record PlatformOverviewResponse(IReadOnlyList<CompanyOverviewResponse> Companies);
