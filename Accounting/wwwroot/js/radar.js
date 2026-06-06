// js/radar.js

export function createRadar({
  THREE,
  scene,
  width,
  height,
  gradientWidth = 500,
  scanSpeed = 2
}) {
  const scanLineMaterial = new THREE.LineBasicMaterial({ color: 0xff0000, linewidth: 2 });
  let scanLineX = 0;
  let scanLine;
  let gradientMesh;
  let active = false;

  function removeScanLine() {
    if (scanLine) {
      scene.remove(scanLine);
      scanLine.geometry.dispose();
      scanLine = null;
    }
  }

  function removeGradient() {
    if (gradientMesh) {
      scene.remove(gradientMesh);
      gradientMesh.geometry.dispose();
      gradientMesh.material.dispose();
      gradientMesh = null;
    }
  }

  function hide() {
    removeScanLine();
    removeGradient();
    active = false;
  }

  function createScanLine(x) {
    removeScanLine();

    const scanGeom = new THREE.BufferGeometry().setFromPoints([
      new THREE.Vector3(x, 0, 0.3),
      new THREE.Vector3(x, height, 0.3)
    ]);

    scanLine = new THREE.Line(scanGeom, scanLineMaterial);
    scene.add(scanLine);
  }

  function createOrUpdateGradient(x) {
    removeGradient();

    const leftEdge = x - gradientWidth;
    const rightEdge = x;

    if (rightEdge <= 0) return;

    const geometry = new THREE.PlaneGeometry(gradientWidth, height);

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
    gradientMesh.position.set(leftEdge + gradientWidth / 2, height / 2, 0.29);
    scene.add(gradientMesh);
  }

  function restart() {
    scanLineX = 0;
    active = true;
    createScanLine(scanLineX);
    createOrUpdateGradient(scanLineX);
  }

  function update() {
    if (!active) return;

    scanLineX += scanSpeed;

    if (scanLineX > width + gradientWidth) {
      hide();
      return;
    }

    createScanLine(scanLineX);
    createOrUpdateGradient(scanLineX);
  }

  function setScanLineX(x) {
    scanLineX = x;
    active = true;
    createScanLine(scanLineX);
    createOrUpdateGradient(scanLineX);
  }

  function getScanLineX() {
    return scanLineX;
  }

  function isActive() {
    return active;
  }

  function dispose() {
    hide();
    scanLineMaterial.dispose();
  }

  return {
    update,
    restart,
    setScanLineX,
    getScanLineX,
    isActive,
    hide,
    dispose
  };
}