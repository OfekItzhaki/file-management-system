# Architecture Migration Plan: WPF → React + Web API

## Overview
Migrating from WPF desktop application to React web application with ASP.NET Core Web API backend.

## Recommendation: **React** ✅
Better fit for file management systems due to:
- Superior file upload/drag-drop libraries
- Faster UI development
- Better ecosystem for file operations
- More flexible and lighter than Angular

---

## Current Architecture (WPF)
```
┌─────────────────┐
│  WPF Desktop    │
│  Presentation   │
│  (MainWindow)   │
└────────┬────────┘
         │ Direct DI
         ▼
┌─────────────────┐
│  Application    │
│  (MediatR)      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Infrastructure  │
│ (EF Core/DB)    │
└─────────────────┘
```

## New Architecture (React + Web API)
```
┌─────────────────┐
│  React Web      │
│  (Frontend)     │
└────────┬────────┘
         │ HTTP/REST API
         ▼
┌─────────────────┐
│  ASP.NET Core   │
│  Web API        │
│  (Controllers)  │
└────────┬────────┘
         │ MediatR
         ▼
┌─────────────────┐
│  Application    │
│  (CQRS/Handlers)│
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Infrastructure  │
│ (EF Core/DB)    │
└─────────────────┘
```

---

## Project Structure

### 1. **FileManagementSystem.API** (New)
**Purpose**: RESTful Web API backend
```
FileManagementSystem.API/
├── Controllers/
│   ├── FilesController.cs        # /api/files
│   ├── FoldersController.cs      # /api/folders
│   ├── SearchController.cs       # /api/search
│   └── AuthController.cs         # /api/auth
├── Middleware/
│   ├── ExceptionHandlingMiddleware.cs
│   └── AuthenticationMiddleware.cs
├── Configuration/
│   ├── DependencyInjection.cs
│   └── SwaggerConfig.cs
├── Program.cs
└── appsettings.json
```

**Key Features**:
- REST API endpoints
- JWT authentication
- File upload/download endpoints
- CORS configuration for React
- Swagger/OpenAPI documentation
- SignalR for real-time progress (optional)

### 2. **FileManagementSystem.Web** (New)
**Purpose**: React frontend application
```
FileManagementSystem.Web/
├── public/
│   └── index.html
├── src/
│   ├── components/
│   │   ├── FileList.tsx
│   │   ├── FolderTree.tsx
│   │   ├── SearchBar.tsx
│   │   ├── FileUpload.tsx
│   │   └── ProgressBar.tsx
│   ├── pages/
│   │   ├── Dashboard.tsx
│   │   └── FileDetails.tsx
│   ├── services/
│   │   ├── api.ts              # API client
│   │   ├── fileService.ts
│   │   └── authService.ts
│   ├── hooks/
│   │   ├── useFiles.ts
│   │   └── useFolders.ts
│   ├── utils/
│   │   └── formatters.ts
│   ├── App.tsx
│   └── index.tsx
├── package.json
└── tsconfig.json
```

**Key Technologies**:
- React 18+ with TypeScript
- React Router for navigation
- Axios/Fetch for API calls
- React Query/TanStack Query for data fetching
- react-dropzone for drag-drop uploads
- Material-UI or Ant Design for UI components
- Zustand or Redux Toolkit for state management

---

## Implementation Plan

### Phase 1: Create Web API Project ✅
1. Create `FileManagementSystem.API` project
2. Configure ASP.NET Core Web API
3. Set up dependency injection (reuse from Presentation)
4. Add controllers for basic CRUD operations
5. Configure CORS for React frontend
6. Add Swagger/OpenAPI

### Phase 2: Implement API Endpoints ✅
1. **FilesController**
   - `GET /api/files` - List files (with pagination)
   - `GET /api/files/{id}` - Get file details
   - `POST /api/files/upload` - Upload file
   - `DELETE /api/files/{id}` - Delete file
   - `PUT /api/files/{id}/rename` - Rename file

2. **FoldersController**
   - `GET /api/folders` - List folders (tree structure)
   - `GET /api/folders/{id}/files` - Get files in folder

3. **SearchController**
   - `GET /api/search?q=term&tags=tag1,tag2` - Search files

4. **AuthController**
   - `POST /api/auth/login` - Login
   - `POST /api/auth/logout` - Logout
   - `GET /api/auth/me` - Get current user

### Phase 3: React Frontend Setup ✅
1. Initialize React app with TypeScript
2. Set up routing (React Router)
3. Configure API client (Axios)
4. Set up state management (Zustand/Redux)
5. Add UI component library

### Phase 4: Build React Components ✅
1. **Dashboard** - Main page with file list and folder tree
2. **FileList** - Table/grid of files with pagination
3. **FolderTree** - Hierarchical folder navigation
4. **SearchBar** - Search input with filters
5. **FileUpload** - Drag-drop file upload component
6. **FileDetails** - File metadata display

### Phase 5: Integration & Testing ✅
1. Connect React to API
2. Test file upload/download
3. Test search functionality
4. Test folder navigation
5. End-to-end testing

