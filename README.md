# CGA Metrology System

Sistema web para la gestión de la trazabilidad metrológica de equipos de medición utilizado por **CGA-OIL Inspection Services S.A.S.**. La plataforma centraliza la administración de equipos, controles metrológicos, documentación técnica, indicadores y alertas, permitiendo mantener el historial completo de cada instrumento durante todo su ciclo de vida.

---

# Características principales

- Gestión integral de equipos metrológicos.
- Configuración flexible de controles por equipo.
- Registro de calibraciones, verificaciones y mantenimientos.
- Generación automática de fichas técnicas y hojas de vida en PDF.
- Gestión documental integrada con Google Drive.
- Dashboard ejecutivo con indicadores metrológicos.
- Maestro de equipos con exportación a Excel.
- Score metrológico para priorización de riesgos.
- Alertas y notificaciones automáticas.
- Auditoría de acciones del sistema.
- Administración de usuarios y roles mediante ASP.NET Core Identity.

---

# Arquitectura

El proyecto sigue una arquitectura por capas basada en ASP.NET Core MVC.

```text
Presentation (ASP.NET Core MVC)
│
├── Controllers
├── Views
├── ViewModels
│
Application
│
├── DTOs
├── Interfaces
├── Services
│
Domain
│
├── Entities
│
Infrastructure
│
├── Entity Framework Core
├── PostgreSQL
├── Identity
├── Google Drive
├── SMTP
│
External Services
├── Google Drive API
├── PostgreSQL
└── Email SMTP
```

---

# Tecnologías utilizadas

- .NET 10
- ASP.NET Core MVC
- Entity Framework Core
- PostgreSQL
- ASP.NET Core Identity
- Razor Views
- Bootstrap
- ClosedXML
- QuestPDF
- Google Drive API
- SMTP
- Docker
- Render

---

# Estructura del proyecto

```text
CGA.MetrologySystem
│
├── CGA.MetrologySystem
│   ├── Controllers
│   ├── Views
│   ├── Models
│   ├── ViewModels
│   ├── Services
│   ├── Configuration
│   └── wwwroot
│
├── CGA.MetrologySystem.Application
│
├── CGA.MetrologySystem.Domain
│
├── CGA.MetrologySystem.Infrastructure
│
└── CGA.MetrologySystem.Tests
```

---

# Módulos principales

## Gestión de equipos

Permite registrar y administrar los equipos metrológicos, incluyendo información técnica, ubicación, responsable, proveedor, fotografía y características metrológicas.

## Configuración de controles

Define los controles metrológicos aplicables a cada equipo, estableciendo periodicidades, unidades de tiempo y reglas que posteriormente son utilizadas por el sistema para calcular vencimientos, alertas y estados.

## Calibraciones

Registro de calibraciones realizadas por laboratorios, incluyendo certificados, observaciones, fechas y cálculo automático de la próxima calibración.

## Verificaciones

Registro de verificaciones operativas mediante listas de comprobación, resultados individuales, evidencias y generación automática de documentación.

## Mantenimientos

Gestión de mantenimientos preventivos y correctivos, actividades realizadas, evidencias y documentos asociados.

## Gestión documental

Administración de certificados, evidencias, fichas técnicas, hojas de vida y fotografías de equipos mediante integración con Google Drive.

## Control metrológico

Visualización del estado de cada equipo mediante semaforización que permite identificar equipos vigentes, próximos a vencer, vencidos o sin configuración.

## Dashboard

Panel ejecutivo con indicadores para el seguimiento del estado metrológico de los equipos y análisis de información relevante para la toma de decisiones.

## Maestro de equipos

Vista consolidada del inventario metrológico con información técnica, estado, score de riesgo y exportación a Microsoft Excel.

## Alertas y notificaciones

Generación automática de alertas para controles próximos a vencer, controles vencidos y otras condiciones relevantes, incluyendo envío de notificaciones por correo electrónico.

## Auditoría

Registro de acciones realizadas por los usuarios para garantizar la trazabilidad de las operaciones efectuadas dentro del sistema.

---

# Integraciones

## Google Drive

El sistema almacena en Google Drive:

- Certificados de calibración
- Evidencias
- Fotografías
- Fichas técnicas
- Hojas de vida
- Documentos PDF generados automáticamente

La base de datos conserva únicamente las referencias necesarias para acceder a dichos archivos.

## Correo electrónico

Las alertas y notificaciones se envían mediante un servidor SMTP configurable desde el archivo de configuración de la aplicación.

---

# Instalación

## Requisitos

- .NET 10 SDK
- PostgreSQL
- Visual Studio 2022 o superior
- Credenciales de Google Drive
- Configuración SMTP (opcional)

### Restaurar paquetes

```bash
dotnet restore
```

### Compilar

```bash
dotnet build CGA.MetrologySystem.slnx
```

### Ejecutar

```bash
dotnet run --project CGA.MetrologySystem/CGA.MetrologySystem.csproj
```

---

# Configuración

La configuración principal se encuentra en:

```
appsettings.json
appsettings.Development.json
```

Es necesario configurar:

- ConnectionStrings
- GoogleDriveSettings
- GoogleOAuthSettings
- GoogleOAuthTokenStorageSettings
- SmtpSettings
- AlertasSettings
- NotificacionesSettings

No se recomienda almacenar credenciales, tokens o secretos directamente en el repositorio.

---

# Docker

El proyecto incluye un Dockerfile preparado para su despliegue mediante contenedores.

La aplicación soporta el uso de la variable de entorno `PORT`, permitiendo su ejecución en plataformas como Render.

---

# Despliegue

El sistema puede desplegarse en servicios compatibles con aplicaciones ASP.NET Core, como Render.

Para el despliegue se requiere configurar:

- Base de datos PostgreSQL
- Variables de entorno
- Credenciales de Google Drive
- Configuración SMTP

Todos los archivos fuente deben mantenerse en codificación UTF-8 para garantizar el correcto funcionamiento en entornos Linux.

---

# Roles del sistema

- Administrador del Sistema
- Administrador Metrológico
- Técnico

Cada rol dispone de permisos específicos sobre los diferentes módulos de la plataforma.

---

# Buenas prácticas

- Mantener los archivos fuente en UTF-8.
- No versionar credenciales ni tokens.
- Ejecutar `dotnet build` antes de publicar cambios.
- Mantener las migraciones sincronizadas con el modelo de datos.
- Verificar la configuración de Google Drive antes de desplegar.

---

# Licencia

Proyecto desarrollado como parte del trabajo de titulación para la carrera de Ingeniería de Software de la Universidad de Las Américas (UDLA).

Su uso está destinado a fines académicos y al apoyo de la gestión de trazabilidad metrológica de **CGA-OIL Inspection Services S.A.S.**
