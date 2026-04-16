# Multi-User Server Architecture for MonoGame Online Compiler VB

## Overview

This document describes the enhanced multi-user server architecture that allows multiple users to work on their own MonoGame projects independently, preventing conflicts and ensuring data isolation.

## Architecture Components

### 1. User Management System

**Purpose**: Provide secure user authentication and project ownership.

**Components**:
- **User Model**: Stores user credentials and metadata
- **Authentication**: BCrypt password hashing for security
- **Session Management**: Track user sessions and login state

**Key Features**:
- User registration and login
- Unique username and email validation
- Password hashing with BCrypt
- Session tracking

### 2. Project Management System

**Purpose**: Enable users to create, manage, and organize their game projects.

**Components**:
- **GameProject Model**: Stores project metadata and code
- **Project Isolation**: Each user's projects are completely isolated
- **Version Control**: Track creation and modification timestamps

**Key Features**:
- Create, update, delete projects
- Project ownership validation
- Automatic timestamp tracking
- Project listing per user

### 3. Asset Management System

**Purpose**: Manage game assets (images, sounds, etc.) with proper isolation.

**Components**:
- **GameAsset Model**: Store asset metadata and file paths
- **User-Specific Storage**: Separate directories for each user's assets
- **File Management**: Handle file uploads, storage, and deletion

**Key Features**:
- Secure file upload per project
- User-specific asset directories
- Automatic file cleanup on project deletion
- Asset metadata tracking

### 4. Enhanced Compilation Service

**Purpose**: Compile user projects with proper isolation and resource management.

**Components**:
- **EnhancedMonoGameCompilerService**: Multi-threaded compilation with concurrency control
- **Semaphore-based Limiting**: Prevent server overload with concurrent compilation limits
- **Isolated Build Environments**: Each compilation runs in its own temporary directory

**Key Features**:
- Concurrent compilation limiting (max 2 simultaneous compilations)
- User-specific build directories
- Automatic cleanup of temporary files
- Compilation history tracking
- Storage usage monitoring

### 5. Database System

**Purpose**: Persist all user data, projects, and compilation history.

**Components**:
- **SQLite Database**: Lightweight, file-based database
- **Entity Framework Core**: ORM for database operations
- **Relationship Management**: Proper foreign key relationships

**Key Features**:
- Automatic database initialization
- Cascade delete for related entities
- Unique constraints for data integrity
- Query optimization with indexes

## Directory Structure

```
webapp/
├── CompiledGames/              # Compiled game outputs (isolated by user/project)
│   ├── game_{userId}_{projectId}_{sessionId}/
│   │   ├── index.html
│   │   ├── _framework/
│   │   └── ...
├── UserAssets/                 # User-specific asset storage
│   ├── {userId}/
│   │   ├── {projectId}/
│   │   │   ├── asset1.png
│   │   │   ├── sound1.wav
│   │   │   └── ...
├── Models/                     # Data models
│   └── UserModels.cs
├── Services/                   # Business logic services
│   ├── AppDbContext.cs
│   ├── UserService.cs
│   ├── MonoGameCompilerService.cs
│   └── EnhancedMonoGameCompilerService.cs
├── Controllers/                # API endpoints
│   ├── AuthController.cs
│   ├── ProjectController.cs
│   ├── MonoGameController.cs
│   └── EnhancedMonoGameController.cs
└── monogame_compiler.db        # SQLite database file
```

## API Endpoints

### Authentication

#### Register User
```
POST /api/auth/register
Content-Type: application/json

{
  "username": "johndoe",
  "email": "john@example.com",
  "password": "securepassword123"
}

Response:
{
  "success": true,
  "userId": 1,
  "username": "johndoe",
  "message": "Registration successful"
}
```

#### Login
```
POST /api/auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "securepassword123"
}

Response:
{
  "success": true,
  "userId": 1,
  "username": "johndoe",
  "message": "Login successful"
}
```

### Project Management

#### Get User Projects
```
GET /api/project/list?userId=1

Response: [
  {
    "id": 1,
    "name": "My First Game",
    "vbCode": "...",
    "createdAt": "2024-01-15T10:30:00Z",
    "updatedAt": "2024-01-15T14:20:00Z",
    "userId": 1,
    "assets": [],
    "compilationSessions": []
  }
]
```

#### Create Project
```
POST /api/project/create
Content-Type: application/json

{
  "userId": 1,
  "name": "My First Game",
  "vbCode": "Imports Microsoft.Xna.Framework..."
}

Response: {
  "id": 1,
  "name": "My First Game",
  "vbCode": "...",
  "createdAt": "2024-01-15T10:30:00Z",
  "userId": 1
}
```

#### Update Project
```
PUT /api/project/{projectId}?userId=1
Content-Type: application/json

{
  "userId": 1,
  "name": "Updated Game Name",
  "vbCode": "Imports Microsoft.Xna.Framework..."
}

Response: {
  "id": 1,
  "name": "Updated Game Name",
  "vbCode": "...",
  "updatedAt": "2024-01-15T14:20:00Z",
  "userId": 1
}
```

#### Delete Project
```
DELETE /api/project/{projectId}?userId=1

Response: {
  "message": "Project deleted successfully"
}
```

### Asset Management

