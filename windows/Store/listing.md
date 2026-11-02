# Microsoft Store listing

Texts to paste into Partner Center when submitting WakaBar. English first, Spanish below. Keep them in
sync with the app: when a feature changes, update both languages here.

## Properties (both languages)

| Field | Value |
|---|---|
| Category | Developer tools |
| Privacy policy URL | https://github.com/cesarRomero100/waka-bar/blob/main/PRIVACY.md |
| Website | https://github.com/cesarRomero100/waka-bar |
| Support contact | https://github.com/cesarRomero100/waka-bar/issues |
| Pricing | Free |
| Markets | All |

## Restricted capability: `runFullTrust`

Partner Center asks why the app uses it. Paste:

> WakaBar is a WinUI 3 desktop app (Windows App SDK). runFullTrust is required for packaged desktop apps:
> it runs the tray icon (Shell_NotifyIcon), the COM server that provides the Widgets Board widget, and the
> startup task. The app only connects to the WakaTime server the user configures.

## Notes for certification

> WakaBar shows coding stats from WakaTime and needs a WakaTime token. To test it: create a free account at
> https://wakatime.com/signup, get your token at https://wakatime.com/oauth and paste it in the Settings
> window that opens on first launch. A new account has no coding time yet, so the stats show
> 0 min and the widget shows its empty state; that's expected. WakaBar lives in the system tray (open the ^
> overflow next to the clock). The widget is in the Widgets Board: Win+W > Add widgets > WakaBar.

---

## English (en-us)

**Product name:** WakaBar

**Short description**

Your WakaTime coding time in the system tray and on the Widgets Board: today, the last 7 days, your
daily goal and your streak.

**Description**

WakaBar keeps your WakaTime stats one glance away. Today's coding time lives in the system tray, next to
the clock, and a widget on the Widgets Board shows the week.

Click the tray icon for the details: today, the last 7 days with a daily average and a bar chart, and
your top languages or projects. Set a daily goal and a ring around the icon fills up as you code, turning
green when you reach it. Keep a streak going by coding every day.

Works with WakaTime and with self-hosted servers like Wakapi. WakaBar is free and open source, with no
ads and no tracking: it only talks to your WakaTime server, and your token stays in Windows Credential
Manager.

You need a WakaTime account and the WakaTime plugin in your editor, so there's coding time to show.

**Features**

- Today's coding time in the system tray, refreshed every 5 minutes and when your PC wakes up
- Last 7 days: total, daily average and a bar chart
- Top languages and projects
- Daily goal with a progress ring on the tray icon
- Streak: consecutive days coding, and today's top project
- Widgets Board widget in small, medium and large sizes; click it to open your dashboard
- Works with WakaTime and self-hosted Wakapi
- Launch at startup, from Settings
- English and Spanish, following your Windows language
- Free and open source, with no ads or tracking

**Keywords:** WakaTime, Wakapi, coding time, time tracking, developer, productivity, widget

**What's new in this version**

First release on the Microsoft Store.

---

## Español (es-mx, es-es)

**Nombre del producto:** WakaBar

**Descripción breve**

Tu tiempo programando de WakaTime en la bandeja del sistema y en el panel de widgets: hoy, los últimos 7
días, tu meta diaria y tu racha.

**Descripción**

WakaBar tiene tus estadísticas de WakaTime siempre a la vista. El tiempo que llevas programando hoy vive
en la bandeja del sistema, junto al reloj, y un widget en el panel de widgets muestra la semana.

Haz clic en el ícono de la bandeja para ver el detalle: hoy, los últimos 7 días con promedio diario y
gráfica de barras, y tus lenguajes o proyectos principales. Define una meta diaria y un anillo alrededor
del ícono se llena mientras programas, y se pone verde cuando la cumples. Mantén tu racha programando
todos los días.

Funciona con WakaTime y con servidores propios como Wakapi. WakaBar es gratis y de código abierto, sin
anuncios ni rastreo: solo se conecta a tu servidor de WakaTime, y tu token se queda en el Administrador
de credenciales de Windows.

Necesitas una cuenta de WakaTime y el plugin de WakaTime en tu editor, para que haya tiempo que mostrar.

**Características**

- Tu tiempo programando de hoy en la bandeja del sistema, actualizado cada 5 minutos y al despertar tu PC
- Últimos 7 días: total, promedio diario y gráfica de barras
- Lenguajes y proyectos principales
- Meta diaria con un anillo de progreso en el ícono de la bandeja
- Racha: días seguidos programando, y tu proyecto principal de hoy
- Widget para el panel de widgets en tamaño pequeño, mediano y grande; haz clic para abrir tu dashboard
- Funciona con WakaTime y con Wakapi autoalojado
- Abrir al iniciar Windows, desde Ajustes
- Inglés y español, según el idioma de Windows
- Gratis y de código abierto, sin anuncios ni rastreo

**Palabras clave:** WakaTime, Wakapi, tiempo programando, registro de tiempo, desarrollador, productividad, widget

**Novedades de esta versión**

Primera versión en Microsoft Store.

---

## Screenshots

At least one per language, 1366×768 or larger (PNG). Take them on Windows 11 with real stats, in light
and dark mode:

1. The stats window open from the tray icon.
2. The Widgets Board with the large widget.
3. The Settings window.
4. The tray icon with the goal ring, with its tooltip.
