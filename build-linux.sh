#!/bin/bash

# Script de build pour Linux
# Génère un exécutable portable pour Linux x64

echo "🔄 Construction de l'application pour Linux..."

# Nettoyage des builds précédents
rm -rf bin/Release

# Build pour Linux x64
dotnet publish 0900_OdywardRoleManager.csproj -c Release -r linux-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:UseAppHost=true

if [ $? -eq 0 ]; then
    echo "✅ Build Linux terminé avec succès !"
    echo "📦 Exécutable disponible dans: bin/Release/net8.0/linux-x64/publish/"
    
    # Afficher la taille du fichier
    exe_file="bin/Release/net8.0/linux-x64/publish/0900_OdywardRoleManager"
    if [ -f "$exe_file" ]; then
        size=$(du -h "$exe_file" | cut -f1)
        echo "📏 Taille de l'exécutable: $size"
        
        # S'assurer que le fichier est exécutable
        chmod +x "$exe_file"
        echo "🔧 Permissions d'exécution ajoutées"
    fi
else
    echo "❌ Erreur lors du build Linux"
    exit 1
fi