// js/sectors.js
// Module to display shaded claimed sectors on the game grid using Three.js

export function initSectors({
  THREE,
  scene,
  gridSize,
  width,
  height,
  color = 0x99ccff, // Light blue
  opacity = 0.25,
  z = 0.05
}) {
  // Map: "SectorX:SectorY" -> { mesh, timeoutId }
  const sectorMeshes = new Map();

  function removeSector(key) {
    const entry = sectorMeshes.get(key);
    if (!entry) return;

    if (entry.timeoutId) {
      clearTimeout(entry.timeoutId);
    }

    scene.remove(entry.mesh);
    entry.mesh.geometry.dispose();
    entry.mesh.material.dispose();
    sectorMeshes.delete(key);
  }

  function scheduleExpiration(key, occupyUntil) {
    if (!occupyUntil) return null;

    const expireAt = new Date(occupyUntil).getTime();
    if (Number.isNaN(expireAt)) return null;

    const msUntilExpire = expireAt - Date.now();

    if (msUntilExpire <= 0) {
      setTimeout(() => removeSector(key), 0);
      return null;
    }

    return setTimeout(() => {
      removeSector(key);
    }, msUntilExpire);
  }

  // Call this with the array of claims [{ sectorX, sectorY, occupyUntil, ... }]
  function updateSectors(claims) {
    if (!Array.isArray(claims)) return;

    const keepKeys = new Set();

    for (const claim of claims) {
      const sectorX = claim.sectorX ?? claim.SectorX;
      const sectorY = claim.sectorY ?? claim.SectorY;
      const occupyUntil = claim.occupyUntil ?? claim.OccupyUntil;

      if (sectorX == null || sectorY == null) continue;

      const key = `${sectorX}:${sectorY}`;
      keepKeys.add(key);

      let entry = sectorMeshes.get(key);

      if (!entry) {
        const mesh = new THREE.Mesh(
          new THREE.PlaneGeometry(gridSize, gridSize),
          new THREE.MeshBasicMaterial({
            color: color,
            transparent: true,
            opacity: opacity,
            depthTest: false
          })
        );

        const x = sectorX * gridSize + gridSize / 2;
        const y = height - (sectorY * gridSize + gridSize / 2);
        mesh.position.set(x, y, z);

        scene.add(mesh);

        entry = {
          mesh,
          timeoutId: null
        };

        sectorMeshes.set(key, entry);
      }

      if (entry.timeoutId) {
        clearTimeout(entry.timeoutId);
        entry.timeoutId = null;
      }

      entry.timeoutId = scheduleExpiration(key, occupyUntil);
    }

    // Remove any old meshes not in the current claims
    for (const [key] of sectorMeshes) {
      if (!keepKeys.has(key)) {
        removeSector(key);
      }
    }
  }

  return {
    updateSectors
  };
}