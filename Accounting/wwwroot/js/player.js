// js/player.js
// -- Player module for Game.cshtml --
// Exports: initPlayers, updatePlayers, handlePlayerClick, handlePlayerMove, getPlayerPixels

export function initPlayers({
  THREE,
  scene,
  width,
  height,
  pixelSize,
  userId,
  onSectorClaims = null // Callback to receive latest sector claims after every position report
}) {
  // -- Player Data Structures --
  // userId -> { mesh, anim: {from:{x,y}, to:{x,y}, start:timestamp, duration:ms} }
  const playerPixels = new Map();
  let latestPlayersFromServer = [];
  let lastSentX = null, lastSentY = null;
  let needsRender = true;

  // -- Helpers --
  function lerp(a, b, t) {
    return a + (b - a) * t;
  }

  // --- API communication ---
  // Modified: claim parameter (default false)
  async function sendCoordinates(x, y, claim = false) {
    try {
      const response = await fetch('/api/player/report-position', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ x, y, userId, claim })
      });
      if (response.ok) {
        const data = await response.json();
        let list = data.players || data.Players;
        if (Array.isArray(list)) {
          latestPlayersFromServer = list.map((p, i) => ({
            x: p.x ?? p.X,
            y: p.y ?? p.Y,
            userId: p.userId || p.UserId || null
          }));
          needsRender = true;
        }
        // Always call onSectorClaims with latest claims if provided
        if (typeof onSectorClaims === "function") {
          const claims = data.sectorClaims || data.SectorClaims || [];
          onSectorClaims(claims);
        }
      }
    } catch (err) { }
  }

  // -- Public Functions --

  // Handle player click to claim (sector claims will update via onSectorClaims)
  async function handlePlayerClick(clickX, clickY) {
    await sendCoordinates(clickX, clickY, true);
    lastSentX = clickX;
    lastSentY = clickY;
  }

  // Handle player movement (report only if moved)
  async function handlePlayerMove(mouseX, mouseY, mouseOverCanvas) {
    if (mouseOverCanvas) {
      if (mouseX !== lastSentX || mouseY !== lastSentY) {
        await sendCoordinates(mouseX, mouseY, false);
        lastSentX = mouseX;
        lastSentY = mouseY;
      }
    }
  }

  // Update player pixels and handle animation
  function updatePlayers(now) {
    now = now || performance.now();

    // 1. Remove pixels not in latest player list
    const keepIds = new Set();
    for (let i = 0; i < latestPlayersFromServer.length; ++i) {
      const p = latestPlayersFromServer[i];
      let id = p.userId || (p.x + ':' + p.y + ':' + i);
      keepIds.add(id);
    }
    for (const [id, pixel] of playerPixels) {
      if (!keepIds.has(id)) {
        scene.remove(pixel.mesh);
        playerPixels.delete(id);
        needsRender = true;
      }
    }

    // 2. Update/create player pixels and animation targets
    for (let i = 0; i < latestPlayersFromServer.length; ++i) {
      const p = latestPlayersFromServer[i];
      const id = p.userId || (p.x + ':' + p.y + ':' + i);
      const isSelf = (id === userId);
      const targetAbsPos = {
        x: p.x + 0.5, // center of pixel
        y: height - p.y - 0.5, // flip Y, center of pixel
        z: isSelf ? 0.25 : 0.2
      };

      let pixel = playerPixels.get(id);

      if (!pixel) {
        // Create mesh at target (no animation on first appearance)
        const mesh = new THREE.Mesh(
          new THREE.PlaneGeometry(pixelSize, pixelSize),
          new THREE.MeshBasicMaterial({
            color: isSelf ? 0x44ff44 : 0xffffff,
            opacity: 1,
            transparent: false,
            depthTest: false
          })
        );
        mesh.position.set(targetAbsPos.x, targetAbsPos.y, targetAbsPos.z);
        scene.add(mesh);

        playerPixels.set(id, {
          mesh,
          anim: {
            from: { x: targetAbsPos.x, y: targetAbsPos.y },
            to: { x: targetAbsPos.x, y: targetAbsPos.y },
            start: now,
            duration: 1000
          }
        });
        needsRender = true;
      } else {
        // Only start new animation if destination changed
        const prevTo = pixel.anim.to;
        if (prevTo.x !== targetAbsPos.x || prevTo.y !== targetAbsPos.y) {
          // Continue from wherever it is (even if in the middle of anim)
          let t = Math.min(1, (now - pixel.anim.start) / pixel.anim.duration);
          const curX = lerp(pixel.anim.from.x, pixel.anim.to.x, t);
          const curY = lerp(pixel.anim.from.y, pixel.anim.to.y, t);

          pixel.anim = {
            from: { x: curX, y: curY },
            to: { x: targetAbsPos.x, y: targetAbsPos.y },
            start: now,
            duration: 1000
          };
          needsRender = true;
        }
      }
    }

    // 3. Animate all player pixels
    for (const [id, pixel] of playerPixels) {
      let t = Math.min(1, (now - pixel.anim.start) / pixel.anim.duration);
      let curX, curY;
      if (t < 1) {
        curX = lerp(pixel.anim.from.x, pixel.anim.to.x, t);
        curY = lerp(pixel.anim.from.y, pixel.anim.to.y, t);
        pixel.mesh.position.set(curX, curY, pixel.mesh.position.z);
        needsRender = true;
      } else {
        curX = pixel.anim.to.x;
        curY = pixel.anim.to.y;
        pixel.mesh.position.set(curX, curY, pixel.mesh.position.z);
      }
      // Color for self vs others
      if (id === userId) {
        pixel.mesh.material.color.setHex(0x44ff44);
      } else {
        pixel.mesh.material.color.setHex(0xffffff);
      }
    }

    return needsRender;
  }

  // Expose only what is needed
  return {
    updatePlayers,
    handlePlayerClick,
    handlePlayerMove,
    setNeedsRender: (v) => { needsRender = v; },
    getNeedsRender: () => needsRender,
    getPlayerPixels: () => playerPixels
  };
}