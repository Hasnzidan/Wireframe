# Wireframe Order Management System

A web-based order management system built with ASP.NET Core MVC that allows users to manage orders and order items.

## Features

- Create, read, update, and delete orders
- Manage order items with products and quantities
- Filter orders by customer name and date range
- Secure access with authentication
- Responsive user interface

## Technologies Used

- ASP.NET Core MVC
- Entity Framework Core
- Bootstrap
- SQL Server

## Getting Started

1. Clone the repository
2. Update the connection string in `appsettings.json`
3. Run Entity Framework migrations:
   ```
   dotnet ef database update
   ```
4. Run the application:
   ```
   dotnet run
   ```

## Prerequisites

- .NET 7.0 SDK or later
- SQL Server
- Visual Studio 2022 or Visual Studio Code
