#!/bin/bash

echo "🌐 Testing GitHub Pages deployment locally..."
echo "📍 Navigate to http://localhost:8080/Lenia/ in your browser"
echo "Press Ctrl+C to stop the server"
echo ""

cd release/wwwroot
python3 -m http.server 8080