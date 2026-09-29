/*
===============================================================================
Proyecto: Sistema Modular de Gestión OxiTigre
Componente: OxiTigre.Domain.Errors.ErrorCode
Archivo: ErrorCode.cs | Versión: 1.0.0 | Fecha: 2026-08-19 | ID pedido: FABRICA
Desarrollador: Agustin Omar Cauzi | Correo: agustincauzi10@hotmail.com
Descripción funcional: Representa y valida un código funcional de error.
Historial: 1.0.0 | 2026-08-19 | FABRICA | Agustin Omar Cauzi | Creación inicial.
===============================================================================
*/
namespace OxiTigre.Domain.Errors;

/// <summary>
/// Representa un código de error compuesto por el módulo y un consecutivo de cuatro posiciones.
/// </summary>
public readonly record struct ErrorCode
{
    /// <summary>Inicializa un código funcional a partir de su módulo y secuencia.</summary>
    /// <param name="module">Número del módulo que forma la primera parte del código.</param>
    /// <param name="sequence">Secuencia correlativa dentro del módulo.</param>
    private ErrorCode(int module, int sequence)
    {
        Module = module;
        Sequence = sequence;
        Value = checked((module * 10_000L) + sequence);
    }

    /// <summary>Obtiene el identificador del módulo, entre 1 y 999.</summary>
    public int Module { get; }

    /// <summary>Obtiene el consecutivo del error dentro del módulo, entre 1 y 9999.</summary>
    public int Sequence { get; }

    /// <summary>Obtiene el código numérico sin ceros a la izquierda.</summary>
    public long Value { get; }

    /// <summary>
    /// Crea un código validado mediante la fórmula <c>módulo * 10000 + consecutivo</c>.
    /// </summary>
    /// <param name="module">Identificador del módulo entre 1 y 999.</param>
    /// <param name="sequence">Consecutivo del error entre 1 y 9999.</param>
    /// <returns>Un código de error válido.</returns>
    /// <exception cref="ArgumentOutOfRangeException">El módulo o consecutivo está fuera del rango permitido.</exception>
    public static ErrorCode Create(int module, int sequence)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(module, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(module, 999);
        ArgumentOutOfRangeException.ThrowIfLessThan(sequence, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sequence, 9_999);

        return new ErrorCode(module, sequence);
    }

    /// <summary>Devuelve el código numérico utilizando una representación independiente de la cultura.</summary>
    /// <returns>El código sin separadores ni ceros a la izquierda.</returns>
    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
