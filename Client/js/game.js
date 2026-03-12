const canvas = document.getElementById('gameCanvas');
const ctx = canvas.getContext('2d');
const TILE_SIZE = 16;
const SCALE = 4;

let gameState = null;
let lastUpdate = 0;
let sessionId = null;
let roomId = "default";
let otherPlayers = {};

const connection = new signalR.HubConnectionBuilder()
    .withUrl("/gameHub")
    .configureLogging(signalR.LogLevel.Information)
    .build();

const COLORS = {
    // Soft, warm palette for day
    Grass: '#8eb360',
    GrassDetail: '#7a9e52',
    Water: '#81c3d7',
    DeepWater: '#5b96a8',
    Dirt: '#c4a484',
    TilledDirt: '#8d6e63',
    Stone: '#90a4ae',
    Wood: '#a1887f',
    Wall: '#5d4037',
    Mountain: '#78909c',
    
    // Player
    PlayerSkin: '#ffccbc',
    PlayerShirt: '#5c6bc0',
    PlayerPants: '#37474f',
    PlayerHair: '#4e342e',
    
    // Objects
    TreeLeaf: '#43a047',
    TreeTrunk: '#5d4037',
    Iron: '#b0bec5',
    Gold: '#ffd54f',
    Moonlight: '#9575cd',
    MoonGlow: 'rgba(209, 196, 233, 0.2)'
};

async function init() {
    console.log("Inicjalizacja gry...");
    canvas.width = 1920;
    canvas.height = 1080;
    ctx.imageSmoothingEnabled = false;
    
    const joinBtn = document.getElementById('join-btn');
    if (joinBtn) {
        console.log("Znaleziono przycisk join-btn, przypisuję zdarzenie kliknięcia.");
        joinBtn.onclick = async () => {
            console.log("Kliknięto przycisk 'Dołącz do gry'. ID Pokoju:", document.getElementById('room-id-input').value);
            roomId = document.getElementById('room-id-input').value || "default";
            
            console.log("Ukrywam menu, pokazuję kontener gry.");
            document.getElementById('room-menu').style.display = 'none';
            document.getElementById('game-container').style.display = 'block';
            document.getElementById('room-display').innerText = `Pokój: ${roomId}`;
            
            try {
                await fetchGameState();
                console.log("Stan gry pobrany pomyślnie.");
                await setupSignalR();
                console.log("SignalR połączony pomyślnie.");
            } catch (error) {
                console.error("Błąd podczas dołączania do gry:", error);
            }
        };
    } else {
        console.error("Nie znaleziono elementu o id 'join-btn'!");
    }
    
    setInterval(fetchGameState, 10000);
    
    window.addEventListener('keydown', handleInput);
    canvas.addEventListener('mousedown', handleMouse);
    requestAnimationFrame(render);
    console.log("Pętla renderowania uruchomiona.");
}

// Uruchomienie init po załadowaniu DOM
document.addEventListener('DOMContentLoaded', init);

async function setupSignalR() {
    connection.on("PlayerMoved", (id, x, y, direction) => {
        otherPlayers[id] = { position: { X: x, Y: y }, facing: direction };
    });

    connection.on("PlayerJoined", (id, sId) => {
        console.log("New player joined:", id, sId);
    });

    connection.on("BroadcastAction", (id, action, x, y) => {
        // Natychmiastowe odświeżenie po akcji innego gracza
        fetchGameState();
    });

    connection.on("SyncRoomState", (state) => {
        if (state && state.FarmMap) {
            gameState.FarmMap = state.FarmMap;
        }
    });

    try {
        await connection.start();
        await connection.invoke("JoinRoom", roomId, sessionId);
        AudioEngine.init();
        document.addEventListener('click', () => {
            if (AudioEngine.ctx.state === 'suspended') {
                AudioEngine.ctx.resume();
            }
            AudioEngine.playAmbient(gameState?.Time?.IsNight || false);
            AudioEngine.playMusic(gameState?.Time?.IsNight || false);
        }, { once: true });
    } catch (err) {
        console.error("SignalR Error:", err);
    }
}

async function fetchGameState() {
    try {
        const response = await fetch(`/api/game/state?roomId=${roomId}`);
        const data = await response.json();
        gameState = data.GameState;
        sessionId = data.SessionId;
        updateUI(data);
    } catch (err) { console.error(err); }
}

