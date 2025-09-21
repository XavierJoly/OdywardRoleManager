@echo off
REM Script de build pour Windows
REM Génère un exécutable portable pour Windows x64

echo 🔄 Construction de l'application pour Windows...

REM Nettoyage des builds précédents
if exist bin\Release rmdir /s /q bin\Release

REM Build pour Windows x64
dotnet publish 0900_OdywardRoleManager.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true /p:UseAppHost=true

if %ERRORLEVEL% EQU 0 (
    echo ✅ Build Windows terminé avec succès !
    echo 📦 Exécutable disponible dans: bin\Release\net8.0\win-x64\publish\
    
    REM Afficher la taille du fichier
    if exist "bin\Release\net8.0\win-x64\publish\0900_OdywardRoleManager.exe" (
        for %%I in ("bin\Release\net8.0\win-x64\publish\0900_OdywardRoleManager.exe") do echo 📏 Taille de l'exécutable: %%~zI bytes
    )
) else (
    echo ❌ Erreur lors du build Windows
    exit /b 1
)

pause