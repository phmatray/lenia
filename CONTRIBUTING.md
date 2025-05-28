# Contributing to Lenia

Thank you for your interest in contributing to the Lenia project! This document provides guidelines and information for contributors.

## 🤝 Code of Conduct

This project adheres to a code of conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

- Be respectful and inclusive
- Focus on what is best for the community
- Show empathy towards other community members
- Use welcoming and inclusive language

## 🚀 Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Git](https://git-scm.com/)
- A code editor (Visual Studio, VS Code, JetBrains Rider)

### Setting Up Development Environment

1. **Fork the repository**
   ```bash
   # Click the "Fork" button on GitHub, then clone your fork
   git clone https://github.com/YOUR_USERNAME/lenia.git
   cd lenia
   ```

2. **Add upstream remote**
   ```bash
   git remote add upstream https://github.com/ORIGINAL_OWNER/lenia.git
   ```

3. **Install dependencies**
   ```bash
   dotnet restore
   ```

4. **Run the application**
   ```bash
   cd Lenia/Lenia
   dotnet run
   ```

## 🎯 Types of Contributions

### 🐛 Bug Reports
- Use the GitHub issue tracker
- Include a clear title and description
- Provide steps to reproduce the issue
- Include browser/OS information
- Add screenshots if helpful

### ✨ Feature Requests
- Check if the feature already exists or is planned
- Explain the use case and benefits
- Provide mockups or examples if applicable
- Consider implementation complexity

### 🔧 Code Contributions
- Bug fixes
- Performance optimizations
- New Lenia patterns or algorithms
- UI/UX improvements
- Documentation updates

## 📝 Development Guidelines

### Code Style

- Follow [C# coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/inside-a-program/coding-conventions)
- Use meaningful variable and method names
- Add XML documentation for public APIs
- Keep methods focused and small
- Use async/await properly

### Blazor Specific Guidelines

- Use MudBlazor components consistently
- Follow the existing component structure
- Minimize JavaScript interop when possible
- Use proper lifecycle methods (`OnInitialized`, `OnAfterRender`)
- Handle component disposal properly

### Performance Guidelines

- Maintain 60 FPS target performance
- Profile changes that affect simulation speed
- Use `Parallel.For` for CPU-intensive operations
- Minimize memory allocations in hot paths
- Cache expensive calculations

### Project Structure

```
Lenia/
├── Lenia/                     # Server-side Blazor Web App
│   ├── Components/
│   ├── Program.cs
│   └── Lenia.csproj
├── Lenia.Client/              # Client-side Blazor WebAssembly  
│   ├── Components/
│   ├── Pages/
│   ├── Layout/
│   ├── wwwroot/
│   └── Lenia.Client.csproj
└── Tests/                     # Unit and integration tests
```

## 🔄 Pull Request Process

### Before Submitting

1. **Check existing issues/PRs** to avoid duplicates
2. **Create an issue** for significant changes to discuss approach
3. **Update documentation** if needed
4. **Add tests** for new functionality
5. **Ensure CI passes** locally

### Submitting a Pull Request

1. **Create a feature branch**
   ```bash
   git checkout -b feature/amazing-feature
   ```

2. **Make your changes**
   - Write clean, documented code
   - Follow the style guidelines
   - Add tests for new features

3. **Commit your changes**
   ```bash
   git add .
   git commit -m "Add amazing feature
   
   - Detailed description of changes
   - Any breaking changes noted
   - Fixes #issue_number"
   ```

4. **Push to your fork**
   ```bash
   git push origin feature/amazing-feature
   ```

5. **Create a Pull Request**
   - Use a clear title and description
   - Reference related issues
   - Include screenshots for UI changes
   - Add testing instructions

### Pull Request Template

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Performance improvement
- [ ] Documentation update
- [ ] Refactoring

## Testing
- [ ] Unit tests added/updated
- [ ] Manual testing performed
- [ ] Performance testing (if applicable)

## Screenshots
(If applicable)

## Breaking Changes
(List any breaking changes)
```

## 🧪 Testing

### Running Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test Tests/Lenia.Tests.csproj

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Writing Tests
- Write unit tests for new algorithms
- Test edge cases and error conditions
- Use descriptive test names
- Mock external dependencies
- Test performance-critical code

### Manual Testing Checklist
- [ ] Simulation runs at target FPS
- [ ] All UI controls work correctly
- [ ] Pattern presets load properly
- [ ] Responsive design works on mobile
- [ ] Browser compatibility (Chrome, Firefox, Safari, Edge)

## 📊 Performance Considerations

### Simulation Performance
- Profile before and after changes
- Measure FPS impact
- Test on different grid sizes
- Consider memory usage

### UI Performance
- Minimize re-renders
- Use `ShouldRender()` when appropriate
- Optimize component hierarchies
- Test on slower devices

## 🐛 Debugging

### Common Issues
- **Build failures**: Check .NET version and dependencies
- **Runtime errors**: Check browser console for JavaScript errors
- **Performance issues**: Use browser dev tools profiler
- **UI problems**: Inspect MudBlazor component usage

### Debugging Tools
- Browser DevTools
- Visual Studio debugger
- .NET CLI diagnostics
- Performance profilers

## 📚 Resources

### Documentation
- [Blazor Documentation](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [MudBlazor Components](https://mudblazor.com/components)
- [Lenia Research Papers](https://arxiv.org/abs/1812.05433)

### Learning Resources
- [Blazor University](https://blazor-university.com/)
- [.NET Documentation](https://docs.microsoft.com/en-us/dotnet/)
- [C# Programming Guide](https://docs.microsoft.com/en-us/dotnet/csharp/)

## 📞 Getting Help

- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: Questions and general discussion
- **Pull Request Comments**: Code-specific questions

## 🎉 Recognition

Contributors will be recognized in:
- GitHub contributors list
- Release notes
- Project documentation
- Special thanks in README

Thank you for contributing to Lenia! 🧬✨