function updateUI(data) {
    if (!data.GameState) return;
    
    const gameState = data.GameState;
    const player = gameState.Player;
    const time = data.DisplayTime;

    // Time & Date
    const timeStr = `Dzień ${time.Day} (${time.Season}) - ${String(time.Hour).padStart(2, '0')}:${String(time.Minute).padStart(2, '0')}`;
    document.getElementById('time-display').innerText = timeStr;
    
    // Moon Phase
    let moonDisplay = document.getElementById('moon-display');
    if (!moonDisplay) {
        moonDisplay = document.createElement('div');
        moonDisplay.id = 'moon-display';
        moonDisplay.style = 'position: fixed; top: 60px; right: 20px; color: #fff; font-family: monospace;';
        document.body.appendChild(moonDisplay);
    }
    moonDisplay.innerText = `Faza: ${time.MoonPhase}`;
    moonDisplay.style.color = time.IsFullMoon ? '#f1c40f' : 'white';

    // Skills display
    let skillsDiv = document.getElementById('skills-display');
    if (!skillsDiv) {
        skillsDiv = document.createElement('div');
        skillsDiv.id = 'skills-display';
        skillsDiv.style = 'position: fixed; top: 100px; right: 20px; background: rgba(0,0,0,0.7); color: #fff; padding: 10px; border-radius: 5px; font-size: 12px; font-family: monospace;';
        document.body.appendChild(skillsDiv);
    }
    
    let skillsHtml = '<strong>UMIEJĘTNOŚCI</strong><br>';
    for (const [skill, exp] of Object.entries(player.Skills)) {
        const level = Math.floor(exp / 100) + 1;
        skillsHtml += `${skill}: Lvl ${level} (${exp % 100}/100)<br>`;
    }
    skillsDiv.innerHTML = skillsHtml;

    // Money
    document.getElementById('money-display').innerText = `${player.Money}g`;

    // Energy Bar
    let energyBar = document.getElementById('energy-bar');
    if (!energyBar) {
        energyBar = document.createElement('div');
        energyBar.id = 'energy-bar';
        energyBar.style = 'width: 200px; height: 20px; background: #333; position: fixed; bottom: 80px; left: 20px; border: 2px solid #fff;';
        
        const inner = document.createElement('div');
        inner.id = 'energy-inner';
        inner.style = 'height: 100%; background: #4caf50; width: 100%; transition: width 0.3s;';
        energyBar.appendChild(inner);
        
        const text = document.createElement('div');
        text.id = 'energy-text';
        text.style = 'position: absolute; width: 100%; text-align: center; color: #fff; font-size: 12px; line-height: 20px;';
        energyBar.appendChild(text);
        
        document.body.appendChild(energyBar);
    }
    
    const energyPercent = (player.Energy / player.MaxEnergy) * 100;
    const energyInner = document.getElementById('energy-inner');
    energyInner.style.width = `${energyPercent}%`;
    document.getElementById('energy-text').innerText = `Energia: ${player.Energy}/${player.MaxEnergy}`;
    
    if (player.Energy < 20) energyInner.style.background = '#f44336';
    else if (player.Energy < 50) energyInner.style.background = '#ffeb3b';
    else energyInner.style.background = '#4caf50';

    // Inventory Bar
    const invBar = document.getElementById('inventory-bar');
    invBar.innerHTML = '';
    
    // Show empty slots up to inventory limit
    for (let i = 0; i < player.InventorySlots; i++) {
        const item = player.Inventory[i];
        const slot = document.createElement('div');
        slot.className = `inventory-slot ${i === player.SelectedSlot ? 'selected' : ''}`;
        
        if (item) {
            const icon = getItemIcon(item);
            slot.innerHTML = `
                <span title="${item.Name}">${icon}</span>
                <div class="durability-bar" style="display: ${item.Type === 'Tool' ? 'block' : 'none'}">
                    <div class="durability-fill" style="width: ${(item.Durability/item.MaxDurability)*100}%"></div>
                </div>
                ${item.Count > 1 ? `<div class="item-count">${item.Count}</div>` : ''}
            `;
        }
        
        slot.onclick = () => selectSlot(i);
        invBar.appendChild(slot);
    }
}

