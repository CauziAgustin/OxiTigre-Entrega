# Panel administrativo web

Cliente Blazor de supervisión conectado a la misma API que WinForms. Solo admite sesiones cuyo rol funcional sea
`ADMINISTRADOR`; la validación se realiza al iniciar sesión y nuevamente mediante autorización de ruta.

## Alcance actual

- indicadores de usuarios, clientes, productos, pedidos, ventas y alertas de mínimo;
- stock físico, reservado y disponible por producto y depósito;
- pedidos, ventas internas y usuarios recientes;
- cookie cifrada, `HttpOnly`, no persistente y limitada a la vigencia de la sesión de API;
- cierre coordinado de la sesión web y el token opaco de la API.

El panel es de supervisión. Las altas y modificaciones continúan en WinForms para no duplicar reglas hasta aprobar
qué operaciones administrativas deben habilitarse desde un navegador.

## Ejecución local

Con la API iniciada en `http://localhost:5000`:

```powershell
dotnet run --project frontend/OxiTigre.AdminWeb
```

Abrir `http://localhost:5100`. La empresa local es `OXITIGRE`. Puede cambiarse la API con `OXITIGRE_API_URL`.
