#!/bin/bash

# Build and deploy locally for testing GitHub Pages deployment

echo "🧬 Building Lenia for GitHub Pages deployment..."

# Clean previous builds
rm -rf release

# Build the project
dotnet build --configuration Release

# Publish Blazor WebAssembly
dotnet publish Lenia/Lenia.Client/Lenia.Client.csproj -c Release -o release --nologo

# Update base href for GitHub Pages
echo "📝 Updating base href for GitHub Pages..."
sed -i '' 's/<base href="\/" \/>/<base href="\/Lenia\/" \/>/g' release/wwwroot/index.html

# Create 404.html for client-side routing
cp release/wwwroot/index.html release/wwwroot/404.html

# Add .nojekyll to prevent Jekyll processing
touch release/wwwroot/.nojekyll

echo "✅ Build complete! Files are in the 'release/wwwroot' directory"
echo "🌐 To test locally, run: dotnet serve -o -p 8080 --directory release/wwwroot"