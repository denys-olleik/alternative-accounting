// js/radar.js

export function createRadar({
  THREE,
  scene,
  width,
  height,
  gradientWidth = 500,
  scanSpeed = 2
}) {
  const scanLineMaterial = new THREE.LineBasicMaterial({ color: 0xff0000, linewidth: 2 }); // RED
  let scanLineX = 0;
  let scanLine;
  let gradientMesh;

  function createScanLine(x) {
    if (scanLine) scene.remove(scanLine);
    const scanGeom = new THREE.BufferGeometry().setFromPoints([
      new THREE.Vector3(x, 0, 0.3),
      new THREE.Vector3(x, height, 0.3)
    ]);
    scanLine = new THREE.Line(scanGeom, scanLineMaterial);
    scene.add(scanLine);
  }

  function createOrUpdateGradient(x) {
    if (gradientMesh) scene.remove(gradientMesh);

    // The gradient should always be 500px wide, with its right edge at 'x'.
    // When x > width, it should continue off the right side.
    let leftEdge = x - gradientWidth;
    let rightEdge = x;

    const w = gradientWidth;
    if (rightEdge <= 0) return;

    const geometry = new THREE.PlaneGeometry(w, height);

    const material = new THREE.ShaderMaterial({
      transparent: true,
      uniforms: {
        color: { value: new THREE.Color(0xff0000) },
        opacityStart: { value: 0.5 },
        opacityEnd: { value: 0.0 }
      },
      vertexShader: `
        varying vec2 vUv;
        void main() {
          vUv = uv;
          gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0);
        }
      `,
      fragmentShader: `
        uniform vec3 color;
        uniform float opacityStart;
        uniform float opacityEnd;
        varying vec2 vUv;
        void main() {
          float alpha = mix(opacityEnd, opacityStart, vUv.x);
          gl_FragColor = vec4(color, alpha);
        }
      `
    });

    gradientMesh = new THREE.Mesh(geometry, material);
    gradientMesh.position.set(leftEdge + w / 2, height / 2, 0.29);
    scene.add(gradientMesh);
  }

  function update() {
    scanLineX += scanSpeed;
    if (scanLineX > width + gradientWidth) scanLineX = 0;
    createScanLine(scanLineX);
    createOrUpdateGradient(scanLineX);
  }

  function setScanLineX(x) {
    scanLineX = x;
    createScanLine(scanLineX);
    createOrUpdateGradient(scanLineX);
  }

  function getScanLineX() {
    return scanLineX;
  }

  function dispose() {
    if (scanLine) scene.remove(scanLine);
    if (gradientMesh) scene.remove(gradientMesh);
    scanLine = null;
    gradientMesh = null;
  }

  return {
    update,
    setScanLineX,
    getScanLineX,
    dispose
  };
}