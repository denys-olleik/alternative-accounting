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
  console.log("[sectors] initSectors called", {
    gridSize,
    width,
    height,
    color,
    opacity,
    z,
    now: new Date().toISOString()
  });

  // Map: "SectorX:SectorY" -> { mesh, timeoutId }
  const sectorMeshes = new Map();

  function removeSector(key, reason = "unknown") {
    console.log("[sectors] removeSector called", {
      key,
      reason,
      exists: sectorMeshes.has(key),
      now: new Date().toISOString()
    });

    const entry = sectorMeshes.get(key);
    if (!entry) {
      console.warn("[sectors] removeSector: no entry found", { key, reason });
      return;
    }

    if (entry.timeoutId) {
      console.log("[sectors] removeSector: clearing timeout", {
        key,
        timeoutId: entry.timeoutId
      });
      clearTimeout(entry.timeoutId);
    }

    scene.remove(entry.mesh);
    entry.mesh.geometry.dispose();
    entry.mesh.material.dispose();
    sectorMeshes.delete(key);

    console.log("[sectors] removeSector: removed", {
      key,
      remainingCount: sectorMeshes.size,
      now: new Date().toISOString()
    });
  }

  function scheduleExpiration(key, occupyUntil) {
    console.log("[sectors] scheduleExpiration called", {
      key,
      occupyUntil,
      occupyUntilType: typeof occupyUntil,
      now: new Date().toISOString()
    });

    if (!occupyUntil) {
      console.warn("[sectors] scheduleExpiration: no occupyUntil", {
        key,
        occupyUntil
      });
      return null;
    }

    const expireAt = new Date(occupyUntil).getTime();

    console.log("[sectors] scheduleExpiration parsed date", {
      key,
      occupyUntil,
      expireAt,
      expireAtIso: Number.isNaN(expireAt) ? null : new Date(expireAt).toISOString(),
      nowMs: Date.now(),
      nowIso: new Date().toISOString()
    });

    if (Number.isNaN(expireAt)) {
      console.warn("[sectors] scheduleExpiration: invalid occupyUntil date", {
        key,
        occupyUntil
      });
      return null;
    }

    const msUntilExpire = expireAt - Date.now();

    console.log("[sectors] scheduleExpiration delay calculated", {
      key,
      msUntilExpire,
      secondsUntilExpire: msUntilExpire / 1000
    });

    if (msUntilExpire <= 0) {
      console.warn("[sectors] scheduleExpiration: already expired, scheduling immediate removal", {
        key,
        occupyUntil,
        msUntilExpire
      });

      setTimeout(() => {
        console.log("[sectors] immediate expiration timeout fired", {
          key,
          now: new Date().toISOString()
        });
        removeSector(key, "already-expired-timeout");
      }, 0);

      return null;
    }

    const timeoutId = setTimeout(() => {
      console.log("[sectors] expiration timeout fired", {
        key,
        occupyUntil,
        expectedExpireAt: new Date(expireAt).toISOString(),
        now: new Date().toISOString(),
        lateByMs: Date.now() - expireAt
      });

      removeSector(key, "expiration-timeout");
    }, msUntilExpire);

    console.log("[sectors] scheduleExpiration: timeout scheduled", {
      key,
      timeoutId,
      msUntilExpire,
      fireAtIso: new Date(expireAt).toISOString()
    });

    return timeoutId;
  }

  // Call this with the array of claims [{SectorX, SectorY, OccupyUntil, ...}]
  function updateSectors(claims) {
    console.log("[sectors] updateSectors called", {
      claims,
      isArray: Array.isArray(claims),
      count: Array.isArray(claims) ? claims.length : null,
      existingKeys: Array.from(sectorMeshes.keys()),
      now: new Date().toISOString()
    });

    if (!Array.isArray(claims)) {
      console.error("[sectors] updateSectors: claims is not an array", claims);
      return;
    }

    const keepKeys = new Set();

    for (const claim of claims) {
      console.log("[sectors] processing claim", claim);

      const sectorX = claim.sectorX ?? claim.SectorX;
      const sectorY = claim.sectorY ?? claim.SectorY;
      const occupyUntil = claim.occupyUntil ?? claim.OccupyUntil;

      console.log("[sectors] normalized claim", {
        original: claim,
        sectorX,
        sectorY,
        occupyUntil
      });

      if (sectorX == null || sectorY == null) {
        console.warn("[sectors] claim missing sector coordinates", {
          claim,
          sectorX,
          sectorY
        });
        continue;
      }

      const key = `${sectorX}:${sectorY}`;
      keepKeys.add(key);

      let entry = sectorMeshes.get(key);

      if (!entry) {
        console.log("[sectors] creating mesh for sector", {
          key,
          sectorX,
          sectorY
        });

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

        console.log("[sectors] mesh position set", {
          key,
          x,
          y,
          z
        });

        scene.add(mesh);

        entry = {
          mesh,
          timeoutId: null
        };

        sectorMeshes.set(key, entry);

        console.log("[sectors] mesh added", {
          key,
          totalCount: sectorMeshes.size
        });
      } else {
        console.log("[sectors] existing mesh found", {
          key,
          hasTimeout: !!entry.timeoutId,
          timeoutId: entry.timeoutId
        });
      }

      if (entry.timeoutId) {
        console.log("[sectors] clearing previous timeout before rescheduling", {
          key,
          timeoutId: entry.timeoutId
        });
        clearTimeout(entry.timeoutId);
        entry.timeoutId = null;
      }

      entry.timeoutId = scheduleExpiration(key, occupyUntil);

      console.log("[sectors] claim scheduling complete", {
        key,
        timeoutId: entry.timeoutId,
        hasTimeout: !!entry.timeoutId
      });
    }

    console.log("[sectors] removing stale sectors not in current claims", {
      keepKeys: Array.from(keepKeys),
      currentKeys: Array.from(sectorMeshes.keys())
    });

    // Remove any old meshes not in the current claims
    for (const [key] of sectorMeshes) {
      if (!keepKeys.has(key)) {
        console.log("[sectors] stale sector found", { key });
        removeSector(key, "not-in-current-claims");
      }
    }

    console.log("[sectors] updateSectors complete", {
      finalKeys: Array.from(sectorMeshes.keys()),
      finalCount: sectorMeshes.size,
      now: new Date().toISOString()
    });
  }

  return {
    updateSectors
  };
}