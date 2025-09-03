# Gaming Café Management Suite

A comprehensive, modern gaming café management system built with C# .NET 8, designed for Windows-based gaming cafés. This localhost-only solution provides complete management capabilities for gaming stations, users, inventory, payments, and analytics.

## 🎮 Features

### Core Functionality
- **🔐 User Registration & Secure Login** - JWT authentication with password hashing
- **⏱️ Real-time Game-time Tracking** - Live session monitoring with automatic billing
- **📅 Reservations & Booking System** - Advanced booking management for gaming stations
- **💳 Integrated Payment Processing** - Multiple payment methods including wallet system
- **🛒 Point of Sale (POS)** - Desktop application for customer transactions
- **📦 Inventory Management** - Complete stock control with low-stock alerts
- **🏆 Loyalty Programs** - Customer rewards and points system
- **👤 User Profiles with Wallet** - Digital wallet and transaction history
- **🖥️ Remote Management** - Web-based administration interface
- **🎯 PS5 Integration Framework** - Ready for console management

### Technical Features
- **Modern UI Design** - Bootstrap web interface + Material Design desktop app
- **Real-time Updates** - SignalR for live data synchronization
- **Scalable Architecture** - Multi-project .NET solution with clean separation
- **Local Database** - SQL Server LocalDB for localhost-only operation
- **Cross-platform Compatible** - .NET 8 with Windows-first design

## 🏗️ Architecture

```
Gaming Café Management Suite/
├── src/
│   ├── GamingCafe.Core/          # Business models and entities
│   ├── GamingCafe.Data/          # Entity Framework database layer
│   ├── GamingCafe.API/           # REST API backend
│   ├── GamingCafe.Admin/         # Blazor web admin panel
│   └── GamingCafe.POS/           # WPF desktop POS application
├── GamingCafeManagement.sln      # Solution file
└── README.md
```

### Project Structure
- **GamingCafe.Core** - Shared business models, enums, and entities
- **GamingCafe.Data** - Entity Framework DbContext and configurations
- **GamingCafe.API** - ASP.NET Core Web API with authentication
- **GamingCafe.Admin** - Blazor Server web application for management
- **GamingCafe.POS** - WPF desktop application for point-of-sale

## 🚀 Quick Start

### Prerequisites
- Windows 10/11
- .NET 8.0 SDK or later
- Visual Studio 2022 or VS Code
- SQL Server LocalDB (included with Visual Studio)

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/windows-gaming-cafe-management-suite.git
   cd windows-gaming-cafe-management-suite
   ```

2. **Restore packages**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run the applications**

   **Start API Server:**
   ```bash
   dotnet run --project src/GamingCafe.API/GamingCafe.API.csproj
   ```
   API will be available at: `http://localhost:5148`

   **Start Admin Panel:**
   ```bash
   dotnet run --project src/GamingCafe.Admin/GamingCafe.Admin.csproj
   ```
   Admin panel will be available at: `http://localhost:5201`

   **Start POS Application:**
   ```bash
   dotnet run --project src/GamingCafe.POS/GamingCafe.POS.csproj
   ```

## 🎯 Usage

### Default Login
- **Username:** `admin`
- **Password:** `admin123`

### Admin Panel Features
Navigate to `http://localhost:5201` to access:

- **📊 Dashboard** - Real-time overview and statistics
- **🖥️ Stations** - Gaming station management and monitoring
- **👥 Users** - Customer and staff account management
- **🎮 Sessions** - Active gaming session tracking
- **📦 Inventory** - Product and stock management
- **📈 Reports** - Analytics and revenue reporting

### POS Application
The desktop POS application provides:
- Product catalog with categories
- Shopping cart functionality
- Multiple payment methods
- Receipt generation
- Customer lookup
- Real-time inventory updates

## 🗃️ Database

The system uses SQL Server LocalDB with Entity Framework Core. The database is automatically created and seeded with sample data on first run:

