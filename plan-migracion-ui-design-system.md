# Plan de Integración y Migración de la librería `ui-design-system` en DeskIQ

## 1. Descripción del Problema

**Meta:**  
Centralizar el sistema visual de DeskIQ integrando la librería Angular `ui-design-system`, eliminando redundancias, asegurando uniformidad, accesibilidad, mantenibilidad y cumplimiento con las políticas de calidad y auditoría internas.

**Suposiciones y riesgos identificados:**
- DeskIQ contiene estilos globales similares a los del design system, lo cual puede generar duplicidades.
- Algunos componentes podrían seguir usando estilos legacy y no las nuevas directivas/utilidades.
- Integración debe ser compatible y no disruptiva con TailwindCSS y la estructura de Angular 21.x.
- La migración debe mantener todos los flujos UI accesibles y auditables.

---

## 2. Solución Propuesta

**Estrategia de integración:**
- Instalar `ui-design-system` como dependencia local o desde registro privado (según necesidad).
- Importar el SCSS global del design system y refactorizar/eliminar solapamientos con los estilos actuales.
- Migrar componentes relevantes a las nuevas directivas (`ButtonDirective`) y servicios (`ThemeService`).
- Validar todas las rutas, botones y elementos visuales para que sean accesibles mediante la navegación estándar.
- Alinear la integración a las políticas de versionado, estructura de carpetas y pipelines CI/CD actuales.

---

## 3. Arquitectura y Consideraciones Técnicas

### Componentes afectados:

- **ui-design-system (librería Angular):**
    - Estilos SCSS globales (tokens, utilidades, dark/light).
    - Directiva `ButtonDirective`.
    - Servicio `ThemeService` (gestión de modo light/dark/system).

- **DeskIQ (cliente Angular):**
    - Entrada global de estilos (`src/styles.css` o `src/styles.scss`).
    - Componentes UI (botones, tarjetas, formularios, etc.).
    - Lógica de gestión de temas y persistencia.

### Flujo de datos y dependencias:

- `ThemeService` define y propaga modo visual al árbol de componentes.
- Las directivas exponen lógica y estilos unificados a nivel de template.
- La importación de los estilos afecta a todo el DOM de la aplicación, por lo que requiere evaluación de colisiones y redundancias.

### Estrategia de convivencia y transición:

- Realizar auditoría inicial de estilos actuales vs design system.
- Migración progresiva de componentes legacy hacia la nueva API/directiva.
- Monitorear visualmente y con pruebas la correcta adopción e identificación de regresiones.

---

## 4. Guía de Implementación

### a. Auditoría y alineación de estilos
- Comparar tokens, clases y utilidades ya presentes en `styles.css` con los del SCSS del design system.
- Eliminar o refactorizar duplicados para evitar sobre-especificidad o inconsistencia visual.

### b. Integración de la dependencia
1. `ui-design-system` se instala desde GitHub usando git+https:
   ```json
   "ui-design-system": "git+https://github.com/Arturo-Manzo/ui-design-system.git"
   ```
2. El repositorio de GitHub tiene:
   - `private: false` en package.json
   - Script `prepare` que ejecuta `npm run build` automáticamente
   - Script `prepare` copia los archivos compilados a la raíz del paquete
3. npm clona el repositorio, ejecuta el script prepare (build), y copia los archivos necesarios

**Nota**: No hay copia local de ui-design-system en el repo DeskIQ. Se instala directamente desde GitHub.

### c. Importación de estilos globales
- En `src/styles.css` o `src/styles.scss`, agregar:
  ```scss
  @import 'ui-design-system/styles/ui-design-system.scss';
  ```
- Verificar que la build de Angular resuelva correctamente el path.

### d. Uso de las utilidades y directivas del Design System
- Migrar botones clave para utilizar la directiva:
  ```ts
  import { ButtonDirective } from 'ui-design-system';
  ```
  Y en plantilla:
  ```html
  <button appButton buttonSize="md">Confirmar</button>
  ```
