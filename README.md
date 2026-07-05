# 🌿 EcoTrack — Monitoreo de Senderos en Tiempo Real

**EcoTrack** es una plataforma integral para la gestión y monitoreo de visitantes y personal en áreas naturales protegidas. Combina una aplicación móvil (desarrollada con **.NET MAUI**) con un **panel de control web interactivo** que muestra estadísticas, alertas y un mapa en vivo con la ubicación de todos los usuarios dentro del parque.

---

## ✨ Características principales

- 📱 **App móvil multiplataforma** (Android principal) con seguimiento de ubicación en primer y segundo plano.
- 🗺️ **Mapa interactivo** con capas GeoJSON (senderos, líneas de alta tensión, límites, puntos de interés, etc.).
- 🔴 **Alertas en tiempo real** para usuarios en peligro o fuera del sendero.
- 📊 **Dashboard web** con métricas: visitantes, guardaparques, administradores y personas en riesgo.
- 🧭 **Modo simulación** de rutas para pruebas y demostraciones.
- 🔐 **Autenticación y roles** (admin, guard, user) con sesiones persistentes.
- ⏱️ **Actualización continua** cada 1.5 segundos (ubicación, notificaciones, mapa).
- 📦 **Servicio en segundo plano** (Android) que mantiene el envío de ubicación incluso con la app cerrada.

---

## 🛠️ Stack tecnológico

| Componente | Tecnología |
|------------|------------|
| **App móvil** | .NET MAUI (C#) |
| **Servidor API** | Python 3 + Flask |
| **Base de datos** | PostgreSQL + PostGIS |
| **Dashboard web** | HTML, CSS, JavaScript + Leaflet |
| **Geolocalización** | GPS nativo (Android) + simulación |
| **Comunicación** | HTTP REST (JSON) |

---

## 📁 Estructura del repositorio
