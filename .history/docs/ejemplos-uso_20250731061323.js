/**
 * 📚 Ejemplos de Uso - API Procesador OCR
 * 
 * Este archivo contiene ejemplos prácticos de cómo usar la API
 * desde JavaScript (navegador y Node.js)
 */

// ============================================================================
// CONFIGURACIÓN BÁSICA
// ============================================================================

const API_CONFIG = {
    baseUrl: 'https://procesadorocr.onrender.com',
    apiKey: 'tu-api-key-aqui',
    headers: {
        'X-API-Key': 'tu-api-key-aqui',
        'Content-Type': 'application/json'
    }
};

// ============================================================================
// FUNCIONES UTILITARIAS
// ============================================================================

/**
 * Función para manejar errores de la API
 */
function handleApiError(error) {
    console.error('Error en la API:', error);
    if (error.response) {
        console.error('Status:', error.response.status);
        console.error('Data:', error.response.data);
    }
    throw error;
}

/**
 * Función para mostrar progreso de carga
 */
function showLoading(message = 'Procesando...') {
    console.log(`🔄 ${message}`);
    // Aquí puedes agregar tu lógica de UI para mostrar loading
}

/**
 * Función para mostrar éxito
 */
function showSuccess(message) {
    console.log(`✅ ${message}`);
    // Aquí puedes agregar tu lógica de UI para mostrar éxito
}

// ============================================================================
// EJEMPLOS - CARNÉ ADUANERO
// ============================================================================

/**
 * Ejemplo 1: Procesar PNG de Carné Aduanero
 */
