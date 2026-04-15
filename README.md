# DeskIQ Ticket System

Sistema de tickets empresarial multicanal construido con .NET 8, PostgreSQL y Angular.

## Características

- **500 tickets/día** capacidad
- **Múltiples canales**: Web UI, Email, WhatsApp, API
- **Backend .NET 8** con Entity Framework Core
- **Frontend Angular** con TailwindCSS
- **PostgreSQL** base de datos
- **Redis** para caché y sesiones
- **SignalR** para comunicación en tiempo real
- **JWT Authentication** con roles
- **Control de acceso basado en roles (RBAC)** con 7 roles granulares
- **Multi-departamento** para roles de supervisión
- **Auditoría** completa de cambios administrativos
- **Docker** para despliegue

## Arquitectura

```
DeskIQ.TicketSystem/
src/
  DeskIQ.TicketSystem.Core/          # Entidades y dominio
  DeskIQ.TicketSystem.Infrastructure/ # EF Core, servicios externos
  DeskIQ.TicketSystem.Application/    # Lógica de negocio, MediatR
  DeskIQ.TicketSystem.API/           # Web API con JWT
  deskiq-client/                     # Angular Client
```

## Inicio Rápido con Docker

1. **Clonar y construir**
```bash
git clone <repository>
cd DeskIQ
# Bash: cp .env.example .env
# PowerShell: Copy-Item .env.example .env
docker compose --env-file .env up --build -d
```

2. **Acceder a los servicios**
- API: http://localhost:5000
- Swagger UI: http://localhost:5000/swagger
- Cliente Angular: http://localhost:3000
- PostgreSQL: localhost:5433
- Redis: localhost:6379

3. **Usuario por defecto**
- Email: admin@deskiq.com
- Password: Admin123!
- Role: Administrador

## Roles y Permisos

El sistema implementa un control de acceso basado en roles (RBAC) con 7 roles granulares:

### Roles Disponibles
1. **Cliente** - Crea y ve solo sus propios tickets
2. **Cliente Supervisor** - Ve todos los tickets de su departamento (solo lectura)
3. **Operador** - Gestiona tickets en su departamento
4. **Operador Supervisor** - Operador + acceso a métricas del departamento
5. **Supervisor General** - Multi-departamento con filtros avanzados
6. **Auditor** - Acceso multi-departamento de solo lectura con comentarios
7. **Administrador** - Acceso completo de administración del sistema

Para ver la matriz completa de permisos, consulte [ROLE_MATRIX.md](ROLE_MATRIX.md).

### Migración de Roles
- Agent → Operador
- Supervisor → Operador Supervisor
- Admin → Administrador

Ejecute el script de migración: `init-scripts/05-role-refinement-migration.sql`

## Configuración Local

### Prerrequisitos
- Docker Desktop

### Levantar entorno de prueba
```bash
docker compose --env-file .env up --build -d
```

### Detener entorno
```bash
docker compose down
```

### Ver logs
```bash
docker compose logs -f
```

## API Endpoints

### Autenticación
- `POST /api/auth/login` - Iniciar sesión
- `POST /api/auth/register` - Registrar usuario
- `POST /api/auth/validate` - Validar token

### Tickets
- `GET /api/tickets` - Listar tickets (con filtros)
- `GET /api/tickets/{id}` - Obtener ticket
- `POST /api/tickets` - Crear ticket
- `PUT /api/tickets/{id}` - Actualizar ticket
- `POST /api/tickets/{id}/messages` - Agregar mensaje

### Departamentos
- `GET /api/departments` - Listar departamentos
- `GET /api/departments/{id}` - Obtener departamento
- `POST /api/departments` - Crear departamento

### Usuarios
- `GET /api/users` - Listar usuarios (con filtros)
- `GET /api/users/{id}` - Obtener usuario
- `POST /api/users` - Crear usuario
- `PUT /api/users/{id}` - Actualizar usuario

## Canales de Entrada

### Email
Configurar cuentas IMAP en la base de datos:
```json
{
  "email": "support@company.com",
  "imapServer": "imap.gmail.com",
  "imapPort": 993,
  "useSsl": true,
  "smtpServer": "smtp.gmail.com",
  "smtpPort": 587
}
```

### WhatsApp
Configurar API Business:
```json
{
  "phoneNumber": "+1234567890",
  "accessToken": "whatsapp_access_token",
  "webhookSecret": "webhook_secret"
}
```

## Variables de Entorno

```env
ConnectionStrings__DefaultConnection=Host=localhost;Database=DeskIQ_Tickets;Username=postgres;Password=your_password
Redis__ConnectionString=localhost:6379
Jwt__Key=your-super-secret-jwt-key-that-is-at-least-256-bits-long
Jwt__Issuer=DeskIQ.TicketSystem
Jwt__Audience=DeskIQ.TicketSystem
Jwt__ExpireMinutes=1440
```

## Desarrollo

### Estructura de Proyectos
- **Core**: Entidades, enums, interfaces
- **Infrastructure**: EF Core, servicios externos (Email, Redis)
- **Application**: Servicios de aplicación, DTOs, validaciones
- **API**: Controllers, middleware, configuración
- **Client**: Componentes Angular, páginas, servicios

### Próximos Pasos
1. Implementar SignalR Hub para notificaciones en tiempo real
2. Crear servicios de procesamiento de emails
3. Desarrollar componentes Angular
4. Implementar Hangfire para tareas background
5. Agregar reportes y métricas
6. Configurar SLA tracking

## Licencia

MIT License - ver archivo LICENSE para detalles.
