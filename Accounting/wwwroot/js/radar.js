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
  function createRedGradientTexture() {
    const size = 128;
    const canvas = document.createElement('canvas');
    canvas.width = size;
    canvas.height = 1;
    const ctx = canvas.getContext('2d');
    const grad = ctx.createLinearGradient(0, 0, size, 0);
    grad.addColorStop(0, 'rgba(255,34,34,0.0)');
    grad.addColorStop(1, 'rgba(255,34,34,0.5)');
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

  let radarMesh = null;
  const pixelRadarStates = new Map();

  function createRadarMesh() {
    if (radarMesh) scene.remove(radarMesh);
    const geom = new THREE.PlaneGeometry(scanWidth, height);
    const gradientTexture = createRedGradientTexture();
    const mat = new THREE.MeshBasicMaterial({
      color: 0xffffff,
      map: gradientTexture,
      transparent: true,
      opacity: 1.0,
      depthTest: false
    });
    radarMesh = new THREE.Mesh(geom, mat);
    radarMesh.position.set(-scanWidth * 1.5, height / 2, z); // Start fully off left
    scene.add(radarMesh);
  }
  createRadarMesh();

  // New: Sweep from center at -scanWidth*1.5 (fully off left), to center at width + scanWidth*1.5 (fully off right)
  const sweepStart = -scanWidth * 1.5;
  const sweepEnd = width + scanWidth * 1.5;
  const sweepSpan = sweepEnd - sweepStart;

  function updateRadar(playerPixels, now = performance.now()) {
    const t = now;
    const cycleTime = t % duration;
    const frac = cycleTime / duration;
    const radarX = sweepStart + frac * sweepSpan;

    radarMesh.position.set(radarX, height / 2, z);

    const left = radarX - scanWidth / 2;
    const right = radarX + scanWidth / 2;
    radarMesh.visible = (right > 0) && (left < width);

    for (const [id, pixel] of playerPixels) {
      const meshX = pixel.mesh.position.x;
      const isInRadar = meshX >= left && meshX < right;
      let state = pixelRadarStates.get(id);
      if (!state) {
        state = { isInRadar: false, enteredAt: 0, leftAt: 0 };
        pixelRadarStates.set(id, state);
      }
      if (!state.isInRadar && isInRadar) {
        state.isInRadar = true;
        state.enteredAt = t;
        pixel.mesh.scale.set(4, 4, 1);
      }
      if (state.isInRadar && !isInRadar) {
        state.isInRadar = false;
        state.leftAt = t;
      }
    }

    for (const [id, state] of pixelRadarStates) {
      if (!playerPixels.has(id)) {
        pixelRadarStates.delete(id);
        continue;
      }
      const pixel = playerPixels.get(id);
      if (!state.isInRadar && state.leftAt > 0) {
        const dt = t - state.leftAt;
        const deflateDuration = 1000;
        if (dt < deflateDuration) {
          const scale = 1 + 3 * (1 - dt / deflateDuration);
          pixel.mesh.scale.set(scale, scale, 1);
        } else {
          pixel.mesh.scale.set(1, 1, 1);
          pixelRadarStates.delete(id);
        }
      }
      if (!state.isInRadar && state.leftAt === 0) {
        pixel.mesh.scale.set(1, 1, 1);
      }
    }
  }

  return {
    updateRadar
  };
}