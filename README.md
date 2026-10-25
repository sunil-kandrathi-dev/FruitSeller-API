# FruitSeller-API
ASP.NET Core Web API for the Fruit Seller Management Application.

## Setup
1. Set the SQL Server connection string in `FruitSeller.API/appsettings.json` or via User Secrets:
   - `ConnectionStrings:FruitSellerDb`
   - Current configured value: `Data Source=Localhost;Integrated Security=True;Persist Security Info=False;Pooling=False;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=True;Application Name="SQL Server Management Studio";Command Timeout=0`
   - If your database is not the default database for Localhost, add `Initial Catalog=YourDatabaseName;` to the connection string.
2. Ensure the SQL Server instance contains the `dbo.Shops`, `dbo.Fruits`, `dbo.Purchases`, `dbo.Sales`, `dbo.Payments`, `dbo.Expenses`, and `dbo.InventoryMovements` tables described in the project requirements.
3. Restore packages and run the API:
   - `dotnet restore`
   - `dotnet run --project FruitSeller.API/FruitSeller.API.csproj`
4. Open Swagger from the app root, typically `https://localhost:<port>/swagger`.

## Notes
- The API expects an existing SQL Server schema and does not create or alter tables or run migrations.
- I verified the SQL Server instance at `Localhost` is reachable, but it currently does not contain the required FruitSeller table set. The app must connect to the correct database instance and schema before it can serve real data.
- Sales use the database computed column for `TotalAmount` and the API does not accept a client-supplied total.
- Conservative stock policy: a sale is rejected if it would reduce stock below zero. This is a deliberate guardrail and should be confirmed with the product owner if negative stock is ever allowed.
- Date validation uses UTC date boundaries (`DateOnly.FromDateTime(DateTime.UtcNow)`) to keep behavior consistent without assuming a local server timezone.
