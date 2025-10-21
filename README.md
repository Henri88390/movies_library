# Movies Library

A full-stack application for managing a movie library with a .NET 8 backend API and Angular 19 frontend.

## Prerequisites

Before running this project, make sure you have the following installed:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) (version 18 or higher)
- [Angular CLI](https://angular.dev/tools/cli) (optional, for easier development)

## Project Structure

```
movies_library/
├── backend/           # .NET 8 ASP.NET Core API
├── backend.Tests/     # Backend unit/integration tests
├── frontend/          # Angular 19 frontend application
└── README.md          # This file
```

## Backend Setup (.NET API)

### Installation

1. Navigate to the backend directory:

   ```bash
   cd backend
   ```

2. Restore .NET packages:

   ```bash
   dotnet restore
   ```

3. Apply database migrations (SQLite):
   ```bash
   dotnet ef database update
   ```

### Running the Backend

To start the backend API server:

```bash
cd backend
dotnet run
```

The API will be available at: `http://localhost:5176`

#### Alternative port configuration:

```bash
dotnet run --urls "http://localhost:5176"
```

### Backend Features

- RESTful API for movie management (CRUD operations)
- SQLite database with Entity Framework Core
- Data validation and DTOs
- CORS enabled for frontend communication
- Sample data seeding

### API Endpoints

- `GET /api/movies/movies` - Get all movies
- `GET /api/movies/movies/{id}` - Get movie by ID
- `POST /api/movies/movies` - Create new movie
- `PUT /api/movies/movies/{id}` - Update movie
- `PATCH /api/movies/movies/{id}` - Partially update movie
- `DELETE /api/movies/movies/{id}` - Delete movie

### Swagger Documentation

Once the backend is running, you can access the Swagger UI at:
**http://localhost:5176/swagger**

The Swagger page provides:

- Interactive API documentation
- Ability to test API endpoints directly
- Request/response schemas
- Authentication details (if applicable)

## Frontend Setup (Angular)

### Installation

1. Navigate to the frontend directory:

   ```bash
   cd frontend
   ```

2. Install npm packages:
   ```bash
   npm install
   ```

### Running the Frontend

To start the Angular development server:

```bash
cd frontend
npm start
```

Or using Angular CLI:

```bash
cd frontend
ng serve
```

The frontend will be available at: `http://localhost:4200`

### Frontend Features

- Responsive movie library interface
- Movie listing with search and filtering
- Movie details view
- Add/Edit/Delete movies functionality
- Modern Angular 19 with standalone components

## Running Tests

### Backend Tests

To run the .NET backend tests:

```bash
cd backend.Tests
dotnet test
```

For detailed test output:

```bash
cd backend.Tests
dotnet test --verbosity normal
```

For test coverage:

```bash
cd backend.Tests
dotnet test --collect:"XPlat Code Coverage"
```

### Frontend Tests

To run Angular unit tests:

```bash
cd frontend
npm test
```

Or using Angular CLI:

```bash
cd frontend
ng test
```

To run tests in CI mode (single run):

```bash
cd frontend
ng test --watch=false --browsers=ChromeHeadless
```

## Development Workflow

### Full Stack Development

1. **Start the backend:**

   ```bash
   cd backend
   dotnet run
   ```

2. **In a new terminal, start the frontend:**

   ```bash
   cd frontend
   npm start
   ```

3. **Access the application:**
   - Frontend: http://localhost:4200
   - Backend API: http://localhost:5176
   - Swagger UI: http://localhost:5176/swagger

### Database Management

To reset the database:

```bash
cd backend
dotnet ef database drop
dotnet ef database update
```

To create a new migration:

```bash
cd backend
dotnet ef migrations add YourMigrationName
```

## Building for Production

### Backend

```bash
cd backend
dotnet build --configuration Release
dotnet publish --configuration Release
```

### Frontend

```bash
cd frontend
npm run build
```

The built files will be in the `frontend/dist/` directory.

## Environment Configuration

### Backend

- Development settings: `backend/appsettings.Development.json`
- Production settings: `backend/appsettings.json`

### Frontend

- Development: `frontend/src/environments/environment.ts`
- Production: `frontend/src/environments/environment.prod.ts`

## Troubleshooting

### Common Issues

1. **CORS errors**: Ensure the backend CORS policy includes the frontend URL
2. **Database connection**: Check SQLite file permissions and path
3. **Port conflicts**: Make sure ports 4200 (frontend) and 5176 (backend) are available
4. **Node modules**: Try `rm -rf node_modules && npm install` if frontend issues persist
5. **Package restore**: Run `dotnet restore` if backend packages are missing

### API Connection Issues

If the frontend cannot connect to the API:

1. Verify the backend is running on http://localhost:5176
2. Check the API base URL in the frontend service configuration
3. Ensure CORS is properly configured in the backend

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests to ensure everything works
5. Submit a pull request

## Technology Stack

### Backend

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core 9.0.10
- SQLite Database
- Swagger/OpenAPI
- xUnit (testing)

### Frontend

- Angular 19
- TypeScript
- SCSS
- Karma/Jasmine (testing)
- Angular CLI

## License

This project is for educational purposes.
