// js/sectors.js
// Module to display shaded claimed hex sectors on the game grid using Three.js

export function initSectors({
  THREE,
  scene,
  width,
  height,
  hexSize,
  hexMapRadius,
  hexOrigin,
  color = 0x99ccff,
  opacity = 0.25,
  z = 0.05
}) {
  // Map: "SectorX:SectorY" -> { mesh, timeoutId }
  const sectorMeshes = new Map();

  function axialToPixel(q, r) {
    // Pointy-top hex layout
    const x = hexSize * Math.sqrt(3) * (q + r / 2);
    const y = hexSize * 1.5 * r;

    return {
      x: hexOrigin.x + x,
      y: hexOrigin.y + y
    };
  }

  function isInsideHexMap(q, r) {
    const s = -q - r;
    return Math.max(Math.abs(q), Math.abs(r), Math.abs(s)) < hexMapRadius;
  }

  function createHexShape(size) {
    const shape = new THREE.Shape();

    for (let i = 0; i < 6; i++) {
      const angle = THREE.MathUtils.degToRad(60 * i - 30);
      const x = size * Math.cos(angle);
      const y = size * Math.sin(angle);

      if (i === 0) {
        shape.moveTo(x, y);
      } else {
        shape.lineTo(x, y);
      }
    }

    shape.closePath();
    return shape;
  }

  const hexGeometry = new THREE.ShapeGeometry(createHexShape(hexSize));

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

      const q = sectorX;
      const r = sectorY;

      if (!isInsideHexMap(q, r)) continue;

      const key = `${q}:${r}`;
      keepKeys.add(key);

      let entry = sectorMeshes.get(key);

      if (!entry) {
        const mesh = new THREE.Mesh(
          hexGeometry.clone(),
          new THREE.MeshBasicMaterial({
            color: color,
            transparent: true,
            opacity: opacity,
            depthTest: false
          })
        );

        const pos = axialToPixel(q, r);
        mesh.position.set(pos.x, pos.y, z);

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