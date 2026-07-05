# 🌿 EcoTrack

**Sistema de monitoreo geoespacial para áreas protegidas**

EcoTrack conecta a los visitantes del Parque Natural Metropolitano con la administración mediante una app móvil (Android) y un panel web en tiempo real. El sistema guía a los usuarios por senderos autorizados, previene el acercamiento a líneas de alta tensión y alerta automáticamente sobre desvíos o zonas de riesgo.

---

## 🎯 Objetivo

- Guiar a los visitantes dentro de los senderos permitidos.
- Evitar que se acerquen a las líneas de alta tensión.
- Supervisar en tiempo real la ubicación y el estado de todos los usuarios.
- Generar alertas automáticas ante desvíos o peligros.

---

## ✨ Características

### 📱 Aplicación Móvil (Android)
- Mapa interactivo con posición en vivo.
- Notificación de estado: **SEGURO**, **ADVERTENCIA** o **PELIGRO**.
- Seguimiento en primer y segundo plano (servicio foreground).
- Modo simulación con 6 perfiles de ruta.
- Visualización de otros usuarios (admin/guard).
- Switch de seguimiento para centrado automático.

### 🖥️ Panel Web (Dashboard)
- Métricas en vivo: visitantes, guardaparques, administradores, fuera de sendero y en peligro.
- Tabla de ocupación de senderos con estado de flujo.
- Alertas de seguridad clickeables para centrar el mapa en el usuario.
- Mapa interactivo con capas GeoJSON (senderos, líneas de tensión, límites, etc.).
- Actualización automática cada 1.5 segundos.
- Carga de GeoJSON por drag & drop o selección individual.

### 🔐 Seguridad
- Inicio de sesión con credenciales.
- Contraseñas encriptadas con SHA‑256.
- Gestión de roles: **Admin**, **Guard**, **User**.
- Cierre de sesión seguro.
- Backup automático de la base de datos (configurable).

---

## 🛠️ Tecnologías

- **Aplicación Móvil**: .NET MAUI, C#, XAML.
- **Panel Web**: HTML5, CSS3, JavaScript, Leaflet.js.
- **Servidor API**: Python 3, Flask, Flask‑CORS, Shapely, PyProj.
- **Base de Datos**: PostgreSQL, PostGIS.
- **Seguridad**: SHA‑256, variables de entorno.
- **Despliegue**: Render.

---

**🌍 EcoTrack — Monitoreo inteligente para la conservación y seguridad en áreas naturales.**
