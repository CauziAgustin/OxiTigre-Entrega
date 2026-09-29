/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.System.ApiErrorResponse
Archivo: ApiErrorResponse.cs | Versión: 1.0.0 | Fecha: 2026-08-20 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Expone un error controlado sin detalles técnicos sensibles.
Historial: 1.0.0 | 2026-08-20 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.System;

/// <summary>Identifica un error funcional y su correlación para soporte.</summary>
public sealed record ApiErrorResponse(long ErrorCode, string Message, Guid CorrelationId);
