# 📚 Documentación de la API - Procesador OCR

Esta carpeta contiene la documentación completa y moderna de la API del Procesador OCR.

## 📁 Archivos Incluidos

- **`api-documentation.html`** - Documentación principal en HTML
- **`styles.css`** - Estilos CSS modernos y responsivos
- **`README.md`** - Este archivo de instrucciones

## 🚀 Cómo Usar la Documentación

### 1. Abrir la Documentación
Simplemente abre el archivo `api-documentation.html` en tu navegador web preferido.

### 2. Navegación
- **Sidebar izquierdo**: Navegación rápida entre secciones
- **Header**: Información general de la API
- **Secciones**: Cada tipo de documento tiene su propia sección

### 3. Características Interactivas

#### 📋 Copiar Código
- **Click en cualquier bloque de código** para copiarlo al portapapeles
- Aparece feedback visual "Copiado!" cuando se copia exitosamente

#### 🎯 Navegación Suave
- **Click en enlaces del sidebar** para navegar suavemente a las secciones
- **Resaltado automático** de la sección actual en el sidebar

#### 📱 Responsive Design
- **Desktop**: Layout completo con sidebar fijo
- **Tablet**: Sidebar adaptativo
- **Mobile**: Layout vertical optimizado

## 📖 Secciones de la Documentación

### 🔧 Información General
- URL Base de la API
- Autenticación por API Key
- Límites de archivos
- Códigos de respuesta HTTP

### 📄 Documentos Soportados

#### 1. **Carné Aduanero** (`/api/CarnetAduanero/`)
- `POST /procesar` - Procesar PNG de carné
- `POST /procesar-lote` - Procesar múltiples PDFs
- `POST /procesar-texto-ocr` - Procesar texto OCR

#### 2. **TACT ADC** (`/api/TactAdc/`)
- `POST /procesar` - Procesar PNG de TACT ADC
- `POST /procesar-texto` - Procesar texto OCR

#### 3. **Selección Aforo** (`/api/SeleccionAforo/`)
- `POST /procesar` - Procesar PNG de selección aforo
- `POST /procesar-lote` - Procesar múltiples PDFs

#### 4. **Guía Despacho** (`/api/GuiaDespacho/`)
- `POST /procesar` - Procesar PNG de guía despacho
- `POST /procesar-lote` - Procesar múltiples PDFs

#### 5. **Documento Recepción** (`/api/DocumentoRecepcion/`)
- `POST /procesar` - Procesar PNG de documento recepción
- `POST /campos-criticos` - Extraer solo campos críticos

#### 6. **Comprobante Transacción** (`/api/ComprobanteTransaccion/`)
- `POST /procesar` - Procesar PNG de comprobante
- `POST /procesar-texto` - Procesar texto OCR

#### 7. **Declaración Ingreso** (`/api/DeclaracionIngreso/`)
- `POST /procesar` - Procesar PNG de declaración
- `POST /campos-criticos` - Extraer campos críticos
- `POST /campos-criticos-multiple` - Múltiples documentos

## 🎨 Características del Diseño

### ✨ Moderno y Profesional
- **Gradientes** en el header
- **Sombras suaves** en las tarjetas
- **Animaciones** sutiles en hover
- **Iconos** de Font Awesome

### 🎯 UX Optimizada
- **Navegación intuitiva** con sidebar
- **Feedback visual** en interacciones
- **Código copiable** con un click
- **Responsive** para todos los dispositivos

### 🌈 Esquema de Colores
- **Azul primario**: `#3b82f6`
- **Grises**: Escala de `#f8fafc` a `#1e293b`
- **Verde éxito**: `#4ade80`
- **Amarillo warning**: `#fbbf24`
- **Rojo error**: `#f87171`

## 🔧 Personalización

### Cambiar URL Base
Edita la línea en `api-documentation.html`:
```html
<code>https://tu-dominio-render.onrender.com</code>
```

### Cambiar Colores
Modifica las variables CSS en `styles.css`:
```css
:root {
    --primary-color: #3b82f6;
    --success-color: #4ade80;
    --warning-color: #fbbf24;
    --error-color: #f87171;
}
```

### Agregar Nuevos Endpoints
1. Copia una sección existente en `api-documentation.html`
2. Actualiza el título, descripción y endpoints
3. Agrega el enlace en el sidebar

## 📱 Compatibilidad

### Navegadores Soportados
- ✅ Chrome 90+
- ✅ Firefox 88+
- ✅ Safari 14+
- ✅ Edge 90+

### Dispositivos
- ✅ Desktop (1920px+)
- ✅ Laptop (1024px+)
- ✅ Tablet (768px+)
- ✅ Mobile (480px+)

## 🚀 Despliegue

### Opción 1: GitHub Pages
1. Sube los archivos a un repositorio GitHub
2. Activa GitHub Pages en Settings
3. La documentación estará disponible en `https://usuario.github.io/repo/`

### Opción 2: Servidor Web
1. Sube los archivos a tu servidor web
2. Accede via `https://tu-dominio.com/docs/`

### Opción 3: Render/Netlify
1. Conecta tu repositorio
2. Configura el build para servir archivos estáticos
3. Despliega automáticamente

## 📞 Soporte

Si necesitas ayuda con la documentación:

1. **Revisa este README** para instrucciones básicas
2. **Inspecciona el código** HTML/CSS para personalizaciones
3. **Contacta al equipo** de desarrollo para cambios mayores

## 🔄 Actualizaciones

### Versión 1.0
- ✅ Documentación completa de todos los endpoints
- ✅ Diseño moderno y responsivo
- ✅ Funcionalidades interactivas
- ✅ Compatibilidad multiplataforma

### Próximas Versiones
- 🔄 Ejemplos en JavaScript/TypeScript
- 🔄 Swagger/OpenAPI integration
- 🔄 Búsqueda en tiempo real
- 🔄 Modo oscuro

---

**¡Disfruta usando la documentación de la API!** 🎉 