### Seeded Data
- **1 Admin User** - Default administrator account
- **5 Gaming Stations** - PC-01 through PC-05
- **4 Sample Products** - Energy drinks, snacks, accessories
- **1 Loyalty Program** - Default rewards program
- **1 PS5 Console** - Sample console for PS5 integration

### Entity Models
- Users, GameStations, GameSessions
- Products, Transactions, Inventory
- PS5Consoles, Reservations
- LoyaltyPrograms, WalletTransactions

## 🔧 Configuration

### API Configuration
Edit `src/GamingCafe.API/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=GamingCafeDB;Integrated Security=True"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-here",
    "Issuer": "GamingCafeAPI",
    "Audience": "GamingCafeClients"
  }
}
```

### Admin Panel Configuration
Edit `src/GamingCafe.Admin/appsettings.json`:
```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5148"
  }
}
```

## 🛠️ Development

### Technology Stack
- **Backend:** ASP.NET Core 8.0, Entity Framework Core
- **Frontend:** Blazor Server, Bootstrap 5, FontAwesome
- **Desktop:** WPF with Material Design
- **Database:** SQL Server LocalDB
- **Authentication:** JWT tokens, BCrypt password hashing
- **Real-time:** SignalR
- **API:** RESTful design with Swagger documentation

### Key Dependencies
- Microsoft.EntityFrameworkCore.SqlServer
- Microsoft.AspNetCore.Authentication.JwtBearer
- Microsoft.AspNetCore.SignalR
- BCrypt.Net-Next
- Bootstrap 5.3
- FontAwesome 6.0

### Building from Source
```bash
# Build entire solution
dotnet build

# Build specific project
dotnet build src/GamingCafe.API/GamingCafe.API.csproj

# Run tests (when available)
dotnet test

# Publish for deployment
dotnet publish src/GamingCafe.API/GamingCafe.API.csproj -c Release
```

## 📝 API Documentation

The API includes Swagger documentation available at `http://localhost:5148/swagger` when running in development mode.

### Key Endpoints
- `POST /api/auth/login` - User authentication
- `GET /api/stations` - Get all gaming stations
- `POST /api/sessions` - Start new gaming session
- `GET /api/products` - Get inventory products
- `POST /api/transactions` - Process payments

## 🔒 Security Features

- **JWT Authentication** - Secure token-based authentication
- **Password Hashing** - BCrypt for secure password storage
- **Role-based Authorization** - Admin, Staff, and Customer roles
- **CORS Protection** - Configured for localhost operation
- **Input Validation** - Comprehensive model validation
- **SQL Injection Prevention** - Entity Framework parameterized queries

## 🚦 System Requirements

### Minimum Requirements
- Windows 10 (version 1909 or later)
- 4 GB RAM
- 1 GB free disk space
- .NET 8.0 Runtime

### Recommended Requirements
- Windows 11
- 8 GB RAM
- 2 GB free disk space
- SSD storage for better performance

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📋 Roadmap

### Planned Features
- [ ] Advanced reporting with charts and graphs
- [ ] Email notifications for reservations
- [ ] Mobile app companion
- [ ] Advanced PS5 remote control
- [ ] Multi-location support
- [ ] Advanced loyalty program features
- [ ] Automated backup system
- [ ] Performance monitoring dashboard

### Completed Features
- [x] User management system
- [x] Gaming station tracking
- [x] POS functionality
- [x] Inventory management
- [x] Basic reporting
- [x] Wallet system
- [x] Real-time updates

## 🐛 Known Issues

- SignalR package warnings in .NET 10 preview (cosmetic only)
- PageTitle component warnings in Blazor (cosmetic only)

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Built with modern .NET 8 technologies
- UI components from Bootstrap and FontAwesome
- Material Design inspiration for desktop application
- Entity Framework for robust data management

## 📞 Support

For support, please open an issue on GitHub or contact the development team.

---

**Made with ❤️ for gaming café owners who want to streamline their operations and focus on providing great gaming experiences.**