function getItemIcon(item) {
    if (item.Type === 'Tool') {
        if (item.Name.includes('Pickaxe')) return '⛏️';
        if (item.Name.includes('Axe')) return '🪓';
        if (item.Name.includes('Hoe')) return '🌾';
        if (item.Name.includes('Watering')) return '💧';
        if (item.Name.includes('Rod')) return '🎣';
    }
    if (item.Type === 'Seed') return '🌱';
    if (item.Type === 'Fish') return '🐟';
    if (item.Type === 'Resource') {
        if (item.Name === 'Stone') return '🪨';
        if (item.Name === 'Iron Ore') return '⛓️';
        if (item.Name === 'Wood') return '🪵';
    }
    return item.Name[0];
}

function render(time) {
    if (!gameState) {
        requestAnimationFrame(render);
        return;
    }

    const ambient = getAmbientLight();
    ctx.fillStyle = ambient.bg;
    ctx.fillRect(0, 0, canvas.width, canvas.height);
    
    const player = gameState.Player;
    const offsetX = canvas.width / 2 - (player.Position.X * TILE_SIZE * SCALE);
    const offsetY = canvas.height / 2 - (player.Position.Y * TILE_SIZE * SCALE);

    const map = gameState.CurrentMineMap || gameState.FarmMap;
    const startX = Math.floor(-offsetX / (TILE_SIZE * SCALE));
    const startY = Math.floor(-offsetY / (TILE_SIZE * SCALE));
    const endX = startX + Math.ceil(canvas.width / (TILE_SIZE * SCALE)) + 1;
    const endY = startY + Math.ceil(canvas.height / (TILE_SIZE * SCALE)) + 1;

    for (let y = startY; y < endY; y++) {
        for (let x = startX; x < endX; x++) {
            const tile = getTileAt(x, y);
            if (tile) {
                drawTile(x, y, tile, offsetX, offsetY);
                if (tile.Object) drawObject(x, y, tile.Object, offsetX, offsetY, time);
            }
        }
    }

    if (gameState.Monsters) {
        gameState.Monsters.forEach(monster => drawMonster(monster, offsetX, offsetY, time));
    }

    if (gameState.Animals && !gameState.CurrentMineMap) {
        gameState.Animals.forEach(animal => drawAnimal(animal, offsetX, offsetY, time));
    }

    if (gameState.NPCs && !gameState.CurrentMineMap) {
        gameState.NPCs.forEach(npc => drawNPC(npc, offsetX, offsetY, time));
    }

    for (let id in otherPlayers) {
        const p = otherPlayers[id];
        drawPlayer(p.position.X, p.position.Y, p.facing, offsetX, offsetY, time, true);
    }

    // Draw current player
    drawPlayer(player.Position.X, player.Position.Y, player.Facing, offsetX, offsetY, time, false);

    // Day/Night Overlay
    ctx.fillStyle = ambient.overlay;
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    // Moonlight effect (simple radial gradient at night)
    if (gameState.Time.CurrentHour >= 20 || gameState.Time.CurrentHour <= 5) {
        const pScreenX = offsetX + player.Position.X * TILE_SIZE * SCALE + (TILE_SIZE * SCALE / 2);
        const pScreenY = offsetY + player.Position.Y * TILE_SIZE * SCALE + (TILE_SIZE * SCALE / 2);
        
        const gradient = ctx.createRadialGradient(pScreenX, pScreenY, 50, pScreenX, pScreenY, 300);
        gradient.addColorStop(0, 'rgba(255, 255, 255, 0.15)');
        gradient.addColorStop(1, 'rgba(0, 0, 0, 0)');
        
        ctx.globalCompositeOperation = 'screen';
        ctx.fillStyle = gradient;
        ctx.fillRect(0, 0, canvas.width, canvas.height);
        ctx.globalCompositeOperation = 'source-over';
    }

    // Draw Weather effects
    if (typeof drawWeather === 'function') {
        drawWeather(time);
    }

    requestAnimationFrame(render);
}

