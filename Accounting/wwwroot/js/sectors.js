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
  // Map: "SectorX:SectorY" -> Mesh
  const sectorMeshes = new Map();

  // Call this with the array of claims [{SectorX, SectorY, ...}]
  function updateSectors(claims) {
    const keepKeys = new Set();

    for (const claim of claims) {
      const key = `${claim.sectorX ?? claim.SectorX}:${claim.sectorY ?? claim.SectorY}`;
      keepKeys.add(key);

      if (!sectorMeshes.has(key)) {
        const mesh = new THREE.Mesh(
          new THREE.PlaneGeometry(gridSize, gridSize),
          new THREE.MeshBasicMaterial({
            color: color,
            transparent: true,
            opacity: opacity,
            depthTest: false
          })
        );
        // Calculate center position of sector
        const x = (claim.sectorX ?? claim.SectorX) * gridSize + gridSize / 2;
        const y = height - ((claim.sectorY ?? claim.SectorY) * gridSize + gridSize / 2);
        mesh.position.set(x, y, z);
        sectorMeshes.set(key, mesh);
        scene.add(mesh);
      }
    }

    // Remove any old meshes not in the current claims
    for (const [key, mesh] of sectorMeshes) {
      if (!keepKeys.has(key)) {
        scene.remove(mesh);
        sectorMeshes.delete(key);
      }
    }
  }

  return {
    updateSectors
  };
}