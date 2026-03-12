// COLORS is defined in game.js to avoid duplicate declaration error

function getAmbientLight() {
    if (!gameState) return { bg: '#e3f2fd', overlay: 'rgba(0,0,0,0)' };
    const player = gameState.Player;
    const isMoonlightGrove = player.Position.X > 40 && player.Position.X < 60 && player.Position.Y < 20;
    
    const hour = gameState.Time.CurrentHour;
    const minute = gameState.Time.CurrentMinute;
    const timeValue = hour + minute / 60;
    
    let baseLight = { bg: '#e3f2fd', overlay: 'rgba(0,0,0,0)' };

    if (isMoonlightGrove && (timeValue >= 20 || timeValue < 5)) {
        baseLight = { 
            bg: '#311b92', 
            overlay: 'rgba(103, 58, 183, 0.4)',
            glow: true
        };
    } else if (timeValue >= 10 && timeValue < 16) {
        baseLight = { bg: '#e3f2fd', overlay: 'rgba(255, 255, 255, 0)' };
    } else if (timeValue >= 16 && timeValue < 20) {
        const factor = (timeValue - 16) / 4;
        baseLight = { 
            bg: '#fbe9e7', 
            overlay: `rgba(255, 112, 67, ${factor * 0.4})` 
        };
    } else if (timeValue >= 20 || timeValue < 5) {
        baseLight = { 
            bg: '#1a237e', 
            overlay: 'rgba(49, 27, 146, 0.5)' 
        };
    } else if (timeValue >= 5 && timeValue < 10) {
        const factor = 1 - (timeValue - 5) / 5;
        baseLight = { 
            bg: '#e8eaf6', 
            overlay: `rgba(63, 81, 181, ${factor * 0.3})` 
        };
    }

    // Modyfikacja przez pogodę
    if (gameState.Weather === 1 || gameState.Weather === 2) { // Rain or Storm
        baseLight.overlay = 'rgba(44, 62, 80, 0.4)';
    } else if (gameState.Weather === 3) { // Foggy
        baseLight.overlay = 'rgba(236, 240, 241, 0.6)';
    } else if (gameState.Weather === 4) { // FullMoonMagic
        baseLight.overlay = 'rgba(103, 58, 183, 0.3)';
    }

    return baseLight;
}

function drawWeather(time) {
    if (!gameState) return;
    
    const weather = gameState.Weather;
    if (weather === 1 || weather === 2) { // Rain or Storm
        ctx.strokeStyle = 'rgba(174, 214, 241, 0.5)';
        ctx.lineWidth = 2;
        for (let i = 0; i < 100; i++) {
            const x = (Math.sin(i * 123) * 0.5 + 0.5) * canvas.width;
            const y = ((time / 2 + i * 50) % canvas.height);
            ctx.beginPath();
            ctx.moveTo(x, y);
            ctx.lineTo(x - 5, y + 15);
            ctx.stroke();
        }
        
        if (weather === 2 && Math.random() < 0.01) { // Lightning
            ctx.fillStyle = 'rgba(255, 255, 255, 0.8)';
            ctx.fillRect(0, 0, canvas.width, canvas.height);
        }
    }
    
    if (weather === 3) { // Fog
        const grad = ctx.createLinearGradient(0, 0, 0, canvas.height);
        grad.addColorStop(0, 'rgba(255, 255, 255, 0)');
        grad.addColorStop(0.5, 'rgba(255, 255, 255, 0.1)');
        grad.addColorStop(1, 'rgba(255, 255, 255, 0.2)');
        ctx.fillStyle = grad;
        ctx.fillRect(0, 0, canvas.width, canvas.height);
    }
}

function getTileAt(x, y) {
    const map = gameState.CurrentMineMap || gameState.FarmMap;
    if (x < 0 || x >= map.Width || y < 0 || y >= map.Height) return null;
    return map.Tiles[y][x];
}

function getTileTypeName(type) {
    return ["Grass", "Dirt", "Water", "Stone", "Wood", "Wall", "TilledDirt", "Mountain", "DeepWater"][type];
}