function getAmbientLight() {
    const player = gameState.Player;
    const isMoonlightGrove = player.Position.X > 40 && player.Position.X < 60 && player.Position.Y < 20;
    
    const hour = gameState.Time.CurrentHour;
    const minute = gameState.Time.CurrentMinute;
    const timeValue = hour + minute / 60;
    
    if (isMoonlightGrove && (timeValue >= 20 || timeValue < 5)) {
        return { 
            bg: '#311b92', 
            overlay: 'rgba(103, 58, 183, 0.4)',
            glow: true
        };
    }

    // Dzień (10:00 - 16:00) - Jasne, naturalne kolory
    if (timeValue >= 10 && timeValue < 16) {
        return { bg: '#e3f2fd', overlay: 'rgba(255, 255, 255, 0)' };
    }
    // Zachód (16:00 - 20:00) - Pomarańczowo-różowe światło
    if (timeValue >= 16 && timeValue < 20) {
        const factor = (timeValue - 16) / 4;
        return { 
            bg: '#fbe9e7', 
            overlay: `rgba(255, 112, 67, ${factor * 0.4})` 
        };
    }
    // Noc (20:00 - 05:00) - Chłodne niebiesko-fioletowe odcienie
    if (timeValue >= 20 || timeValue < 5) {
        return { 
            bg: '#1a237e', 
            overlay: 'rgba(49, 27, 146, 0.5)' 
        };
    }
    // Świt (05:00 - 10:00) - Delikatne niebieskie
    if (timeValue >= 5 && timeValue < 10) {
        const factor = 1 - (timeValue - 5) / 5;
        return { 
            bg: '#e8eaf6', 
            overlay: `rgba(63, 81, 181, ${factor * 0.3})` 
        };
    }
    return { bg: '#e3f2fd', overlay: 'rgba(0,0,0,0)' };
}

function getTileAt(x, y) {
    const map = gameState.CurrentMineMap || gameState.FarmMap;
    if (x < 0 || x >= map.Width || y < 0 || y >= map.Height) return null;
    return map.Tiles[y][x];
}

function drawTile(x, y, tile, offX, offY) {
    const tx = offX + x * TILE_SIZE * SCALE;
    const ty = offY + y * TILE_SIZE * SCALE;
    const size = TILE_SIZE * SCALE;
    
    // Base color
    ctx.fillStyle = COLORS[getTileTypeName(tile.Type)] || COLORS.Grass;
    ctx.fillRect(tx, ty, size, size);

    // Pixel art details
    const seed = (x * 733 + y * 911) % 100;

    if (tile.Type === 0) { // Grass
        ctx.fillStyle = COLORS.GrassDetail;
        if (seed < 15) {
            // Tiny grass tufts
            ctx.fillRect(tx + 8, ty + 12, 4, 8);
            ctx.fillRect(tx + 12, ty + 16, 4, 4);
        } else if (seed < 25) {
            ctx.fillRect(tx + 40, ty + 32, 4, 8);
            ctx.fillRect(tx + 44, ty + 36, 4, 4);
        }
        // Small flowers
        if (seed < 5) {
            ctx.fillStyle = '#fff9c4';
            ctx.fillRect(tx + 24, ty + 24, 4, 4);
        }
    } else if (tile.Type === 2 || tile.Type === 8) { // Water
        ctx.fillStyle = 'rgba(255,255,255,0.2)';
        const wave = Math.sin(Date.now() / 800 + x * 0.5 + y * 0.5) * 4;
        ctx.fillRect(tx + 12, ty + 16 + wave, size - 24, 4);
    } else if (tile.Type === 6) { // Tilled Dirt
        ctx.fillStyle = 'rgba(0,0,0,0.1)';
        for(let i=0; i<4; i++) {
            ctx.fillRect(tx + 4, ty + i * 14 + 6, size - 8, 4);
        }
    } else if (tile.Type === 7) { // Mountain
        ctx.fillStyle = 'rgba(0,0,0,0.1)';
        ctx.fillRect(tx + 4, ty + 4, size - 8, size - 8);
    }

    // Moonlight Grove Effect (Magical glow)
    if (tile.Moisture > 0.4) {
        const pulse = (Math.sin(Date.now() / 1000 + x + y) + 1) / 2;
        ctx.fillStyle = `rgba(149, 117, 205, ${0.1 + pulse * 0.1})`;
        ctx.fillRect(tx, ty, size, size);
        
        if (seed < 10 && (Date.now() % 2000 < 1000)) {
            ctx.fillStyle = 'rgba(255, 255, 255, 0.6)';
            ctx.fillRect(tx + seed % 48 + 8, ty + (seed * 7) % 48 + 8, 4, 4);
        }
    }
}

