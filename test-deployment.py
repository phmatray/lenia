#!/usr/bin/env python3
import http.server
import socketserver
import os
import webbrowser
from threading import Timer

PORT = 8080
DIRECTORY = "release/wwwroot"

class MyHTTPRequestHandler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=DIRECTORY, **kwargs)

def open_browser():
    webbrowser.open(f'http://localhost:{PORT}/Lenia/')

print(f"🌐 Starting local server for GitHub Pages testing...")
print(f"📁 Serving directory: {DIRECTORY}")
print(f"🔗 URL: http://localhost:{PORT}/Lenia/")
print(f"Press Ctrl+C to stop the server\n")

# Open browser after 1 second
timer = Timer(1.0, open_browser)
timer.start()

with socketserver.TCPServer(("", PORT), MyHTTPRequestHandler) as httpd:
    try:
        httpd.serve_forever()
    except KeyboardInterrupt:
        print("\n🛑 Server stopped.")
        timer.cancel()