function drawTile(x, y, tile, offX, offY) {
    const tx = offX + x * TILE_SIZE * SCALE;
    const ty = offY + y * TILE_SIZE * SCALE;
    const size = TILE_SIZE * SCALE;
    
    ctx.fillStyle = COLORS[getTileTypeName(tile.Type)] || COLORS.Grass;
    ctx.fillRect(tx, ty, size, size);

    if (tile.Moisture > 0.5) {
        ctx.fillStyle = 'rgba(149, 117, 205, 0.15)';
        ctx.fillRect(tx, ty, size, size);
        ctx.shadowBlur = 15;
        ctx.shadowColor = COLORS.Moonlight;
    }

    const seed = (x * 733 + y * 911) % 100;

    if (tile.Type === 0) { // Grass
        ctx.fillStyle = COLORS.GrassDetail;
        if (seed < 15) {
            ctx.fillRect(tx + 8, ty + 12, 4, 8);
            ctx.fillRect(tx + 12, ty + 16, 4, 4);
        } else if (seed < 25) {
            ctx.fillRect(tx + 40, ty + 32, 4, 8);
            ctx.fillRect(tx + 44, ty + 36, 4, 4);
        }
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

    ctx.shadowBlur = 0;

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
        ctx.fillStyle = COLORS.TreeTrunk;
        ctx.fillRect(tx + size * 0.35, ty + size * 0.4, size * 0.3, size * 0.6);
        ctx.fillStyle = COLORS.TreeLeaf;
        const wobble = Math.sin(time / 600 + x * 100) * 3;
        drawPixelCircle(tx + size / 2 + wobble, ty + size * 0.2, size * 0.6, COLORS.TreeLeaf);
        drawPixelCircle(tx + size / 2 + wobble, ty + size * 0.3, size * 0.4, 'rgba(0,0,0,0.1)');
        drawPixelCircle(tx + size / 3 + wobble, ty + size * 0.1, size * 0.2, 'rgba(255,255,255,0.1)');
    } else if (type === "Rock") {
        ctx.fillStyle = obj.SubType === "Iron" ? COLORS.Iron : COLORS.Stone;
        ctx.beginPath();
        ctx.moveTo(tx + 8, ty + size - 8);
        ctx.lineTo(tx + size / 2, ty + 16);
        ctx.lineTo(tx + size - 8, ty + size - 8);
        ctx.fill();
        ctx.fillStyle = 'rgba(0,0,0,0.2)';
        ctx.fillRect(tx + size/2, ty + 30, 8, 8);
    } else if (type === "Crop") {
        const growth = (obj.GrowthStage / obj.MaxGrowthStage);
        const plantHeight = growth * size * 0.7;
        
        if (obj.SubType && (obj.SubType.includes("Moon") || obj.SubType.includes("Silver"))) {
            ctx.shadowBlur = 10;
            ctx.shadowColor = COLORS.Moonlight;
        }
        
        ctx.fillStyle = obj.IsDiseased ? '#7f8c8d' : (obj.SubType && obj.SubType.includes("Moon") ? COLORS.Moonlight : '#2ecc71');
        ctx.fillRect(tx + size * 0.45, ty + size * 0.9 - plantHeight, size * 0.1, plantHeight);
        if (obj.GrowthStage >= 2) {
            ctx.fillRect(tx + size * 0.3, ty + size * 0.8 - plantHeight/2, size * 0.4, 4);
        }
        if (obj.GrowthStage >= obj.MaxGrowthStage) {
            ctx.fillStyle = obj.SubType === "Moon Blossom" ? '#9b59b6' : '#e74c3c';
            ctx.fillRect(tx + size * 0.35, ty + size * 0.2, size * 0.3, size * 0.3);
            ctx.fillStyle = 'rgba(255,255,255,0.3)';
            ctx.fillRect(tx + size * 0.4, ty + size * 0.3, 4, 4);
        }
        ctx.shadowBlur = 0;
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

function drawMonster(monster, offX, offY, time) {
    const tx = offX + monster.Position.X * TILE_SIZE * SCALE;
    const ty = offY + monster.Position.Y * TILE_SIZE * SCALE;
    const size = TILE_SIZE * SCALE;
    const wobble = Math.sin(time / 200) * 3;

    ctx.fillStyle = monster.Type === "Slime" ? '#4caf50' : '#424242';
    ctx.beginPath();
    ctx.arc(tx + size/2, ty + size/2 + wobble, size/3, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = 'white';
    ctx.fillRect(tx + size/3, ty + size/3 + wobble, 4, 4);
    ctx.fillRect(tx + size*2/3 - 4, ty + size/3 + wobble, 4, 4);
}

function drawAnimal(animal, offX, offY, time) {
    const tx = offX + animal.Position.X * TILE_SIZE * SCALE;
    const ty = offY + animal.Position.Y * TILE_SIZE * SCALE;
    const size = TILE_SIZE * SCALE;
    const bounce = Math.abs(Math.sin(time / 300 + animal.Position.X)) * 5;

    if (animal.Type === "Chicken") {
        ctx.fillStyle = 'white';
        ctx.fillRect(tx + 12, ty + size - 24 - bounce, 24, 20);
        ctx.fillStyle = 'red';
        ctx.fillRect(tx + 30, ty + size - 30 - bounce, 8, 8);
    } else if (animal.Type === "Cow") {
        ctx.fillStyle = 'white';
        ctx.fillRect(tx + 4, ty + size - 32 - bounce, 40, 28);
        ctx.fillStyle = 'black';
        ctx.fillRect(tx + 8, ty + size - 28 - bounce, 12, 12);
    } else if (animal.Type === "MoonFox") {
        ctx.fillStyle = '#9575cd';
        ctx.fillRect(tx + 10, ty + size - 28 - bounce, 28, 24);
        ctx.fillStyle = 'white';
        ctx.fillRect(tx + 30, ty + size - 20 - bounce, 12, 12);
    }
}

function drawNPC(npc, offX, offY, time) {
    const tx = offX + npc.Position.X * TILE_SIZE * SCALE;
    const ty = offY + npc.Position.Y * TILE_SIZE * SCALE;
    const size = TILE_SIZE * SCALE;
    const bounce = Math.abs(Math.sin(time / 400)) * 3;

    ctx.fillStyle = npc.Type === "Smith" ? '#757575' : (npc.Type === "Innkeeper" ? '#8d6e63' : '#ce93d8');
    ctx.fillRect(tx + 12, ty + 8 - bounce, 24, 48);
    ctx.fillStyle = '#ffccbc';
    ctx.fillRect(tx + 16, ty + 12 - bounce, 16, 16);
}

function getItemIcon(item) {
    if (item.Type === 'Tool') {
        if (item.Name.includes('Pickaxe')) return '⛏️';
        if (item.Name.includes('Axe')) return '🪓';
        if (item.Name.includes('Hoe')) return '🌾';
        if (item.Name.includes('Watering')) return '💧';
    }
    if (item.Type === 'Seed') return '🌱';
    if (item.Type === 'Resource') {
        if (item.Name === 'Stone') return '🪨';
        if (item.Name === 'Iron Ore') return '⛓️';
        if (item.Name === 'Wood') return '🪵';
    }
    return item.Name[0];
}
