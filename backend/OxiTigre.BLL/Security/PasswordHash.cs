/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.BLL.Security.PasswordHash
Archivo: PasswordHash.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Agrupa los componentes de una contraseña derivada segura.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.BLL.Security;

/// <summary>Transporta hash, salt, algoritmo e iteraciones sin texto plano.</summary>
public sealed record PasswordHash(byte[] Hash, byte[] Salt, string Algorithm, int Iterations);