---

## What Stays the Same ✅

### Domain Layer
- All entities (`FileItem`, `Folder`, `User`)
- Domain exceptions
- **No changes needed**

### Application Layer
- All MediatR commands/queries
- All handlers
- All validators (FluentValidation)
- DTOs (reuse for API responses)
- **Minimal changes** - adapt DTOs for JSON serialization

### Infrastructure Layer
- EF Core DbContext
- All repositories
- Services (MetadataService, StorageService)
- Authentication/Authorization services
- **No changes needed** - services work the same

---

## What Needs Changes ⚠️

### 1. Authentication
**Current**: Simple token (desktop app)
**New**: JWT tokens (web app)
- Implement JWT in `IAuthenticationService`
- Add JWT middleware in API
- Store tokens in React (localStorage/httpOnly cookies)

### 2. File Upload
**Current**: Direct file system access
**New**: HTTP multipart/form-data
- Create upload endpoint in API
- Use FormData in React
- Handle progress events

### 3. Progress Reporting
**Current**: IProgress<T> callback (direct)
**New**: SignalR or polling
- Option A: SignalR for real-time updates
- Option B: Polling `/api/tasks/{id}/status`

### 4. Dependency Injection
**Current**: WPF app startup
**New**: ASP.NET Core Program.cs
- Move DI configuration from `App.xaml.cs` to `Program.cs`
- Configure services for web API
- Add middleware pipeline

---

## Technology Stack

### Backend (API)
- ✅ ASP.NET Core 8.0 Web API
- ✅ MediatR (already in use)
- ✅ FluentValidation (already in use)
- ✅ EF Core + SQLite (already in use)
- ✅ Serilog (already in use)
- 🆕 JWT Authentication (Microsoft.AspNetCore.Authentication.JwtBearer)
- 🆕 SignalR (optional, for real-time progress)

### Frontend (React)
- 🆕 React 18+ with TypeScript
- 🆕 React Router v6
- 🆕 Axios or Fetch API
- 🆕 React Query / TanStack Query
- 🆕 react-dropzone (file uploads)
- 🆕 Material-UI or Ant Design
- 🆕 Zustand or Redux Toolkit

---

## API Endpoints Design

### Files
```
GET    /api/files                    # List files (with pagination, filters)
GET    /api/files/{id}               # Get file details
POST   /api/files/upload             # Upload file
DELETE /api/files/{id}               # Delete file
PUT    /api/files/{id}/rename        # Rename file
POST   /api/files/{id}/tags          # Add tags to file
GET    /api/files/{id}/download      # Download file
GET    /api/files/{id}/thumbnail     # Get thumbnail
```

### Folders
```
GET    /api/folders                  # Get folder tree
GET    /api/folders/{id}             # Get folder details
GET    /api/folders/{id}/files       # Get files in folder
```

### Search
```
GET    /api/search?q=term&tags=tag1,tag2&isPhoto=true
```

### Authentication
```
POST   /api/auth/login               # Login (returns JWT)
POST   /api/auth/logout              # Logout
GET    /api/auth/me                  # Get current user
```

---

## Migration Steps

### Step 1: Create API Project
```bash
dotnet new webapi -n FileManagementSystem.API
dotnet sln add FileManagementSystem.API/FileManagementSystem.API.csproj
```

### Step 2: Set Up React App
```bash
npx create-react-app FileManagementSystem.Web --template typescript
cd FileManagementSystem.Web
npm install axios react-router-dom @tanstack/react-query react-dropzone
```

### Step 3: Copy Configuration
- Move DI setup from `Presentation/App.xaml.cs` to `API/Program.cs`
- Copy `appsettings.json` structure
- Configure CORS for React

### Step 4: Create Controllers
- Create controllers that use MediatR to send commands/queries
- Map DTOs to API responses

### Step 5: Build React Components
- Start with basic layout
- Add file list and folder tree
- Implement search and upload

### Step 6: Testing
- Test API endpoints (Swagger)
- Test React components
- Integration testing

---

## Benefits of This Migration

✅ **Cross-platform**: Works on Windows, Mac, Linux, mobile browsers
✅ **Easy deployment**: Deploy API and React separately or together
✅ **Better UX**: Modern web UI, responsive design
✅ **Scalability**: API can serve multiple clients (web, mobile apps)
✅ **Maintainability**: Separation of concerns (API + Frontend)
✅ **Modern stack**: React ecosystem, TypeScript, modern tooling

---

## Estimated Effort

- **API Project**: 2-3 days
- **React Setup**: 1 day
- **React Components**: 3-4 days
- **Integration & Testing**: 2 days
- **Total**: ~8-10 days

---

## Next Steps

1. ✅ Create `FileManagementSystem.API` project
2. ✅ Set up basic API structure with controllers
3. ✅ Configure dependency injection
4. ✅ Create React app
5. ✅ Build initial components
6. ✅ Connect frontend to backend

Ready to start? Let me know and I'll begin creating the API project!
