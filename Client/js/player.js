async function tryFish() {
    if (!gameState) return;
    const player = gameState.Player;
    const selectedItem = player.Inventory[player.SelectedSlot];

    if (!selectedItem || !selectedItem.Name.includes("Rod")) {
        showNotification("Musisz trzymać wędkę!", '#ff7043');
        return;
    }

    let targetX = player.Position.X;
    let targetY = player.Position.Y;
    
    if (player.Facing === 0) targetY--; // Up
    else if (player.Facing === 1) targetY++; // Down
    else if (player.Facing === 2) targetX--; // Left
    else if (player.Facing === 3) targetX++; // Right

    const tile = getTileAt(targetX, targetY);
    if (tile && (tile.Type === 2 || tile.Type === 8)) { // Water or DeepWater
        showNotification("Zarzucono wędkę...");
        const response = await fetch(`/api/game/action/fish?roomId=${roomId}`, { method: 'POST' });
        const result = await response.json();
        
        if (result.Success) {
            showNotification(`Złowiono: ${result.Fish.Name}!`, '#4fc3f7');
            fetchGameState();
        } else {
            showNotification(result.Message || "Nic nie bierze...", '#78909c');
        }
    } else {
        showNotification("Musisz stać przodem do wody!", '#ff7043');
    }
}

function showNotification(text, color = '#fff') {
    let notify = document.getElementById('notification');
    if (!notify) {
        notify = document.createElement('div');
        notify.id = 'notification';
        notify.style = 'position: fixed; top: 20px; left: 50%; transform: translateX(-50%); background: rgba(0,0,0,0.8); color: #fff; padding: 15px 30px; border-radius: 20px; font-family: sans-serif; transition: opacity 0.5s; z-index: 1000;';
        document.body.appendChild(notify);
    }
    notify.innerText = text;
    notify.style.color = color;
    notify.style.opacity = '1';
    
    if (window.notifyTimeout) clearTimeout(window.notifyTimeout);
    window.notifyTimeout = setTimeout(() => {
        notify.style.opacity = '0';
    }, 2000);
}

async function selectSlot(index) {
    await fetch(`/api/game/select-slot?slot=${index}&roomId=${roomId}`, { method: 'POST' });
    fetchGameState();
}

async function handleInput(e) {
    if (!gameState) return;
    if (e.key >= '1' && e.key <= '9') {
        selectSlot(parseInt(e.key) - 1);
        return;
    }

    let direction = -1;
    switch(e.key.toLowerCase()) {
        case 'w': direction = 0; break;
        case 's': direction = 1; break;
        case 'a': direction = 2; break;
        case 'd': direction = 3; break;
        case 'r': await fetch(`/api/game/sleep?roomId=${roomId}`, { method: 'POST' }); fetchGameState(); return;
        case 'f': await tryFish(); return;
    }

    if (direction !== -1) {
        AudioEngine.playSound('walk');
        const response = await fetch(`/api/game/move?roomId=${roomId}&direction=${direction}`, {
            method: 'POST'
        });
        const result = await response.json();
        if (result.Success) {
            gameState.Player.Position = result.Position;
            gameState.Player.Facing = result.Facing;
            connection.invoke("UpdatePosition", roomId, result.Position.X, result.Position.Y, direction);
        }
    }
}