function drawObject(x, y, obj, offX, offY, time) {
    const tx = offX + x * TILE_SIZE * SCALE;
    const ty = offY + y * TILE_SIZE * SCALE;
    const size = TILE_SIZE * SCALE;

    const type = typeof obj.Type === 'string' ? obj.Type : ["Tree", "Rock", "Crop", "Resource", "Sprinkler"][obj.Type];

    if (type === "Tree") {
        // Trunk
        ctx.fillStyle = COLORS.TreeTrunk;
        ctx.fillRect(tx + size * 0.35, ty + size * 0.4, size * 0.3, size * 0.6);
        
        // Leaves (pixelated circles)
        ctx.fillStyle = COLORS.TreeLeaf;
        const wobble = Math.sin(time / 600 + x * 100) * 3;
        
        // Main canopy
        drawPixelCircle(tx + size / 2 + wobble, ty + size * 0.2, size * 0.6, COLORS.TreeLeaf);
        // Darker part
        drawPixelCircle(tx + size / 2 + wobble, ty + size * 0.3, size * 0.4, 'rgba(0,0,0,0.1)');
        // Highlight
        drawPixelCircle(tx + size / 3 + wobble, ty + size * 0.1, size * 0.2, 'rgba(255,255,255,0.1)');

    } else if (type === "Rock") {
        ctx.fillStyle = obj.SubType === "Iron" ? COLORS.Iron : COLORS.Stone;
        ctx.beginPath();
        ctx.moveTo(tx + 8, ty + size - 8);
        ctx.lineTo(tx + size / 2, ty + 16);
        ctx.lineTo(tx + size - 8, ty + size - 8);
        ctx.fill();
        
        // Rock detail
        ctx.fillStyle = 'rgba(0,0,0,0.2)';
        ctx.fillRect(tx + size/2, ty + 30, 8, 8);
    } else if (type === "Crop") {
        const growth = (obj.GrowthStage / obj.MaxGrowthStage);
        const plantHeight = growth * size * 0.7;
        
        ctx.fillStyle = obj.IsDiseased ? '#7f8c8d' : '#2ecc71';
        // Stem
        ctx.fillRect(tx + size * 0.45, ty + size * 0.9 - plantHeight, size * 0.1, plantHeight);
        
        // Leaves
        if (obj.GrowthStage >= 2) {
            ctx.fillRect(tx + size * 0.3, ty + size * 0.8 - plantHeight/2, size * 0.4, 4);
        }
        
        // Fruit
        if (obj.GrowthStage >= 5) {
            ctx.fillStyle = obj.SubType === "Moon Blossom" ? '#9b59b6' : '#e74c3c';
            ctx.fillRect(tx + size * 0.35, ty + size * 0.2, size * 0.3, size * 0.3);
            ctx.fillStyle = 'rgba(255,255,255,0.3)';
            ctx.fillRect(tx + size * 0.4, ty + size * 0.3, 4, 4);
        }
    }
}

function drawPixelCircle(x, y, radius, color) {
    ctx.fillStyle = color;
    for (let i = -radius; i <= radius; i += 4) {
        for (let j = -radius; j <= radius; j += 4) {
            if (i * i + j * j <= radius * radius) {
                ctx.fillRect(x + i, y + j, 4, 4);
            }
        }
    }
}

