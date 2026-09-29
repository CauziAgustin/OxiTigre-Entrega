# Fase 12: facturación fiscal argentina

## Estado

La preparación funcional 12A está implementada: existe un diagnóstico visible de requisitos y ventas internas
candidatas. La emisión fiscal permanece desactivada. Los recibos internos de la Fase 11 no se presentan como
facturas y ninguna numeración, CAE o respuesta de ARCA se simula como válida.

La configuración guarda solamente ambiente, servicio, punto de venta y condición tributaria no sensibles. El
certificado y la clave privada deberán resolverse mediante una referencia a un almacén externo.

## Definiciones obligatorias

Antes de crear comprobantes fiscales, Agustin y su asesor contable deben confirmar:

1. CUIT y condición frente al IVA de cada empresa emisora.
2. Tipos de comprobante necesarios: A, B, C, M, notas de crédito y notas de débito.
3. Web service aplicable: `wsfev1` o `wsmtxca`, según el detalle fiscal que deba informarse.
4. Punto de venta exclusivo para Web Services en homologación.
5. Certificado X.509 de homologación asociado al servicio elegido.
6. Reglas de IVA, percepciones, exenciones, moneda y actividades aplicables al negocio real.
7. Formato legal del comprobante y política de conservación de originales y duplicados.

## Alcance técnico posterior a la confirmación

- comprobante borrador separado de la venta interna;
- numeración correlativa por empresa, punto de venta y tipo;
- detalle, impuestos y totales validados antes del envío;
- autorización por CAE y registro completo de solicitud, respuesta, observaciones y errores;
- nota de crédito o débito vinculada, sin modificar ni borrar el comprobante original;
- homologación antes de habilitar producción;
- certificado y clave privada fuera de Git y de la base operativa;
- contingencia CAEA solo si el encuadre real la requiere y el contador la aprueba.

## Fuentes oficiales

- [Webservices de factura electrónica](https://www.arca.gob.ar/ws/documentacion/ws-factura-electronica.asp)
- [WSAA, certificados y ambientes](https://www.arca.gob.ar/ws/documentacion/wsaa.asp)
- [Emisión, autorización y puntos de venta](https://arca.gob.ar/fe/emision-autorizacion/solicitud-autorizacion.asp)
