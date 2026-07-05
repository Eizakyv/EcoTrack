# 🌿 EcoTrack  
## *Sistema de Monitoreo Geoespacial para Áreas Protegidas*

---

## 🎯 Objetivo del Proyecto

EcoTrack es una plataforma que integra tecnología móvil y geoespacial para la **gestión y protección** de áreas naturales.  
Su propósito es:

- **Preservar el ecosistema** – Manteniendo a los visitantes dentro de los senderos autorizados, reduciendo el impacto ambiental.
- **Prevenir accidentes** – Alertando sobre la proximidad a la servidumbre de líneas de alta tensión que cruzan el parque.
- **Asistir en tiempo real** – Conectando a los visitantes con la administración para una supervisión efectiva.
- **Fomentar el turismo responsable** – Promoviendo el uso consciente de los espacios protegidos mediante tecnología accesible y confiable.

---

## 🧩 Componentes del Sistema

| **Aplicación Móvil** (Android) | **Panel de Control Web** | **Servidor API** |
|--------------------------------|---------------------------|-------------------|
| Mapa interactivo con posición en vivo | Dashboard con métricas y alertas | Procesa ubicaciones y calcula distancias |
| Notificaciones de estado (seguro / advertencia / peligro) | Tabla de ocupación de senderos | Autenticación y gestión de roles |
| Seguimiento en segundo plano (servicio foreground) | Mapa con capas GeoJSON | Integración con PostgreSQL / PostGIS |
| Modo simulación con 6 perfiles de ruta | Carga de archivos GeoJSON por drag & drop | Encriptación SHA‑256 |

---

## 🔐 Seguridad y Gestión de Acceso

- **Inicio de sesión** con credenciales propias.
- **Hash de contraseñas** mediante SHA‑256.
- **Roles diferenciados**:
  - `Admin` – control total, simulación y gestión.
  - `Guard` – visualización de mapa y alertas.
  - `User` – uso exclusivo de la app móvil.
- **Cierre de sesión** seguro que detiene la transmisión de ubicación.
- **Backup automático** de la base de datos.

---

## 🛠️ Tecnologías utilizadas

| Capa | Tecnologías |
|------|-------------|
| **App móvil** | .NET MAUI, C#, XAML |
| **Dashboard web** | HTML5, CSS3, JavaScript, Leaflet.js |
| **Servidor API** | Python 3, Flask, Flask‑CORS, Shapely, PyProj |
| **Base de datos** | PostgreSQL, PostGIS |
| **Seguridad** | SHA‑256, variables de entorno |
| **Despliegue** | Render |

---


## 🌱 Impacto ambiental

EcoTrack contribuye directamente a la **conservación** al desincentivar la salida de los senderos, reduciendo la pisoteo de vegetación sensible y la perturbación de la fauna. Además, previene accidentes por proximidad a infraestructura eléctrica, protegiendo tanto a las personas como al ecosistema.

---

## 📌 Estado actual

Proyecto funcional y probado en el Parque Natural Metropolitano.  
App publicada y dashboard accesible desde cualquier navegador.

---

**🧭 EcoTrack – Tecnología al servicio de la naturaleza.**