#### Add Asset
```
POST /api/project/{projectId}/assets?userId=1
Content-Type: multipart/form-data

file: [binary file data]

Response: {
  "id": 1,
  "fileName": "abc123-sprite.png",
  "filePath": "...",
  "fileSize": 12345,
  "contentType": "image/png",
  "gameProjectId": 1
}
```

#### Delete Asset
```
DELETE /api/project/assets/{assetId}?userId=1

Response: {
  "message": "Asset deleted successfully"
}
```

### Compilation

#### Compile Project
```
POST /api/monogame/compile
Content-Type: application/json

{
  "projectId": 1,
  "userId": 1
}

Response: {
  "success": true,
  "gameId": "game_1_1_abc123...",
  "gameUrl": "/games/game_1_1_abc123.../index.html",
  "message": "Game compiled successfully!"
}
```

#### Compile with New Assets
```
POST /api/monogame/compile-with-new-assets?projectId=1&userId=1
Content-Type: multipart/form-data

files: [binary file data...]

Response: {
  "success": true,
  "gameId": "game_1_1_abc123...",
  "gameUrl": "/games/game_1_1_abc123.../index.html",
  "message": "Game compiled successfully!"
}
```

#### Get Game Status
```
GET /api/monogame/status/{gameId}

Response: {
  "gameId": "game_1_1_abc123...",
  "status": "Ready",
  "gameUrl": "/games/game_1_1_abc123.../index.html"
}
```

### System Management

#### Get Storage Usage
```
GET /api/monogame/storage

Response: {
  "CompiledGames": 123456789,
  "UserAssets": 98765432,
  "Total": 222222221
}
```

#### Cleanup Old Games
```
POST /api/monogame/cleanup?daysOld=7

Response: {
  "message": "Cleanup completed successfully. Removed games older than 7 days."
}
```

## Security Features

### 1. User Isolation
- Each user can only access their own projects and assets
- All API endpoints validate user ownership
- File system paths are user-specific

### 2. Password Security
- BCrypt hashing for all passwords
- No plain text password storage
- Secure authentication flow

### 3. File System Security
- User-specific directories prevent cross-user access
- Asset files stored with unique identifiers
- Automatic cleanup of orphaned files

### 4. Resource Management
- Concurrent compilation limits prevent server overload
- Automatic cleanup of temporary build files
- Storage usage monitoring

## Migration from Single-User to Multi-User

### Step 1: Database Migration
The new system uses SQLite with Entity Framework Core. The database is automatically created on first run.

### Step 2: Update Client Code
Replace the old compilation endpoints with the new multi-user endpoints:

**Old:**
```javascript
POST /api/monogame/compile
{
  "vbCode": "...",
  "sessionId": "..."
}
```

**New:**
```javascript
POST /api/monogame/compile
{
  "projectId": 1,
  "userId": 1
}
```

### Step 3: Add Authentication
Implement user registration and login in your frontend application.

### Step 4: Update Project Management
Use the new project management endpoints to create and manage user projects.

## Performance Considerations

### 1. Compilation Performance
- Concurrent compilations limited to 2 to prevent server overload
- Each compilation runs in isolated environment
- Automatic cleanup prevents disk space issues

### 2. Database Performance
- SQLite provides good performance for moderate user loads
- Indexed fields for fast queries
- Efficient relationship loading with Entity Framework

### 3. File System Performance
- User-specific directories reduce contention
- Temporary files cleaned up automatically
- Storage usage monitoring prevents disk full issues

## Scalability Options

### 1. Database Scaling
For higher loads, consider migrating from SQLite to:
- PostgreSQL
- SQL Server
- MySQL

### 2. File Storage Scaling
For distributed deployments, consider:
- Cloud storage (Azure Blob Storage, AWS S3)
- CDN for compiled game files
- Separate file servers

### 3. Compilation Scaling
For high-volume compilation, consider:
- Docker containers for isolated compilation
- Queue-based compilation system
- Distributed compilation across multiple servers

## Monitoring and Maintenance

### 1. Storage Monitoring
Use the `/api/monogame/storage` endpoint to monitor storage usage.

### 2. Cleanup Tasks
Implement regular cleanup of old compiled games using `/api/monogame/cleanup`.

### 3. Database Backup
Regular backups of the SQLite database file are recommended.

## Troubleshooting

### Issue: Compilation fails with "Project not found"
**Solution**: Verify that the user owns the project and the project ID is correct.

### Issue: Assets not appearing in compiled game
**Solution**: Ensure assets are uploaded before compilation and check the compilation logs.

### Issue: Storage space running low
**Solution**: Run the cleanup endpoint to remove old compiled games.

### Issue: Database locked errors
**Solution**: Ensure only one instance of the application is running, or migrate to a server-based database.

## Future Enhancements

1. **JWT Authentication**: Replace simple authentication with JWT tokens
2. **Real-time Updates**: Use SignalR for real-time compilation status
3. **Project Templates**: Provide starter templates for different game types
4. **Collaboration Features**: Allow multiple users to collaborate on projects
5. **Version Control**: Integrate Git for project version management
6. **Cloud Deployment**: Support for cloud-based compilation and storage

## Conclusion

This multi-user architecture provides a robust, secure, and scalable foundation for the MonoGame Online Compiler VB. It ensures that each user's projects are properly isolated while providing efficient resource management and comprehensive API endpoints for all operations.