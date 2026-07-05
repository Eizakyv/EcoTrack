# 🌿 EcoTrack — Sistema de Monitoreo Geoespacial para Áreas Protegidas

**EcoTrack** es una plataforma integral de asistencia y supervisión en tiempo real para el Parque Natural Metropolitano (y otras áreas protegidas). Conecta a los visitantes con la administración mediante una aplicación móvil (Android) y un panel de control web, garantizando la seguridad de las personas y la preservación del entorno natural.

---

## 🎯 Objetivo del Proyecto

Crear un sistema de monitoreo geoespacial que permita:

- **Guiar a los visitantes** dentro de los senderos autorizados, evitando desvíos que puedan dañar el ecosistema.
- **Prevenir accidentes** manteniendo a los usuarios alejados de las líneas de alta tensión que cruzan el área.
- **Brindar herramientas de gestión** a la administración del parque (guardaparques y administradores) para supervisar en tiempo real la ubicación y el estado de todos los visitantes.
- **Generar alertas automáticas** cuando un usuario se sale del sendero o entra en zonas de riesgo.

---

## ✨ Características Principales

### 📱 Aplicación Móvil (Android)

- Mapa interactivo con la posición del usuario (círculo con efecto pulsante).
- Notificación permanente en la interfaz con el estado actual: **SEGURO**, **ADVERTENCIA** o **PELIGRO**.
- Seguimiento de ubicación en **primer plano y segundo plano** (servicio foreground en Android).
- Modo **simulación** con 6 perfiles de ruta para pruebas y demostraciones (Perfect Trail Walker, Zigzag In/Out Tracker, Permanently Lost / Off‑Trail, High Voltage Risk Walker, Within the research zone, Outside the park).
- Visualización de otros usuarios (solo para administradores y guardaparques) dentro del parque.
- Switch de seguimiento para habilitar/deshabilitar el centrado automático del mapa.

### 🖥️ Panel de Control Web (Dashboard)

- **Métricas en vivo**: visitantes en parque, guardaparques activos, administradores, fuera de sendero y en peligro.
- **Tabla de ocupación de senderos**: muestra cuántas personas hay en cada sendero y su estado de flujo (despejado/normal/concurrido).
- **Alertas de seguridad**: lista de usuarios en riesgo (advertencia o peligro) con un clic para centrar el mapa en su ubicación y abrir su popup.
- **Mapa interactivo** con capas GeoJSON (senderos, líneas de alta tensión, límites del parque, parcela de investigación, pluma grúa, puntos de interés).
- **Actualización automática** cada 1.5 segundos.
- **Carga de GeoJSON** mediante arrastrar y soltar (drag & drop) o selección individual.

### 🔐 Seguridad y Gestión de Acceso

- **Inicio de sesión con credenciales** (usuario y contraseña).
- **Encriptación de contraseñas** mediante hash SHA‑256 (nunca se almacenan en texto plano).
- **Gestión de roles**:
  - **Admin**: acceso total (dashboard, simulación, gestión).
  - **Guard**: visualización de mapa y alertas, sin controles administrativos.
  - **User**: solo puede usar la app móvil y ver su propia ubicación.
- **Cierre de sesión** seguro, que elimina la sesión local y detiene el envío de ubicación en segundo plano.
- **Backup automático de la base de datos** (configurable mediante scripts o tareas programadas).

---

## 🛠️ Tecnologías Utilizadas

### Aplicación Móvil
- **.NET MAUI** – Framework multiplataforma para la app Android.
- **C#** – Lenguaje principal.
- **XAML** – Diseño de interfaces de usuario.

### Panel de Control Web
- **HTML5, CSS3, JavaScript** – Estructura y estilos del dashboard.
- **Leaflet.js** – Biblioteca para el mapa interactivo.

### Servidor API
- **Python 3** – Lenguaje del backend.
- **Flask** – Framework web para la API REST.
- **Flask‑CORS** – Manejo de políticas de origen cruzado.
- **Shapely / PyProj** – Procesamiento geoespacial y cálculos de distancias.

### Base de Datos
- **PostgreSQL** – Sistema de gestión de bases de datos relacional.
- **PostGIS** – Extensión geoespacial para manejo de geometrías y consultas espaciales.

### Seguridad
- **SHA‑256** – Hash de contraseñas.
- **Variables de entorno** – Configuración segura de credenciales y URLs.

### Despliegue
- **Render** – Plataforma de hosting para el servidor API.

---

## 📌 Estado del Proyecto

El sistema se encuentra en funcionamiento, con la app móvil publicada en Android y el dashboard accesible desde cualquier navegador. La plataforma ha sido probada en el Parque Natural Metropolitano y está lista para su uso en producción.

---

## 📄 Licencia

Este proyecto está bajo la licencia **MIT**.

---

**🌍 EcoTrack — Monitoreo inteligente para la conservación y seguridad en áreas naturales.**
