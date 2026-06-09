# ![CI Build Status](https://github.com/YourUser/CaseForgeAI/actions/workflows/ci.yml/badge.svg)

# CaseForgeAI

**An immersive detective mystery platform** built with ASP.NET Core, Entity Framework, and PostgreSQL (Supabase). It provides a web UI for playing narrative crime cases, with role‑based authentication, JWT API, and a rich data model for stories, suspects, clues, puzzles, and analytics.

---

## 📖 Overview

CaseForgeAI lets players solve intricate murder‑or‑theft mysteries.  Think **“Clue”** meets **“Detective Conan”**.  The backend stores cases, seeds an example case (the Sterling Safe Heist), and provides APIs for a front‑end Razor‑pages UI.

### Simple Example

When you launch the site, you can log in as **admin@caseforge.com** (password `NoirAdmin123!`).  As an admin you can manage cases; as a regular player you can explore the story, view clues, and solve puzzles.

---

## 🏗️ Architecture Diagram

*(Placeholder – replace with your own diagram or generate one later)*

```
[Client] <--HTTPS--> [ASP.NET Core Web App]
                         |
                         +--[Entity Framework Core]--[PostgreSQL (Supabase)]
                         |
                         +--[Identity] (Cookie + JWT)
```

---

## 🛠️ Installation

```powershell
# 1. Clone the repository (already set up)
git clone https://github.com/YourUser/CaseForgeAI.git
cd CaseForgeAI

# 2. Set the connection string (add to appsettings.Development.json)
#    "DefaultConnection": "Host=YOUR_SUPABASE_HOST;Database=YOUR_DB;Username=YOUR_USER;Password=YOUR_PASS"

# 3. Apply migrations and seed data
dotnet ef database update

# 4. Run the app
dotnet run
```

The app will be available at `https://localhost:5001`.

---

## ▶️ Usage Examples

1. **Login** – Go to `/Account/Login` and use the admin credentials above.
2. **Play a Case** – Navigate to `/Gameplay/Start` (or Home) and follow the story.
3. **Solve a Puzzle** – Click a clue, read the description, and submit an answer (e.g., the safe‑lock cipher answer is `HELP ME`).

---

## 🤝 Contribution Guide

1. Fork the repo.
2. Create a feature branch: `git checkout -b feature/awesome‑feature`.
3. Make changes and ensure they compile:
   ```powershell
   dotnet build
   dotnet test   # (if tests exist)
   ```
4. Submit a Pull Request.

> **Tip:** Follow the existing coding style – services are registered in `Program.cs`, controllers are in the `Controllers/` folder, and data models live under `Core/Entities/`.

---

## 📄 License

Distributed under the MIT License. See `LICENSE` for details.

---

## 🧪 Testing

*No explicit test project is present yet.*  You can add XUnit tests under a `Tests/` folder and run them with:
```powershell
dotnet test
```

---

## 📞 Contact / Support

- **Maintainer:** Your Name – <you@example.com>
- **Issue Tracker:** Open issues on GitHub.

---

## 📦 Tech Stack Rationale (Why These Technologies?)

| Technology | Why it shines for this project |
|------------|--------------------------------|
| **ASP.NET Core** | High‑performance, cross‑platform web framework; perfect for building robust APIs and Razor UI. |
| **Entity Framework Core + PostgreSQL** | EF Core gives a clean object‑relational mapping; PostgreSQL (via Supabase) offers free hosted DB with powerful SQL features. |
| **Identity (Cookies + JWT)** | Provides both web‑session authentication (cookies) and token‑based API auth (JWT) in a single setup. |
| **Dockerfile** | Enables containerised deployment – run the app anywhere with `docker build && docker run`. |
| **GitHub Actions CI badge** | Shows build status automatically, encouraging continuous integration. |

---

*Happy sleuthing!*
