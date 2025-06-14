// js/player.js
// -- Player module for Game.cshtml --
// Exports: initPlayers, updatePlayers, handlePlayerClick, handlePlayerMove, getPlayerPixels

export function initPlayers({
  THREE,
  scene,
  width,
  height,
  pixelSize,
  userId
}) {
  // -- Player Data Structures --
  // userId -> { mesh, label, anim: {from:{x,y}, to:{x,y}, start:timestamp, duration:ms} }
  const playerPixels = new Map();
  let latestPlayersFromServer = [];
  let lastSentX = null, lastSentY = null;
  let needsRender = true;

  // -- Helpers --
  function lerp(a, b, t) {
    return a + (b - a) * t;
  }

  // --- Label helpers ---
  function createLabel(text) {
    // Create a canvas for the label
    const fontSize = 18;
    const padding = 6;
    const font = `${fontSize}px Arial`;
    const canvas = document.createElement('canvas');
    const ctx = canvas.getContext('2d');
    ctx.font = font;
    const textWidth = Math.ceil(ctx.measureText(text).width);
    const textHeight = fontSize + 2;
    canvas.width = textWidth + padding * 2;
    canvas.height = textHeight + padding * 2;

    // Draw background
    ctx.fillStyle = 'rgba(32,32,32,0.85)';
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    // Draw border
    ctx.strokeStyle = 'rgba(255,255,255,0.25)';
    ctx.lineWidth = 2;
    ctx.strokeRect(0, 0, canvas.width, canvas.height);

    // Draw text
    ctx.font = font;
    ctx.fillStyle = '#fff';
    ctx.textBaseline = 'top';
    ctx.fillText(text, padding, padding);

    // Create texture and mesh
    const texture = new THREE.CanvasTexture(canvas);
    texture.needsUpdate = true;
    const material = new THREE.SpriteMaterial({ map: texture, transparent: true });
    const sprite = new THREE.Sprite(material);

    // Set scale to match canvas size (1:1 pixel)
    sprite.scale.set(canvas.width, canvas.height, 1);

    // Store label dimensions for positioning
    sprite.userData = {
      labelWidth: canvas.width,
      labelHeight: canvas.height
    };

    return sprite;
  }

  // Always anchor: bottom right of label to top left of pixel, offset 2px up and left
  function positionLabel(label, pixelPos, labelDims) {
    const offset = 2;
    // The label's bottom right corner is at (pixelPos.x - offset, pixelPos.y - offset)
    // So the label's center is:
    const labelX = pixelPos.x - offset - labelDims.labelWidth / 2;
    const labelY = pixelPos.y - offset - labelDims.labelHeight / 2;
    label.position.set(labelX, labelY, pixelPos.z + 0.01);
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
      }
    } catch (err) { }
  }

  // -- Public Functions --

  // Handle player click to claim
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

  // Periodic keepalive to report last position
  setInterval(() => {
    if (lastSentX !== null && lastSentY !== null) {
      sendCoordinates(lastSentX, lastSentY, false);
    }
  }, 1200);

  // Update player pixels and handle animation
  function updatePlayers(now) {
    now = now || performance.now();

    // 1. Remove pixels/labels not in latest player list
    const keepIds = new Set();
    for (let i = 0; i < latestPlayersFromServer.length; ++i) {
      const p = latestPlayersFromServer[i];
      let id = p.userId || (p.x + ':' + p.y + ':' + i);
      keepIds.add(id);
    }
    for (const [id, pixel] of playerPixels) {
      if (!keepIds.has(id)) {
        scene.remove(pixel.mesh);
        if (pixel.label) scene.remove(pixel.label);
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

        // Create label (show only for self and maybe a few others)
        let label = null;
        if (isSelf || i < 5) { // Show for self and first 5 others
          const labelText = isSelf ? "You" : (p.userId ? p.userId.slice(0, 6) : "Player");
          label = createLabel(labelText);
          scene.add(label);
        }

        playerPixels.set(id, {
          mesh,
          label,
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

    // 3. Animate all player pixels and update label positions
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

      // Update label position if exists
      if (pixel.label) {
        positionLabel(
          pixel.label,
          { x: curX, y: curY, z: pixel.mesh.position.z },
          pixel.label.userData
        );
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