async function procesarCarneAduaneroPNG(file) {
    try {
        showLoading('Procesando carné aduanero...');
        
        const formData = new FormData();
        formData.append('file', file);
        
        const response = await fetch(`${API_CONFIG.baseUrl}/api/CarnetAduanero/procesar`, {
            method: 'POST',
            headers: {
                'accept': 'text/plain'
            },
            body: formData
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        showSuccess('Carné aduanero procesado exitosamente');
        
        console.log('Datos extraídos:', data);
        return data;
        
    } catch (error) {
        handleApiError(error);
    }
}

/**
 * Ejemplo 2: Procesar múltiples PDFs en lote
 */
async function procesarCarnesAduanerosLote(files) {
    try {
        showLoading(`Procesando ${files.length} archivos en lote...`);
        
        const formData = new FormData();
        files.forEach(file => {
            formData.append('files', file);
        });
        
        const response = await fetch(`${API_CONFIG.baseUrl}/api/CarnetAduanero/procesar-lote`, {
            method: 'POST',
            headers: {
                'accept': 'text/plain'
            },
            body: formData
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        showSuccess(`${data.length} carnés procesados exitosamente`);
        
        console.log('Resultados del lote:', data);
        return data;
        
    } catch (error) {
        handleApiError(error);
    }
}

/**
 * Ejemplo 3: Procesar texto OCR de carné aduanero
 */
async function procesarTextoCarneAduanero(textoOcr) {
    try {
        showLoading('Procesando texto OCR...');
        
        const response = await fetch(`${API_CONFIG.baseUrl}/api/CarnetAduanero/procesar-texto-ocr`, {
            method: 'POST',
            headers: {
                'accept': 'text/plain',
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                textoOcr: textoOcr
            })
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        showSuccess('Texto OCR procesado exitosamente');
        
        console.log('Datos extraídos del texto:', data);
        return data;
        
    } catch (error) {
        handleApiError(error);
    }
}

// ============================================================================
// EJEMPLOS - TACT ADC
// ============================================================================

/**
 * Ejemplo 4: Procesar documento TACT ADC
 */
async function procesarTactAdc(file) {
    try {
        showLoading('Procesando documento TACT ADC...');
        
        const formData = new FormData();
        formData.append('file', file);
        
        const response = await fetch(`${API_CONFIG.baseUrl}/api/TactAdc/procesar`, {
            method: 'POST',
            headers: {
                'accept': 'text/plain'
            },
            body: formData
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        showSuccess('Documento TACT ADC procesado exitosamente');
        
        console.log('Datos TACT ADC:', data);
        return data;
        
    } catch (error) {
        handleApiError(error);
    }
}

// ============================================================================
// EJEMPLOS - SELECCIÓN AFORO
// ============================================================================

/**
 * Ejemplo 5: Procesar selección aforo
 */
async function procesarSeleccionAforo(file) {
    try {
        showLoading('Procesando selección aforo...');
        
        const formData = new FormData();
        formData.append('file', file);
        
        const response = await fetch(`${API_CONFIG.baseUrl}/api/SeleccionAforo/procesar`, {
            method: 'POST',
            headers: {
                'accept': 'text/plain'
            },
            body: formData
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        showSuccess('Selección aforo procesada exitosamente');
        
        console.log('Datos selección aforo:', data);
        return data;
        
    } catch (error) {
        handleApiError(error);
    }
}

// ============================================================================
// EJEMPLOS - GUÍA DESPACHO
// ============================================================================

/**
 * Ejemplo 6: Procesar guía de despacho
 */
async function procesarGuiaDespacho(file) {
    try {
        showLoading('Procesando guía de despacho...');
        
        const formData = new FormData();
        formData.append('file', file);
        
        const response = await fetch(`${API_CONFIG.baseUrl}/api/GuiaDespacho/procesar`, {
            method: 'POST',
            headers: {
                'accept': 'text/plain'
            },
            body: formData
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        showSuccess('Guía de despacho procesada exitosamente');
        
        console.log('Datos guía despacho:', data);
        return data;
        
    } catch (error) {
        handleApiError(error);
    }
}

// ============================================================================
// EJEMPLOS - DOCUMENTO RECEPCIÓN
// ============================================================================

/**
 * Ejemplo 7: Procesar documento de recepción
 */
async function procesarDocumentoRecepcion(file) {
    try {
        showLoading('Procesando documento de recepción...');
        
        const formData = new FormData();
        formData.append('file', file);
        
        const response = await fetch(`${API_CONFIG.baseUrl}/api/DocumentoRecepcion/procesar`, {
            method: 'POST',
            headers: {
                'X-API-Key': API_CONFIG.apiKey
            },
            body: formData
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        showSuccess('Documento de recepción procesado exitosamente');
        
        console.log('Datos documento recepción:', data);
        return data;
        
    } catch (error) {
        handleApiError(error);
    }
}

/**
 * Ejemplo 8: Extraer solo campos críticos
 */
async function extraerCamposCriticosRecepcion(file) {
    try {
        showLoading('Extrayendo campos críticos...');
        
        const formData = new FormData();
        formData.append('file', file);
        
        const response = await fetch(`${API_CONFIG.baseUrl}/api/DocumentoRecepcion/campos-criticos`, {
            method: 'POST',
            headers: {
                'X-API-Key': API_CONFIG.apiKey
            },
            body: formData
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        showSuccess('Campos críticos extraídos exitosamente');
        
        console.log('Campos críticos:', data);
        return data;
        
    } catch (error) {
        handleApiError(error);
    }
}

// ============================================================================
// EJEMPLOS - COMPROBANTE TRANSACCIÓN
// ============================================================================

/**
 * Ejemplo 9: Procesar comprobante de transacción
 */
async function procesarComprobanteTransaccion(file) {
    try {
        showLoading('Procesando comprobante de transacción...');
        
        const formData = new FormData();
        formData.append('file', file);
        
        const response = await fetch(`${API_CONFIG.baseUrl}/api/ComprobanteTransaccion/procesar`, {
            method: 'POST',
            headers: {
                'X-API-Key': API_CONFIG.apiKey
            },
            body: formData
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        showSuccess('Comprobante de transacción procesado exitosamente');
        
        console.log('Datos comprobante:', data);
        return data;
        
    } catch (error) {
        handleApiError(error);
    }
}

// ============================================================================
// EJEMPLOS - DECLARACIÓN INGRESO
// ============================================================================

/**
 * Ejemplo 10: Procesar declaración de ingreso
 */
async function procesarDeclaracionIngreso(file) {
    try {
        showLoading('Procesando declaración de ingreso...');
        
        const formData = new FormData();
        formData.append('file', file);
        
        const response = await fetch(`${API_CONFIG.baseUrl}/api/DeclaracionIngreso/procesar`, {
            method: 'POST',
            headers: {
                'X-API-Key': API_CONFIG.apiKey
            },
            body: formData
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        showSuccess('Declaración de ingreso procesada exitosamente');
        
        console.log('Datos declaración:', data);
        return data;
        
    } catch (error) {
        handleApiError(error);
    }
}

// ============================================================================
// EJEMPLOS DE USO EN HTML
// ============================================================================

/**
 * Ejemplo de uso con input file en HTML
 */
function setupFileUpload() {
    // HTML necesario:
    // <input type="file" id="fileInput" accept=".png,.pdf" />
    // <button onclick="handleFileUpload()">Procesar</button>
    
    const fileInput = document.getElementById('fileInput');
    const processButton = document.getElementById('processButton');
    
    processButton.addEventListener('click', async () => {
        const file = fileInput.files[0];
        if (!file) {
            alert('Por favor selecciona un archivo');
            return;
        }
        
        try {
            // Determinar tipo de documento basado en el nombre del archivo
            const fileName = file.name.toLowerCase();
            
            if (fileName.includes('carne')) {
                await procesarCarneAduaneroPNG(file);
            } else if (fileName.includes('tact')) {
                await procesarTactAdc(file);
            } else if (fileName.includes('aforo')) {
                await procesarSeleccionAforo(file);
            } else if (fileName.includes('guia') || fileName.includes('despacho')) {
                await procesarGuiaDespacho(file);
            } else if (fileName.includes('recepcion')) {
                await procesarDocumentoRecepcion(file);
            } else if (fileName.includes('comprobante')) {
                await procesarComprobanteTransaccion(file);
            } else if (fileName.includes('declaracion')) {
                await procesarDeclaracionIngreso(file);
            } else {
                alert('Tipo de documento no reconocido');
            }
            
        } catch (error) {
            console.error('Error procesando archivo:', error);
            alert('Error procesando el archivo');
        }
    });
}

// ============================================================================
// EJEMPLOS PARA NODE.JS
// ============================================================================

/**
 * Ejemplo para Node.js usando fetch
 * Requiere: npm install node-fetch
 */
async function procesarArchivoNodeJS(filePath) {
    const fetch = require('node-fetch');
    const fs = require('fs');
    const FormData = require('form-data');
    
    try {
        showLoading('Procesando archivo desde Node.js...');
        
        const formData = new FormData();
        formData.append('file', fs.createReadStream(filePath));
        
        const response = await fetch(`${API_CONFIG.baseUrl}/api/CarnetAduanero/procesar`, {
            method: 'POST',
            headers: {
                'X-API-Key': API_CONFIG.apiKey,
                ...formData.getHeaders()
            },
            body: formData
        });
        
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        
        const data = await response.json();
        showSuccess('Archivo procesado exitosamente desde Node.js');
        
        console.log('Datos:', data);
        return data;
        
    } catch (error) {
        handleApiError(error);
    }
}

// ============================================================================
// EXPORTAR FUNCIONES (para módulos ES6)
// ============================================================================

if (typeof module !== 'undefined' && module.exports) {
    // Node.js
    module.exports = {
        procesarCarneAduaneroPNG,
        procesarCarnesAduanerosLote,
        procesarTextoCarneAduanero,
        procesarTactAdc,
        procesarSeleccionAforo,
        procesarGuiaDespacho,
        procesarDocumentoRecepcion,
        extraerCamposCriticosRecepcion,
        procesarComprobanteTransaccion,
        procesarDeclaracionIngreso,
        setupFileUpload,
        procesarArchivoNodeJS
    };
} else {
    // Navegador - hacer funciones globales
    window.APIExamples = {
        procesarCarneAduaneroPNG,
        procesarCarnesAduanerosLote,
        procesarTextoCarneAduanero,
        procesarTactAdc,
        procesarSeleccionAforo,
        procesarGuiaDespacho,
        procesarDocumentoRecepcion,
        extraerCamposCriticosRecepcion,
        procesarComprobanteTransaccion,
        procesarDeclaracionIngreso,
        setupFileUpload
    };
}

// ============================================================================
// INSTRUCCIONES DE USO
// ============================================================================

console.log(`
📚 EJEMPLOS DE USO - API PROCESADOR OCR
=======================================

Para usar estos ejemplos:

1. CONFIGURAR API:
   - Edita API_CONFIG.baseUrl con tu URL de Render
   - Edita API_CONFIG.apiKey con tu API key

2. EN NAVEGADOR:
   - Incluye este archivo en tu HTML
   - Usa las funciones globales: window.APIExamples.funcion()

3. EN NODE.JS:
   - npm install node-fetch form-data
   - const { funcion } = require('./ejemplos-uso.js')

4. EJEMPLOS DISPONIBLES:
   - procesarCarneAduaneroPNG(file)
   - procesarCarnesAduanerosLote(files)
   - procesarTextoCarneAduanero(texto)
   - procesarTactAdc(file)
   - procesarSeleccionAforo(file)
   - procesarGuiaDespacho(file)
   - procesarDocumentoRecepcion(file)
   - extraerCamposCriticosRecepcion(file)
   - procesarComprobanteTransaccion(file)
   - procesarDeclaracionIngreso(file)

¡Listo para usar! 🚀
`); 