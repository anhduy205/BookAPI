# BookAPI

## Projects
- `BookApi.Api`: ASP.NET Core Web API on `http://localhost:9999`
- `BookApi.WinForms`: Windows Forms client using the API only

## Features
- Search, add, update, delete books
- Load categories from API
- Upload book images to `BookApi.Api/Content/ImageBooks`
- Postman collection in `Postman/BookApi.postman_collection.json`

## Run
1. Restore/build:
   - `dotnet restore BookApi.sln`
   - `dotnet build BookApi.sln`
2. Run API:
   - `dotnet run --project BookApi.Api`
3. Run WinForms:
   - `dotnet run --project BookApi.WinForms`

## SQL Server Note
- Default connection string uses `.\SQLEXPRESS`.
- The API also tries fallback SQL endpoints: `DESKTOP-AGCJK63\SQLEXPRESS`, `localhost\SQLEXPRESS`, and the previous `SQLEXPRESS01` variants.
- If SQL Server is not reachable, enable TCP/IP for the SQL Server instance, restart the SQL Server service, and adjust the connection string if needed.
