# Deployment Guide

This guide covers various deployment options for the Lenia Blazor WebAssembly application.

## 🚀 Quick Deploy Options

### GitHub Pages (Recommended)
```bash
# Automatic deployment via GitHub Actions
# Just push to main branch - deployment is automated!
git add .
git commit -m "Deploy to GitHub Pages"
git push origin main
```

### Netlify
1. Connect your GitHub repository to Netlify
2. Set build command: `dotnet publish Lenia/Lenia/Lenia.csproj -c Release`
3. Set publish directory: `Lenia/Lenia/bin/Release/net9.0/publish/wwwroot`
4. Deploy automatically on push

### Vercel
1. Install Vercel CLI: `npm i -g vercel`
2. Run `vercel` in project root
3. Follow prompts for deployment

## 🏗️ Build Process

### Local Build
```bash
# Restore dependencies
dotnet restore

# Build in release mode
dotnet build -c Release

# Publish for deployment
dotnet publish Lenia/Lenia/Lenia.csproj -c Release -o ./publish
```

### Build Output
The published output will be in:
```
./publish/wwwroot/
├── _framework/          # Blazor WebAssembly runtime
├── css/                # Stylesheets
├── js/                 # JavaScript files
├── _content/           # MudBlazor assets
├── index.html          # Main page
└── ...                 # Other static assets
```

## 🌐 GitHub Pages Deployment

### Automatic Deployment
The project includes a GitHub Actions workflow that automatically builds and deploys to GitHub Pages when you push to the main branch.

### Manual Setup
1. **Enable GitHub Pages**:
   - Go to repository Settings
   - Navigate to Pages section
   - Select "GitHub Actions" as source

2. **Configure Repository**:
   ```bash
   # Ensure the workflow file exists
   .github/workflows/build-and-deploy.yml
   ```

3. **Push to Deploy**:
   ```bash
   git push origin main
   # Deployment will start automatically
   ```

### Custom Domain
1. Add `CNAME` file to `wwwroot`:
   ```
   yourdomain.com
   ```
2. Configure DNS to point to GitHub Pages
3. Enable HTTPS in repository settings

## 🔧 Configuration for Deployment

### Base Path Configuration
For subdirectory deployments, update `wwwroot/index.html`:
```html
<base href="/your-repo-name/" />
```

### Production Optimizations
Update `Lenia.Client.csproj`:
```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <PublishTrimmed>true</PublishTrimmed>
  <TrimMode>Link</TrimMode>
  <BlazorWebAssemblyPreserveCollationData>false</BlazorWebAssemblyPreserveCollationData>
  <BlazorWebAssemblyPreserveCollationData>false</BlazorWebAssemblyPreserveCollationData>
</PropertyGroup>
```

## 🐳 Docker Deployment

### Dockerfile
```dockerfile
# Use the official .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["Lenia/Lenia/Lenia.csproj", "Lenia/Lenia/"]
COPY ["Lenia/Lenia.Client/Lenia.Client.csproj", "Lenia/Lenia.Client/"]
RUN dotnet restore "Lenia/Lenia/Lenia.csproj"

# Copy source code and build
COPY . .
WORKDIR "/src/Lenia/Lenia"
RUN dotnet build "Lenia.csproj" -c Release -o /app/build
RUN dotnet publish "Lenia.csproj" -c Release -o /app/publish

# Use nginx to serve the static files
FROM nginx:alpine
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
```

### Nginx Configuration
```nginx
events {
    worker_connections 1024;
}

http {
    include       /etc/nginx/mime.types;
    default_type  application/octet-stream;

    server {
        listen 80;
        server_name localhost;
        root /usr/share/nginx/html;
        index index.html;

        location / {
            try_files $uri $uri/ /index.html;
        }

        # Enable gzip compression
        gzip on;
        gzip_types text/plain text/css application/json application/javascript text/xml application/xml application/xml+rss text/javascript application/wasm;
    }
}
```

### Docker Commands
```bash
# Build image
docker build -t lenia .

# Run container
docker run -p 8080:80 lenia

# Access at http://localhost:8080
```

## ☁️ Cloud Deployments

### Azure Static Web Apps
1. Create Azure Static Web App resource
2. Connect to GitHub repository
3. Configure build:
   ```yaml
   app_location: "/"
   app_build_command: "dotnet publish Lenia/Lenia/Lenia.csproj -c Release"
   output_location: "Lenia/Lenia/bin/Release/net9.0/publish/wwwroot"
   ```

