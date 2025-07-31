# FinalProject - AI Chat Application

## Overview
This is an AI-powered chat application built with ASP.NET Core Razor Pages, using Google's Gemini AI for intelligent responses.

## Prerequisites
- .NET 8.0 SDK
- SQL Server (Local or Express)
- Valid Google Gemini API Key

## Setup Instructions

### 1. Database Configuration
1. Update the connection string in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=GoodMeal;User ID=sa;password=YOUR_PASSWORD;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

2. Run database migrations:
```bash
dotnet ef database update --project DAL --startup-project FinalProject
```

### 2. Gemini AI Configuration

#### Getting a Gemini API Key
1. Go to [Google AI Studio](https://makersuite.google.com/app/apikey)
2. Sign in with your Google account
3. Click "Create API Key"
4. Copy the generated API key

#### Configuring the API Key
1. Open `FinalProject/appsettings.json`
2. Replace `YOUR_VALID_GEMINI_API_KEY_HERE` with your actual API key:
```json
{
  "GEMINI_API_KEY": "YOUR_ACTUAL_API_KEY_HERE",
  "AI": {
    "Gemini": {
      "ApiKey": "YOUR_ACTUAL_API_KEY_HERE",
      "DefaultModel": "gemini-1.5-flash",
      "MaxTokens": 2048,
      "Temperature": 0.7
    }
  }
}
```

### 3. Running the Application

1. Build the solution:
```bash
dotnet build
```

2. Run the application:
```bash
dotnet run --project FinalProject
```

3. Navigate to `https://localhost:7XXX` (check console output for exact port)

### 4. Testing the API Connection

#### Method 1: Admin Dashboard
1. Register a new account or login as admin
2. Go to Admin Dashboard
3. Click "Test Gemini API" button
4. Check the result message

#### Method 2: Check Application Logs
Look for these messages in the console:
- ✅ `Gemini API connection validated successfully`
- ⚠️ `Gemini API connection validation failed`
- ❌ `Failed to validate Gemini API connection during startup`

## Troubleshooting

### Common Issues

#### 1. 404 Not Found Error
**Symptoms:** `Response status code does not indicate success: 404 (Not Found)`

**Solutions:**
- Verify your API key is valid and not expired
- Check if you're using the correct model name (`gemini-1.5-flash`)
- Ensure your Google Cloud project has the Generative AI API enabled

#### 2. Invalid API Key
**Symptoms:** Authentication errors or 401 responses

**Solutions:**
- Generate a new API key from Google AI Studio
- Make sure the API key is correctly copied (no extra spaces)
- Verify the API key has the necessary permissions

#### 3. Model Not Found
**Symptoms:** Model-related errors

**Solutions:**
- Use `gemini-1.5-flash` (recommended)
- Alternative models: `gemini-1.5-pro`, `gemini-pro`

### Debug Mode
To enable detailed logging for AI services, update `appsettings.json`:
```json
"Logging": {
  "LogLevel": {
    "Services.Implementations.GeminiAIService": "Debug"
  }
}
```

## Project Structure
```
FinalProject/
├── DAL/                    # Data Access Layer
│   ├── Entities/          # Database entities
│   ├── Repositories/      # Repository implementations
│   └── Data/             # DbContext
├── Services/              # Business logic layer
│   ├── Interfaces/       # Service interfaces
│   └── Implementations/  # Service implementations
└── FinalProject/         # Web application
    ├── Pages/           # Razor Pages
    └── wwwroot/        # Static files
```

## Default Accounts
- **Admin:** Username: `admin`, Password: `admin123`

## Features
- User registration and authentication
- AI-powered chat sessions
- Admin dashboard with API testing
- Session management
- Usage tracking and analytics

## API Models Supported
- `gemini-1.5-flash` (Recommended - Fast and efficient)
- `gemini-1.5-pro` (More capable but slower)
- `gemini-pro` (Legacy model)

## Support
If you encounter issues:
1. Check the application logs
2. Use the "Test Gemini API" button in Admin Dashboard
3. Verify your API key configuration
4. Check Google AI Studio for quota limits # PRN222_Final