- Inyectar `ThemeService` en el componente raíz para gestión de tema y aplicar lógica de inicialización:
  ```ts
  import { ThemeService } from 'ui-design-system';

  constructor(private themeService: ThemeService) {}
  ngOnInit() {
    this.themeService.init();
  }
  ```

### e. Refactor progresivo y validación de flows críticos
- Actualizar componentes UI (tarjetas, inputs, feedbacks, KPIs, tablas) para adoptar clases/utilidades del design system.
- Validar el acceso por navegación natural a todos los flujos importantes (`listado`, `creación`, `edición`, `eliminación`).
- Utilizar pruebas visuales/manuales y suites automatizadas para garantizar la cobertura.

---

## 5. Git, Docker y Pipeline

- Crear una rama específica feature/[nombre]-ui-design-system.
- Realizar commits atómicos, claros, documentando cambios por cada integración/refactor crítico.
- Garantizar que todas las builds Docker e integración continua compilen exitosamente luego de agregar la dependencia.
- Versionar y actualizar la librería cuando haya mejoras en el core visual.

---

## 6. Seguridad y Performance

- Validar que los cambios no introduzcan vulnerabilidades (p. ej., manipulación insegura del DOM mediante servicios de tema).
- Auditar el impacto en el tamaño del bundle y la eficiencia del renderizado.
- Priorizar el tree-shaking y reducción de redundancias después de la migración.

---

## 7. Edge Cases y Riesgos

- Colisión de variables CSS si existen definiciones similares.
- Componentes que requieran refactor mayor si dependían de estilos profundos legacy.
- Navegadores que no soporten completamente Custom Props (bajo riesgo en stack actual).

---

## 8. Checklist de Migración

- [ ] Auditar y comparar tokens/clases/estilos existentes vs SCSS ui-design-system.
- [ ] Agregar dependencia ui-design-system al proyecto y validar instalación.
- [ ] Importar SCSS global y limpiar duplicidades en styles propios.
- [ ] Migrar los principales botones a `ButtonDirective`.
- [ ] Inyectar y validar `ThemeService` en el componente raíz.
- [ ] Refactorizar componentes clave (card/form/table/feedback) para adoptar utilidades del design system.
- [ ] Confirmar accesibilidad e integración UI por navegación de usuario estándar.
- [ ] Validar con pruebas visuales y unitarias la correcta adopción.
- [ ] Documentar cada avance según la política de auditoría (con evidencia, references y screenshots cuando aplique).

---

## 9. Validaciones post-migración

- Asegurar que ningún ID de backend ni campo técnico irrelevante al usuario sea mostrado en la UI (cumplimiento sección 109-124 de auditoría).
- Documentar (por feature) la existencia y accesibilidad desde el menú principal, siguiendo el formato exigido.

---

## 10. Actualización a nuevas versiones

Para actualizar ui-design-system a una nueva versión desde GitHub:

1. Limpiar el caché de npm y reinstalar:
   ```bash
   cd src/deskiq-client
   npm cache clean --force
   npm uninstall ui-design-system
   npm install git+https://github.com/Arturo-Manzo/ui-design-system.git#vX.Y.Z
   ```

   O para la última versión:
   ```bash
   npm install git+https://github.com/Arturo-Manzo/ui-design-system.git
   ```

2. Validar que la build funciona correctamente:
   ```bash
   npm run build
   ```

**Nota**: npm clona el repositorio, ejecuta el script prepare (build), y copia los archivos compilados automáticamente. No se requiere intervención manual.

---

## 11. Mejoras Futuras

- Completar migración progresiva de todos los componentes personalizados.
- Automatizar validaciones de accesibilidad y consistencia visual.
- Llevar registro periódico de upgrades/actualizaciones del design system en DeskIQ y otras apps consumidoras.

---

**Referencias:**
- `.github/agents/audit.md` – Lineamientos de implementación, validación y evidencia.
- `ui-design-system/projects/ui-design-system/README.md` – Detalle de instalación y uso.
- `src/deskiq-client/styles.css` y componentes Angular – Áreas de afectación principal.

---

_Fin del documento de planificación. No realizar cambios en código hasta el visto bueno de esta propuesta._
