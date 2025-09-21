#!/bin/bash

# Script de build pour macOS
# Génère un bundle d'application pour macOS

echo "🔄 Construction de l'application pour macOS..."

# Nettoyage des builds précédents
rm -rf bin/Release

# Build pour macOS x64
dotnet publish 0900_OdywardRoleManager.csproj -c Release -r osx-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:UseAppHost=true

if [ $? -eq 0 ]; then
    echo "✅ Build macOS terminé avec succès !"
    
    # Créer un bundle .app
    app_name="OdywardRoleManager.app"
    app_dir="bin/Release/net8.0/osx-x64/publish/$app_name"
    
    mkdir -p "$app_dir/Contents/MacOS"
    mkdir -p "$app_dir/Contents/Resources"
    
    # Copier l'exécutable
    cp "bin/Release/net8.0/osx-x64/publish/0900_OdywardRoleManager" "$app_dir/Contents/MacOS/"
    
    # Créer Info.plist
    cat > "$app_dir/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleExecutable</key>
    <string>0900_OdywardRoleManager</string>
    <key>CFBundleIconFile</key>
    <string>avalonia-logo.ico</string>
    <key>CFBundleIdentifier</key>
    <string>com.odyward.rolemanager</string>
    <key>CFBundleName</key>
    <string>Odyward Role Manager</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleSignature</key>
    <string>ORMA</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
EOF
    
    # Copier l'icône si elle existe
    if [ -f "Assets/avalonia-logo.ico" ]; then
        cp "Assets/avalonia-logo.ico" "$app_dir/Contents/Resources/"
    fi
    
    echo "📦 Bundle .app créé dans: bin/Release/net8.0/osx-x64/publish/"
    
    # Afficher la taille du bundle
    if [ -d "$app_dir" ]; then
        size=$(du -sh "$app_dir" | cut -f1)
        echo "📏 Taille du bundle: $size"
    fi
else
    echo "❌ Erreur lors du build macOS"
    exit 1
fi