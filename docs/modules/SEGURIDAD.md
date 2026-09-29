# Seguridad: usuarios, roles y sesiones

## Alcance completado

- Login con usuario y contraseña, seguido por selección de empresa autorizada.
- Contraseña temporal con cambio obligatorio en el primer ingreso.
- Token opaco de sesión con vigencia de ocho horas; la base conserva solo SHA-512 del token.
- Cierre voluntario y revocación administrativa de sesiones.
- Detección de revocación cada cinco segundos y cierre informado con cuenta regresiva de quince segundos.
- Bloqueo de quince minutos al quinto intento fallido.
- Listado, alta, edición, activación e inactivación de usuarios.
- Asignación de uno o más roles activos.
- Reinicio administrativo de contraseña temporal con revocación automática de sesiones.

## Reglas funcionales

- La administración requiere el rol `ADMINISTRADOR`.
- Cada usuario pertenece a una empresa y no puede administrarse desde otra.
- Debe existir al menos un rol activo por usuario.
- El administrador autenticado no puede desactivar su propia cuenta ni quitarse `ADMINISTRADOR`.
- El administrador puede revocar sus propias sesiones; esta acción cierra también la aplicación actual.
- Desactivar una cuenta, reiniciar su clave o revocar sus sesiones invalida los tokens anteriores.
- No se eliminan usuarios ni asignaciones: los cambios conservan historial mediante estados y fechas de vigencia.

## Componentes

- WinForms: `LoginForm`, `DashboardForm`, `SessionEndedForm`, `UserManagementForm`, `UserEditForm` y `ChangePasswordForm`.
- API: rutas bajo `/api/security` documentadas en `docs/SECURITY.md`.
- BLL: `AuthenticationService` y `UserAdministrationService`.
- SQL Server: `SEGURIDAD.USUARIOS`, `USUARIOS_ROLES`, `ROLES` y `SESIONES`.

## Verificación

Ejecutar la solución y la prueba transaccional:

```powershell
dotnet test OxiTigre.sln --configuration Release
sqlcmd -S ".\SQLEXPRESS" -d "OxiTigre_Development" -E -b -C -i "tests\Database\SMOKE_SEGURIDAD_FASE2.sql"
```

La revisión manual pendiente está enumerada en `docs/TEST_CASES.md`.
