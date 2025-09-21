#!/bin/bash

# Script de build pour Windows
# Génère un exécutable portable pour Windows x64

echo "🔄 Construction de l'application pour Windows..."

# Nettoyage des builds précédents
rm -rf bin/Release

# Build pour Windows x64
dotnet publish 0900_OdywardRoleManager.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:UseAppHost=true

if [ $? -eq 0 ]; then
    echo "✅ Build Windows terminé avec succès !"
    echo "📦 Exécutable disponible dans: bin/Release/net8.0/win-x64/publish/"
    
    # Afficher la taille du fichier
    exe_file="bin/Release/net8.0/win-x64/publish/0900_OdywardRoleManager.exe"
    if [ -f "$exe_file" ]; then
        size=$(du -h "$exe_file" | cut -f1)
        echo "📏 Taille de l'exécutable: $size"
    fi
else
    echo "❌ Erreur lors du build Windows"
    exit 1
fi