### AWS S3 + CloudFront
1. **Build the application**:
   ```bash
   dotnet publish Lenia/Lenia/Lenia.csproj -c Release
   ```

2. **Upload to S3**:
   ```bash
   aws s3 sync ./publish/wwwroot s3://your-bucket-name --delete
   ```

3. **Configure CloudFront** for SPA routing

### Google Cloud Storage
```bash
# Build and upload
dotnet publish Lenia/Lenia/Lenia.csproj -c Release
gsutil -m rsync -r -d ./publish/wwwroot gs://your-bucket-name
```

## 🔒 Security Considerations

### Content Security Policy
Add to `index.html`:
```html
<meta http-equiv="Content-Security-Policy" content="
  default-src 'self';
  script-src 'self' 'wasm-unsafe-eval';
  style-src 'self' 'unsafe-inline' https://fonts.googleapis.com;
  font-src 'self' https://fonts.gstatic.com;
">
```

### HTTPS Configuration
Ensure HTTPS is enabled for:
- Service worker functionality
- WebAssembly security
- Modern browser features
- SEO benefits

## 📊 Performance Optimization

### Compression
Enable gzip/brotli compression for:
- `.wasm` files
- `.dll` files  
- `.js` files
- `.css` files

### Caching Headers
```nginx
location ~* \.(wasm|dll)$ {
    expires 1y;
    add_header Cache-Control "public, immutable";
}

location ~* \.(js|css)$ {
    expires 1y;
    add_header Cache-Control "public";
}
```

### CDN Configuration
- Serve static assets from CDN
- Configure proper cache headers
- Enable HTTP/2
- Use WebP images where supported

## 🔍 Monitoring & Analytics

### Performance Monitoring
```javascript
// Add to index.html for performance tracking
window.blazorCulture = {
    get: () => window.localStorage['BlazorCulture'],
    set: (value) => window.localStorage['BlazorCulture'] = value
};

// Performance API usage
performance.mark('blazor-start');
```

### Error Tracking
Integrate with services like:
- Sentry
- Application Insights
- Google Analytics
- LogRocket

## 🧪 Testing Deployments

### Local Testing
```bash
# Serve published files locally
cd publish/wwwroot
python -m http.server 8000
# Or use live-server: npx live-server
```

### Staging Environment
- Deploy to staging branch first
- Test all functionality
- Verify performance metrics
- Check cross-browser compatibility

### Production Checklist
- [ ] HTTPS enabled
- [ ] Compression configured
- [ ] Cache headers set
- [ ] Error tracking enabled
- [ ] Performance monitoring active
- [ ] Security headers configured
- [ ] Mobile responsiveness verified
- [ ] Cross-browser testing completed

## 🔄 CI/CD Pipeline

### GitHub Actions Workflow
```yaml
name: Deploy to Production

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
    - uses: actions/checkout@v4
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'
        
    - name: Restore dependencies
      run: dotnet restore
      
    - name: Build
      run: dotnet build --configuration Release
      
    - name: Test
      run: dotnet test --configuration Release
      
    - name: Publish
      run: dotnet publish Lenia/Lenia/Lenia.csproj -c Release -o ./publish
      
    - name: Deploy to GitHub Pages
      uses: peaceiris/actions-gh-pages@v3
      with:
        github_token: ${{ secrets.GITHUB_TOKEN }}
        publish_dir: ./publish/wwwroot
```

## 🆘 Troubleshooting

### Common Issues

#### 404 Errors on Refresh
- Configure server for SPA routing
- Ensure `index.html` fallback
- Check base href configuration

#### Slow Loading
- Enable compression
- Check bundle sizes
- Verify CDN configuration
- Monitor network requests

#### WASM Loading Issues
- Verify MIME types configured
- Check Content Security Policy
- Ensure HTTPS for production
- Validate browser compatibility

### Debug Commands
```bash
# Check published file sizes
du -sh publish/wwwroot/_framework/*

# Verify compression
curl -H "Accept-Encoding: gzip" -I https://yoursite.com/_framework/blazor.webassembly.js

# Test performance
lighthouse https://yoursite.com --output html
```

## 📞 Support

For deployment issues:
- Check [GitHub Issues](https://github.com/yourusername/lenia/issues)
- Review [troubleshooting docs](https://github.com/yourusername/lenia/wiki/Troubleshooting)
- Contact maintainers via [Discussions](https://github.com/yourusername/lenia/discussions)

---

Happy deploying! 🚀