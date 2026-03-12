# 🌙 Moonlight Farm - NOT FINISHED YET

![GitHub License](https://img.shields.io/github/license/jakub/Moonlight-Farm?style=for-the-badge)
![.NET 10](https://img.shields.io/badge/.NET-10.0-512bd4?style=for-the-badge&logo=dotnet)
![SignalR](https://img.shields.io/badge/SignalR-Live--Sync-orange?style=for-the-badge)
![Canvas API](https://img.shields.io/badge/HTML5-Canvas-E34F26?style=for-the-badge&logo=html5)

**Moonlight Farm** is a singleplayer farming RPG inspired by classics like *Stardew Valley*. Built with modern web technologies and a robust .NET backend, it allows you to manage your own farm, grow crops, and develop your homestead with real-time server-side synchronization.

---

## 🌟 Key Features

- � **Server-Side Persistence**: Your progress is automatically synced and saved to a SQLite database using Entity Framework Core.
- 🏠 **World Instances**: Create and manage different farm worlds using a simple room/world ID system.
- 🕒 **Day & Night Cycle**: Dynamic time progression affecting gameplay and world lighting.
- 🌾 **Farming & Interaction System**: Till the soil, water crops, fish, and gather resources.
- 🎨 **Retro Graphics**: Stylized pixel-art interface utilizing the *Press Start 2P* font.
- ⚡ **Live Updates**: High-performance state management powered by SignalR for seamless gameplay.

---

## 🛠️ Technologies

### Frontend
- **HTML5 Canvas**: Fast and efficient 2D world rendering.
- **JavaScript (Vanilla)**: Clean client-side logic without heavy frameworks.
- **SignalR Client**: Handles real-time communication with the server for state updates.
- **CSS3**: UI styling and responsiveness.

### Backend
- **ASP.NET Core 10**: High-performance web server.
- **SignalR Hubs**: Managing game state and synchronizing actions.
- **Entity Framework Core**: Object-relational mapping for the database.
- **SQLite**: Lightweight and fast database for sessions and world state.
- **Hosted Services**: Background game loop running on the server.

---

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Installation & Execution

1.  **Clone the repository**:
    ```bash
    git clone https://github.com/jakub/Moonlight-Farm.git
    cd Moonlight-Farm
    ```

2.  **Run the server**:
    ```bash
    cd Server
    dotnet run
    ```
    The server defaults to port `5555`.

3.  **Access the game**:
    Open your browser and navigate to:
    `http://localhost:5555`

---

## 🎮 Controls

| Key | Action |
| :--- | :--- |
| **W, S, A, D** | Character Movement |
| **LMB** | Action (use tool, interact) |
| **1 - 9** | Select Item from Inventory |
| **R** | Sleep (rest / advance time) |
| **F** | Fishing |

---

## 📂 Project Structure

- `/Client` - Static files, game JS scripts, and CSS styles.
- `/Server` - Backend logic, SignalR Hubs, data models, and database.
- `/Server/Models` - Definitions for sessions, players, and the world.
- `/Server/Services` - Business logic, game loop, and persistence system.

---

## 📄 License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

---
