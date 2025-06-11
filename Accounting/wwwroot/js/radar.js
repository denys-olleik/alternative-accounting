// js/radar.js
// Radar scan module for multiplayer grid game

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
  // State
  let radarMesh = null;
  let lastSweepId = -1;

  // Map: pixelId => { isInRadar: bool, enteredAt: ms }
  const pixelRadarStates = new Map();

  // Create radar rectangle mesh
  function createRadarMesh() {
    if (radarMesh) scene.remove(radarMesh);
    const geom = new THREE.PlaneGeometry(scanWidth, height);
    const mat = new THREE.MeshBasicMaterial({
      color,
      transparent: true,
      opacity,
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