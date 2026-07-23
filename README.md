<div align="center">
  <img src="https://img.icons8.com/clouds/200/000000/broom.png" alt="CleanAI Logo" width="150" height="150"/>
  <h1>🌟 CleanAI - Backend Service 🌟</h1>
  <p><em>The core engine powering the CleanAI marketplace, built with ASP.NET Core & PostgreSQL.</em></p>
 
  ![.NET Core](https://img.shields.io/badge/.NET%208.0-Purple?logo=dotnet&style=for-the-badge)
  ![PostgreSQL](https://img.shields.io/badge/PostgreSQL-316192?logo=postgresql&logoColor=white&style=for-the-badge)
  ![Docker](https://img.shields.io/badge/Docker-2496ED?logo=docker&logoColor=white&style=for-the-badge)
  ![Swagger](https://img.shields.io/badge/Swagger-85EA2D?logo=swagger&logoColor=black&style=for-the-badge)
</div>

---

## 📖 Table of Contents

- [📖 Table of Contents](#-table-of-contents)
- [🚀 About the Project](#-about-the-project)
- [✨ Key Features](#-key-features)
- [📂 Project Structure](#-project-structure)
- [🛠 Getting Started](#-getting-started)
  - [Prerequisites](#prerequisites)
  - [1️⃣ Database \& Docker Setup](#1️⃣-database--docker-setup)
  - [2️⃣ Application Configuration](#2️⃣-application-configuration)
  - [3️⃣ Migrations \& Seeding](#3️⃣-migrations--seeding)
  - [4️⃣ Run the API](#4️⃣-run-the-api)
- [🛡️ Security Notes](#️-security-notes)
- [🔧 Troubleshooting](#-troubleshooting)

---

## 🚀 About the Project

CleanAI is an on-demand cleaning service marketplace that connects **Clients**, **Cleaners (Workers)**, and **Administrators**. This repository contains the robust backend API that handles authentication, booking logic, payment processing, and real-time chat with an AI assistant.

---

## ✨ Key Features

- **JWT Authentication & Authorization**: Secure role-based access for Admin, Client, and Worker.
- **Advanced Booking System**: Real-time availability checking, price calculation, and status tracking.
- **AI Assistant Integration**: Powered by Ollama for smart chat responses.
- **EF Core ORM**: Highly optimized queries with PostgreSQL.
- **Dockerized Environment**: Quick and easy setup for development and production.

---

## 📂 Project Structure

```text
CleaningServiceApp/
 ├── CleaningService.API/   # API Controllers, Middleware & Startup configuration
 ├── BLL/                   # Business Logic Layer (Services, DTOs, Interfaces)
 ├── DAL/                   # Data Access Layer (Entities, DbContext, Repositories)
 ├── docker-compose.yml     # Container orchestration for DB & AI
 └── CleaningServiceApp.sln # Main Solution File
```

---

## 🛠 Getting Started

### Prerequisites

Ensure you have the following installed:
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- Entity Framework Core CLI (v9.0.17):
  ```powershell
  dotnet tool install --global dotnet-ef --version 9.0.17
  ```

### 1️⃣ Database & Docker Setup

Set up your `.env` file for Docker:
```dotenv
DB_HOST_PORT=5433
DB_USER=postgres
DB_PASSWORD=your_secure_password
JWT_SECRET=your_super_secret_key_here
```

Start the PostgreSQL database:
```powershell
docker compose up -d db
```

### 2️⃣ Application Configuration

Configure `appsettings.json` in `CleaningService.API`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=PRM393_Cleaning;Username=postgres;Password=your_secure_password"
  }
}
```

### 3️⃣ Migrations & Seeding

Apply migrations to create the schema and seed demo data:
```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project DAL --startup-project CleaningService.API
```

*Demo Accounts Seeded:*
- **Admin**: `admin@cleanai.local` | `CleanAI123!`
- **Client**: `client@cleanai.local` | `CleanAI123!`
- **Worker**: `worker@cleanai.local` | `CleanAI123!`

### 4️⃣ Run the API

```powershell
dotnet run --project .\CleaningService.API\CleaningService.API.csproj
```
API will be live at `http://localhost:5000`  
Swagger UI available at `http://localhost:5000/swagger`

---

## 🛡️ Security Notes

- 🛑 **Never** commit `.env` or `appsettings.json` to version control.
- 🔑 **Replace** all example secrets and JWT keys before deploying to production.
- 🧪 Seeded credentials are for local **Development** environments only.
- 🚀 **Production migrations** should run through a controlled CI/CD pipeline.

---

## 🔧 Troubleshooting

- **`MSB1009: Project file does not exist`**: Ensure you are running commands from the `CleaningServiceApp` directory.
- **Port Conflict**: If PostgreSQL port `5433` is occupied, change `DB_HOST_PORT` in `.env` and `appsettings.json`.
- **401 Unauthorized on Login**: Ensure you are using `emailOrPhone` in your request body, not just `email`.