function drawPlayer(x, y, facing, offX, offY, time, isOther) {
    const px = offX + x * TILE_SIZE * SCALE + (TILE_SIZE * SCALE / 4);
    const py = offY + y * TILE_SIZE * SCALE;
    const pSize = TILE_SIZE * SCALE / 2;
    const bounce = Math.abs(Math.sin(time / 200 + (isOther ? 1 : 0))) * 4;

    // Shadow
    ctx.fillStyle = 'rgba(0,0,0,0.2)';
    ctx.beginPath();
    ctx.ellipse(px + pSize/2, py + pSize, pSize/2, pSize/4, 0, 0, Math.PI * 2);
    ctx.fill();

    // Body
    ctx.fillStyle = isOther ? '#e67e22' : COLORS.PlayerShirt;
    ctx.fillRect(px, py + pSize * 0.4 - bounce, pSize, pSize * 0.4);
    
    // Legs
    ctx.fillStyle = isOther ? '#d35400' : COLORS.PlayerPants;
    ctx.fillRect(px, py + pSize * 0.8 - bounce * 0.5, pSize, pSize * 0.2);
    
    // Head
    ctx.fillStyle = COLORS.PlayerSkin;
    ctx.fillRect(px + pSize * 0.1, py - bounce, pSize * 0.8, pSize * 0.4);
    
    // Hair
    ctx.fillStyle = isOther ? '#2c3e50' : COLORS.PlayerHair;
    ctx.fillRect(px + pSize * 0.1, py - bounce, pSize * 0.8, pSize * 0.15);

    // Eyes (based on facing)
    ctx.fillStyle = '#000';
    if (facing === 1) { // Down
        ctx.fillRect(px + pSize * 0.2, py + pSize * 0.1 - bounce, 4, 4);
        ctx.fillRect(px + pSize * 0.6, py + pSize * 0.1 - bounce, 4, 4);
    } else if (facing === 0) { // Up
        // No eyes visible from back
    } else if (facing === 2) { // Left
        ctx.fillRect(px + pSize * 0.1, py + pSize * 0.1 - bounce, 4, 4);
    } else if (facing === 3) { // Right
        ctx.fillRect(px + pSize * 0.7, py + pSize * 0.1 - bounce, 4, 4);
    }
}

function drawMonster(monster, offX, offY, time) {
    const tx = offX + monster.Position.X * TILE_SIZE * SCALE;
    const ty = offY + monster.Position.Y * TILE_SIZE * SCALE;
    const size = TILE_SIZE * SCALE;
    const wobble = Math.sin(time / 200) * 3;

    ctx.fillStyle = monster.Type === "Slime" ? '#4caf50' : '#424242';
    ctx.beginPath();
    ctx.arc(tx + size/2, ty + size/2 + wobble, size/3, 0, Math.PI * 2);
    ctx.fill();
    
    // Eyes
    ctx.fillStyle = 'white';
    ctx.fillRect(tx + size/3, ty + size/3 + wobble, 4, 4);
    ctx.fillRect(tx + size*2/3 - 4, ty + size/3 + wobble, 4, 4);
}

function drawAnimal(animal, offX, offY, time) {
    const tx = offX + animal.Position.X * TILE_SIZE * SCALE;
    const ty = offY + animal.Position.Y * TILE_SIZE * SCALE;
    const size = TILE_SIZE * SCALE;
    const bounce = Math.abs(Math.sin(time / 300 + animal.Position.X)) * 5;

    ctx.fillStyle = animal.Type === "Chicken" ? '#fff' : (animal.Type === "Cow" ? '#795548' : '#e0e0e0');
    if (animal.Type === "MoonFox") ctx.fillStyle = COLORS.Moonlight;

    // Simple body
    ctx.fillRect(tx + size*0.2, ty + size*0.4 - bounce, size*0.6, size*0.5);
    // Head
    ctx.fillRect(tx + size*0.5, ty + size*0.2 - bounce, size*0.3, size*0.3);
}

function drawNPC(npc, offX, offY, time) {
    const tx = offX + npc.Position.X * TILE_SIZE * SCALE;
    const ty = offY + npc.Position.Y * TILE_SIZE * SCALE;
    const size = TILE_SIZE * SCALE;
    
    // NPC base
    ctx.fillStyle = '#3f51b5';
    if (npc.Type === "Smith") ctx.fillStyle = '#757575';
    if (npc.Type === "Magic") ctx.fillStyle = '#673ab7';

    ctx.fillRect(tx + size*0.25, ty + size*0.2, size*0.5, size*0.7);
    
    // Name tag
    ctx.fillStyle = 'white';
    ctx.font = '10px "Press Start 2P"';
    ctx.fillText(npc.Name, tx, ty - 10);
}

function getTileTypeName(type) {
    const types = ["Grass", "Dirt", "Water", "Stone", "Wood", "Wall", "TilledDirt", "Mountain", "DeepWater"];
    if (typeof type === 'string') return type;
    return types[type] || "Grass";
}

init();
