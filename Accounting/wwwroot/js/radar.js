// js/radar.js
// Simple Radar Sweep Rectangle (static, left side, 500px wide)

export function initRadar({
  THREE,
  scene,
  width,
  height,
  z = 0.18,
  color = 0x22ffcc,
  opacity = 0.23
}) {
  // Create a rectangle: 500px wide, full canvas height, positioned at x = 0
  const radarWidth = 500;
  const radarGeometry = new THREE.PlaneGeometry(radarWidth, height);
  const radarMaterial = new THREE.MeshBasicMaterial({
    color: color,
    transparent: true,
    opacity: opacity,
    depthTest: false
  });

  const radarMesh = new THREE.Mesh(radarGeometry, radarMaterial);

  // Position: left edge, so center is at (radarWidth/2, height/2)
  radarMesh.position.set(radarWidth / 2, height / 2, z);

  scene.add(radarMesh);

  // No animation or update for now
  return {
    radarMesh
  };
}