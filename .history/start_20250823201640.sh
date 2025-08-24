#!/bin/bash

echo "🚀 Iniciando Procesador OCR con Ollama..."

# Iniciar Ollama en background
echo "📥 Iniciando Ollama..."
ollama serve &

# Esperar a que Ollama esté listo
echo "⏳ Esperando a que Ollama esté listo..."
sleep 15

# Verificar si el modelo Gemma 3: 4B está disponible
echo "🔍 Verificando modelo Gemma 3: 4B..."
if ! ollama list | grep -q "gemma3:4b"; then
    echo "📥 Descargando modelo Gemma 3: 4B..."
    ollama pull gemma3:4b
    echo "✅ Modelo descargado exitosamente"
else
    echo "✅ Modelo Gemma 3: 4B ya está disponible"
fi

# Verificar que Ollama esté respondiendo
echo "🔍 Verificando que Ollama esté respondiendo..."
if curl -s http://localhost:11434/api/tags > /dev/null; then
    echo "✅ Ollama está funcionando correctamente"
else
    echo "⚠️  Ollama no está respondiendo, pero continuando..."
fi

# Iniciar la aplicación .NET
echo "🚀 Iniciando aplicación .NET..."
exec dotnet CarnetAduaneroProcessor.API.dll
