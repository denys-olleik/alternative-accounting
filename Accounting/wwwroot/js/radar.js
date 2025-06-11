// js/radar.js
// Radar scan module for multiplayer grid game, with left-to-right horizontal red gradient

export function initRadar({
  THREE,
  scene,
  width,
  height,
  scanWidth = 200,
  duration = 5000,
  color = 0xff2222,
  opacity = 0.3,
  z = 0.4
}) {
  // --- Helper: Create left-to-right red gradient texture ---
  function createRedGradientTexture() {
    const size = 128;
    const canvas = document.createElement('canvas');
    canvas.width = size;
    canvas.height = 1;
    const ctx = canvas.getContext('2d');

    // Gradient: left (bright) -> right (dark/transparent)
    const grad = ctx.createLinearGradient(0, 0, size, 0);
    grad.addColorStop(0, 'rgba(255,34,34,1)');    // Left: solid red
    grad.addColorStop(0.7, 'rgba(255,34,34,0.48)'); // Middle: semi-transparent
    grad.addColorStop(1, 'rgba(255,34,34,0.03)'); // Right: almost invisible

    ctx.fillStyle = grad;
    ctx.fillRect(0, 0, size, 1);

    const texture = new THREE.Texture(canvas);
    texture.needsUpdate = true;
    texture.wrapS = THREE.ClampToEdgeWrapping;
    texture.wrapT = THREE.ClampToEdgeWrapping;
    texture.magFilter = THREE.LinearFilter;
    texture.minFilter = THREE.LinearFilter;
    return texture;
  }

  // --- State ---
  let radarMesh = null;
  let lastSweepId = -1;

  // Map: pixelId => { isInRadar: bool, enteredAt: ms }
  const pixelRadarStates = new Map();

  // Create radar rectangle mesh with gradient
  function createRadarMesh() {
    if (radarMesh) scene.remove(radarMesh);
    const geom = new THREE.PlaneGeometry(scanWidth, height);

    // Use the gradient texture
    const gradientTexture = createRedGradientTexture();

    const mat = new THREE.MeshBasicMaterial({
      color: 0xffffff, // So the gradient is not tinted
      map: gradientTexture,
      transparent: true,
      opacity: 1.0, // Opacity handled in gradient stops
      depthTest: false
    });
    radarMesh = new THREE.Mesh(geom, mat);
    radarMesh.position.set(-100, height / 2, z); // Start offscreen
    scene.add(radarMesh);
  }

  createRadarMesh();

  // Called every frame by main loop
  // playerPixels: Map<id, {mesh, ...}>
  function updateRadar(playerPixels, now = performance.now()) {
    // Update radar position
    const t = now;
    const cycleTime = t % duration;
    const sweepId = Math.floor(t / duration);
    const radarX = (cycleTime / duration) * width;
    const left = radarX - scanWidth / 2;
    const right = radarX + scanWidth / 2;

    // Move radar mesh
    radarMesh.position.set(radarX, height / 2, z);

    // Track which pixels are in the radar band this frame
    for (const [id, pixel] of playerPixels) {
      const meshX = pixel.mesh.position.x;
      const isInRadar = meshX >= left && meshX < right;

      let state = pixelRadarStates.get(id);
      if (!state) {
        state = { isInRadar: false, enteredAt: 0, leftAt: 0 };
        pixelRadarStates.set(id, state);
      }

      // Entering radar band
      if (!state.isInRadar && isInRadar) {
        state.isInRadar = true;
        state.enteredAt = t;
        pixel.mesh.scale.set(4, 4, 1); // 4x scale instantly
      }
      // Leaving radar band
      if (state.isInRadar && !isInRadar) {
        state.isInRadar = false;
        state.leftAt = t;
      }
    }

    // Animate deflation for pixels that left radar band
    for (const [id, state] of pixelRadarStates) {
      if (!playerPixels.has(id)) {
        pixelRadarStates.delete(id);
        continue;
      }
      const pixel = playerPixels.get(id);
      if (!state.isInRadar && state.leftAt > 0) {
        const dt = t - state.leftAt;
        const deflateDuration = 1000; // ms
        if (dt < deflateDuration) {
          // Ease from 4x back to 1x over 1s
          const scale = 1 + 3 * (1 - dt / deflateDuration);
          pixel.mesh.scale.set(scale, scale, 1);
        } else {
          pixel.mesh.scale.set(1, 1, 1);
          pixelRadarStates.delete(id);
        }
      }
      // If not in radar and not animating, ensure normal size
      if (!state.isInRadar && state.leftAt === 0) {
        pixel.mesh.scale.set(1, 1, 1);
      }
    }
  }

  // Expose
  return {
    updateRadar
  };
}