# Blazor Hybrid Template Architecture

## 🎯 Goal

Create a clean, professional template for cross-platform apps using .NET MAUI + Blazor Hybrid with strict frontend/backend separation.

## 🏗️ Architecture Overview

```
Frontend (wwwroot/)     Bridge Interface     Backend (C#)
├── HTML/CSS/JS        ←→ Blazor Hybrid    ←→ Services
├── Framework-agnostic    JSInterop           Business Logic
└── Swappable for Web                         Data Access
```

## 🔧 Design Principles

1. **Clean Separation**: Frontend and backend are completely independent
2. **Framework Agnostic**: Frontend can be swapped for any framework
3. **Bridge Interface**: Minimal, well-defined communication layer
4. **No Legacy Code**: No WebView polling, iframe hacks, or HTTP polling
5. **Professional Ready**: Clean, reusable, and well-documented

## 📁 Structure

```
AppShell/
├── wwwroot/              # Frontend files
│   ├── index.html        # Main frontend entry
│   ├── styles.css        # Framework-agnostic styles
│   └── app.js            # Frontend application logic
├── Services/             # Backend business logic
├── Models/               # Shared data models
├── Components/           # Blazor components (bridge)
└── MauiProgram.cs        # App configuration
```

## 🚀 Communication

- **Blazor Hybrid**: Reliable JS-to-C# communication via JSInterop
- **Bridge Component**: Minimal Blazor component exposing backend methods
- **Type Safety**: Strongly typed interfaces between frontend and backend
- **Error Handling**: Proper exception propagation and handling

## � Current Status

✅ Legacy code removed  
✅ Clean project structure  
🔄 Blazor Hybrid bridge (next)  
🔄 Frontend bridge interface (next)  
🔄 Backend services (as needed)  

## 🎯 Next Implementation Steps

1. Create Blazor Hybrid bridge component
2. Define frontend bridge interface
3. Implement basic backend services
4. Add comprehensive documentation
5. Package as professional template

### Bridge Connection:
The WebView bridge uses URL-based message passing for true cross-platform compatibility:
- **JavaScript**: `window.MauiDataBridge` - For data operations
- **JavaScript**: `window.MauiNativeBridge` - For platform features
- **Communication**: URL navigation interception (`about:blank#maui-message:`)
- **Benefits**: Works consistently across Windows, macOS, iOS, and Android

## 🌐 Cross-Platform Implementation

### WebView Communication Method
Instead of platform-specific WebView APIs, we use a unified approach:

```javascript
// Unified message posting (works on all platforms)
window.postMauiMessage = function(message) {
    var messageJson = JSON.stringify(message);
    var encodedMessage = encodeURIComponent(messageJson);
    window.location.href = 'about:blank#maui-message:' + encodedMessage;
};
```

```csharp
// C# intercepts URL navigation on all platforms
private async void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
{
    if (e.Url.StartsWith("about:blank#maui-message:"))
    {
        e.Cancel = true; // Cancel the navigation
        var messageJson = Uri.UnescapeDataString(e.Url.Substring("about:blank#maui-message:".Length));
        await HandleWebViewMessage(messageJson);
    }
}
```

### Platform Compatibility
- ✅ **Windows**: MAUI WebView (not WebView2-specific)
- ✅ **macOS**: MAUI WebView with WKWebView backend
- ✅ **iOS**: MAUI WebView with WKWebView backend  
- ✅ **Android**: MAUI WebView with native WebView backend

## 📁 Updated Structure

```
Backend/
├── Api/DataBridge.cs         # Direct JS-to-C# method bridge
├── Services/SampleService.cs # Business logic layer  
├── Data/AppDbContext.cs      # Entity Framework context
└── Models/SampleEntity.cs    # Data models

wwwroot/
├── api.js                    # DataService for direct method calls
├── native-bridge.js          # Platform feature access
├── app.js                    # Main application logic
└── index.html               # Frontend UI
```

## 🎯 Template Usage

This template now provides:
- ✅ Complete frontend/backend separation
- ✅ Framework-agnostic frontend (can use React, Vue, etc.)
- ✅ Direct communication without HTTP
- ✅ SQLite database integration
- ✅ PowerShell build automation
- ✅ Inno Setup installer support
- ✅ Cross-platform deployment

Perfect for deployed applications where network ports would be problematic!
