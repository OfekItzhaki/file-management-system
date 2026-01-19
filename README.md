# File Management System

A production-ready C# desktop application (.NET 8) for managing files and photos using Clean Architecture. Built with WPF for the UI layer.

## Features

- **Directory Scanning**: Async directory scanning with progress reporting
- **File Upload/Organization**: Automatic file deduplication by SHA256 hash
- **Photo Metadata Extraction**: EXIF data extraction (date, GPS, camera info) via SixLabors.ImageSharp
- **Tagging & Search**: Full-text search with EF Core, tag-based filtering
- **Thumbnails**: Async thumbnail generation for photos
- **Folder Hierarchy**: TreeView navigation with hierarchical folder structure
- **Drag & Drop**: File upload via drag-and-drop
- **Delete/Rename**: File operations with undo capability (planned)
- **Modern C#**: Uses records, primary constructors, and async/await throughout

## Architecture

The solution follows Clean Architecture principles with clear separation of concerns:

```
FileManagementSystem/
├── Domain/              # Entities and domain exceptions
├── Application/         # DTOs, Commands/Queries (MediatR), Validators (FluentValidation)
├── Infrastructure/      # EF Core DbContext, Repositories, Services
├── Presentation/        # WPF UI layer
└── Tests/              # xUnit unit and integration tests
```

## Technology Stack

- **.NET 8.0** - Target framework
- **WPF** - Desktop UI framework
- **Entity Framework Core 8.0** - ORM with SQLite provider
- **MediatR 12.2.0** - CQRS pattern implementation
- **FluentValidation 11.9.0** - Command validation
- **Serilog** - Structured logging (file and console sinks)
- **SixLabors.ImageSharp 3.1.2** - Image processing and EXIF extraction
- **xUnit + Moq** - Unit testing framework

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code with C# extension

### Building the Solution

```bash
# Restore packages
dotnet restore

# Build the solution
dotnet build

# Or build a specific project
dotnet build FileManagementSystem.Presentation/FileManagementSystem.Presentation.csproj
```

### Running the Application

```bash
# Run from the Presentation project
cd FileManagementSystem.Presentation
dotnet run

# Or build and run the executable
dotnet build -c Release
.\bin\Release\net8.0-windows\FileManagementSystem.Presentation.exe
```

### Database Setup

The application uses SQLite and will automatically create the database on first run. The database file (`filemanager.db`) will be created in the output directory.

To create or update migrations:

```bash
# Navigate to Infrastructure project
cd FileManagementSystem.Infrastructure

# Create a new migration
dotnet ef migrations add InitialCreate --startup-project ../FileManagementSystem.Presentation

# Update the database
dotnet ef database update --startup-project ../FileManagementSystem.Presentation
```

## Configuration

Configuration is managed via `appsettings.json` in the Presentation project:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=filemanager.db"
  },
  "ThumbnailSettings": {
    "MaxWidth": 200,
    "MaxHeight": 200
  },
  "Serilog": {
    // Logging configuration
  }
}
```

## Usage

### Scanning a Directory

1. Click **File > Scan Directory...**
2. Select a directory to scan
3. Monitor progress in the progress bar
4. Files are automatically indexed and duplicates are detected

### Searching Files

- Type in the search box and press Enter or click Search
- Use "Photos Only" checkbox to filter by photo files
- Click Clear to reset the search

### Managing Files

- **Upload**: File > Upload File... or drag-and-drop files into the window
- **Rename**: Select a file and click Edit > Rename
- **Delete**: Select a file and click Edit > Delete (moves to recycle bin)

### Navigating Folders

- Use the folder tree on the left to navigate directories
- Click on a folder to see its files in the main list view

## Project Structure

### Domain Layer
- `Entities/`: FileItem, Folder entities
- `Exceptions/`: Domain-specific exceptions (FileDuplicateException, etc.)

### Application Layer
- `Commands/`: MediatR commands (ScanDirectoryCommand, UploadFileCommand, etc.)
- `Queries/`: MediatR queries (SearchFilesQuery, GetFoldersQuery, etc.)
- `DTOs/`: Data transfer objects for UI
- `Handlers/`: Command and query handlers
- `Validators/`: FluentValidation validators
- `Interfaces/`: Repository and service interfaces

### Infrastructure Layer
- `Data/`: AppDbContext and EF Core configuration
- `Repositories/`: Repository implementations (FileRepository, FolderRepository, UnitOfWork)
- `Services/`: MetadataService (EXIF extraction), StorageService (file operations, hashing)

### Presentation Layer
- `MainWindow.xaml/cs`: Main UI window
- `ViewModels/`: View models for data binding
- `App.xaml.cs`: Application startup with DI configuration

## Testing

Run tests with:

```bash
dotnet test
```

To run with coverage:

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Logging

Logs are written to:
- Console output (during development)
- `logs/filemanager.log` file (rolling daily)

Log level can be configured in `appsettings.json`.

## Best Practices Implemented

- ✅ **Async/Await**: All I/O operations are async
- ✅ **Dependency Injection**: Microsoft.Extensions.DependencyInjection throughout
- ✅ **Validation**: FluentValidation on all commands
- ✅ **Logging**: Serilog with structured logging
- ✅ **Error Handling**: Custom exceptions and try-catch with user-friendly messages
- ✅ **Security**: Path sanitization, file size/type validation, SHA256 hashing
- ✅ **Performance**: Background tasks for heavy operations, EF AsNoTracking for reads, ListView virtualization
- ✅ **Patterns**: Repository, UnitOfWork, Factory, Observer (progress updates)

## License

This project is provided as-is for demonstration purposes.

## Contributing

This is a template/production-ready application. Feel free to extend it with additional features:

- Cloud storage integration (Azure Blob, AWS S3)
- Batch operations
- Advanced search filters
- Image editing capabilities
- Export functionality
- Plugin system
