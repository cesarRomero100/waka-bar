# Privacy policy

*Last updated: November 2, 2026. [Versión en español abajo](#política-de-privacidad).*

This policy covers the WakaBar apps for macOS and Windows, including their widgets.

**WakaBar collects no data.** It has no account, no analytics, no telemetry, no ads and no crash
reporting, and it doesn't share anything with the WakaBar developers or with third parties.

## What WakaBar stores, on your computer only

- **Your WakaTime token** (or your Wakapi API key), in the macOS Keychain or the Windows Credential
  Manager.
- **Your settings:** the server address, the daily goal and launch at login.
- **Your latest stats** (coding time, languages, projects), so the app and the widget can show them
  right away. The widget reads these cached stats; it never sees your token.

To delete your token, clear it in WakaBar's Settings. On Windows, uninstalling WakaBar removes
everything it stored. On macOS, moving the app to the Trash leaves its settings and cached stats on your
Mac, so clear the token in Settings first. You can also revoke the token on wakatime.com.

## What WakaBar sends, and where

WakaBar only connects to the WakaTime server you set in Settings: `api.wakatime.com` by default, or your
own server, such as a self-hosted [Wakapi](https://github.com/muety/wakapi). It sends your token to
that server to read your stats, every 5 minutes and when the computer wakes up. That server's own
privacy policy applies to your data there; for WakaTime, see
[wakatime.com/legal/privacy-policy](https://wakatime.com/legal/privacy-policy).

Links in the app (your dashboard, this repository) open in your browser.

## Contact

Questions or concerns: [open an issue](https://github.com/cesarRomero100/waka-bar/issues). Changes to
this policy are published here, with the date at the top.

---

# Política de privacidad

*Última actualización: 2 de noviembre de 2026.*

Esta política cubre las apps de WakaBar para macOS y Windows, incluidos sus widgets.

**WakaBar no recopila datos.** No tiene cuentas, analíticas, telemetría, anuncios ni reportes de
errores, y no comparte nada con los desarrolladores de WakaBar ni con terceros.

## Qué guarda WakaBar, solo en tu computadora

- **Tu token de WakaTime** (o tu API key de Wakapi), en el Llavero de macOS o en el Administrador de
  credenciales de Windows.
- **Tu configuración:** la dirección del servidor, la meta diaria y abrir al iniciar sesión.
- **Tus estadísticas más recientes** (tiempo programando, lenguajes, proyectos), para que la app y el
  widget las muestren al instante. El widget lee esa copia; nunca ve tu token.

Para borrar tu token, déjalo vacío en los Ajustes de WakaBar. En Windows, al desinstalar WakaBar se
borra todo lo que guardó. En macOS, mandar la app a la Papelera deja su configuración y sus estadísticas
guardadas en tu Mac, así que primero borra el token en Ajustes. También puedes revocar el token en
wakatime.com.

## Qué envía WakaBar, y a dónde

WakaBar solo se conecta al servidor de WakaTime que configures en Ajustes: `api.wakatime.com` por
defecto, o tu propio servidor, como un [Wakapi](https://github.com/muety/wakapi) autoalojado. Envía tu
token a ese servidor para leer tus estadísticas, cada 5 minutos y cuando la computadora despierta. Tus
datos en ese servidor se rigen por su propia política de privacidad; para WakaTime, consulta
[wakatime.com/legal/privacy-policy](https://wakatime.com/legal/privacy-policy).

Los enlaces de la app (tu dashboard, este repositorio) se abren en tu navegador.

## Contacto

Dudas o comentarios: [abre un issue](https://github.com/cesarRomero100/waka-bar/issues). Los cambios a
esta política se publican aquí, con la fecha al inicio.
