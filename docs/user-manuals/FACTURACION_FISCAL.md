# Facturación fiscal · Preparación

## Objetivo

La pantalla comprueba si la empresa está preparada para iniciar la homologación con ARCA. Las ventas mostradas son
internas: no son facturas, no tienen CAE y no deben entregarse como comprobantes fiscales.

## Uso

1. Ingresar a **Fiscal · Preparación** desde el panel principal.
2. Leer **Requisitos para homologación**. Cada fila muestra el valor actual, su estado y qué falta hacer.
3. Usar **Abrir configuración** para completar únicamente información confirmada por el contador.
4. Presionar **Actualizar diagnóstico** para volver a evaluar los requisitos.
5. Revisar **Ventas internas candidatas** para identificar operaciones que posteriormente podrán originar un
   comprobante fiscal.

## Datos que no deben inventarse

- CUIT y condición frente al IVA del emisor;
- servicio `WSFEV1` o `WSMTXCA`;
- punto de venta Web Services;
- categoría fiscal de cada cliente y tipos de comprobante;
- referencia al certificado de homologación.

El certificado y su clave privada se guardarán en un almacén de secretos externo. Nunca deben pegarse en Git, en un
archivo de configuración versionado ni en el campo **Valor** de un parámetro.

## Resultado esperado en esta etapa

La emisión permanece bloqueada y la pantalla explica los pendientes. Se habilitará recién después de completar los
datos reales, probar homologación y aprobar los resultados con el asesor contable.