async function handleMouse(e) {
    if (!gameState) return;
    const rect = canvas.getBoundingClientRect();
    const scaleX = canvas.width / rect.width;
    const scaleY = canvas.height / rect.height;
    const x = (e.clientX - rect.left) * scaleX;
    const y = (e.clientY - rect.top) * scaleY;

    const player = gameState.Player;
    const offsetX = canvas.width / 2 - (player.Position.X * TILE_SIZE * SCALE);
    const offsetY = canvas.height / 2 - (player.Position.Y * TILE_SIZE * SCALE);

    const tileX = Math.floor((x - offsetX) / (TILE_SIZE * SCALE));
    const tileY = Math.floor((y - offsetY) / (TILE_SIZE * SCALE));

    const dist = Math.sqrt(Math.pow(tileX - player.Position.X, 2) + Math.pow(tileY - player.Position.Y, 2));
    if (dist > 5) return;

    const npc = gameState.NPCs.find(n => n.Position.X === tileX && n.Position.Y === tileY);
    if (npc) {
        if (e.altKey) {
            const response = await fetch(`/api/game/action/propose?npcName=${npc.Name}&roomId=${roomId}`, { method: 'POST' });
            const result = await response.json();
            alert(result.Message);
        } else if (e.shiftKey) {
            // Give gift
            const response = await fetch(`/api/game/action/give-gift?npcName=${npc.Name}&roomId=${roomId}`, { method: 'POST' });
            const result = await response.json();
            if (result.Success) {
                alert(`${npc.Name}: ${result.Message}`);
                fetchGameState();
            }
        } else {
            // Talk or Special interaction
            if (npc.Type === "Smith") {
                const tool = player.Inventory[player.SelectedSlot];
                if (tool && tool.Type === "Tool") {
                    if (confirm(`Ulepszyć ${tool.Name} za 500g?`)) {
                        await fetch(`/api/game/action/upgrade-tool?toolName=${tool.Name}&roomId=${roomId}`, { method: 'POST' });
                        fetchGameState();
                        return;
                    }
                }
            }
            
            const response = await fetch(`/api/game/action/talk?npcName=${npc.Name}&roomId=${roomId}`, { method: 'POST' });
            const result = await response.json();
            alert(`${npc.Name}: ${result.Dialogue}`);
            fetchGameState();
        }
        return;
    }

    const tile = getTileAt(tileX, tileY);
    if (tile && tile.Object && tile.Object.Type === 5) { // 5 is ShippingBin in ObjectType enum
        const response = await fetch(`/api/game/action/ship-item?roomId=${roomId}`, { method: 'POST' });
        const result = await response.json();
        if (result.Success) {
            fetchGameState();
        } else if (result.Message) {
            alert(result.Message);
        }
        return;
    }

    const selectedItem = gameState.Player.Inventory[gameState.Player.SelectedSlot];
    
    let endpoint = `/api/game/use-tool?x=${tileX}&y=${tileY}&roomId=${roomId}`;
    let actionType = "use-tool";
    if (e.shiftKey && selectedItem?.Type === "Seed") {
        endpoint = `/api/game/plant?x=${tileX}&y=${tileY}&seedName=${selectedItem.Name}&roomId=${roomId}`;
        actionType = "plant";
    }

    const response = await fetch(endpoint, { method: 'POST' });
    const result = await response.json();
    if (result.Success) {
        AudioEngine.playSound('tool');
        gameState = result.GameState;
        updateUI({ GameState: gameState, DisplayTime: document.getElementById('time-display').innerText });
        connection.invoke("ActionPerformed", roomId, actionType, tileX, tileY);
    }
}

function drawPlayer(x, y, facing, offX, offY, time, isOther) {
    const px = offX + x * TILE_SIZE * SCALE + (TILE_SIZE * SCALE / 4);
    const py = offY + y * TILE_SIZE * SCALE;
    const pSize = TILE_SIZE * SCALE / 2;
    const bounce = Math.abs(Math.sin(time / 200 + (isOther ? 1 : 0))) * 4;

    ctx.fillStyle = 'rgba(0,0,0,0.2)';
    ctx.beginPath();
    ctx.ellipse(px + pSize/2, py + pSize, pSize/2, pSize/4, 0, 0, Math.PI * 2);
    ctx.fill();

    ctx.fillStyle = isOther ? '#e67e22' : COLORS.PlayerShirt;
    ctx.fillRect(px, py + pSize * 0.4 - bounce, pSize, pSize * 0.4);
    
    ctx.fillStyle = isOther ? '#d35400' : COLORS.PlayerPants;
    ctx.fillRect(px, py + pSize * 0.8 - bounce * 0.5, pSize, pSize * 0.2);
    
    ctx.fillStyle = COLORS.PlayerSkin;
    ctx.fillRect(px + pSize * 0.1, py - bounce, pSize * 0.8, pSize * 0.4);
    
    ctx.fillStyle = isOther ? '#2c3e50' : COLORS.PlayerHair;
    ctx.fillRect(px + pSize * 0.1, py - bounce, pSize * 0.8, pSize * 0.15);

    ctx.fillStyle = '#000';
    if (facing === 1) { // Down
        ctx.fillRect(px + pSize * 0.2, py + pSize * 0.1 - bounce, 4, 4);
        ctx.fillRect(px + pSize * 0.6, py + pSize * 0.1 - bounce, 4, 4);
    } else if (facing === 0) { // Up
    } else if (facing === 2) { // Left
        ctx.fillRect(px + pSize * 0.1, py + pSize * 0.1 - bounce, 4, 4);
    } else if (facing === 3) { // Right
        ctx.fillRect(px + pSize * 0.7, py + pSize * 0.1 - bounce, 4, 4);
    }
}
