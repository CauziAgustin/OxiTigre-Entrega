/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Contracts.System.HealthResponse
Archivo: HealthResponse.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Contrato de respuesta del control de salud de la API.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Contracts.System;

/// <summary>Informa el estado observable de la API y el ambiente que responde.</summary>
/// <param name="Status">Estado general del servicio.</param>
/// <param name="Environment">Ambiente de ejecución.</param>
/// <param name="TimestampUtc">Fecha y hora UTC de la respuesta.</param>
public sealed record HealthResponse(string Status, string Environment, DateTimeOffset TimestampUtc);
