# WUP 3.8 Example Files — NR with Rotation (rx, ry, rz)

These example files demonstrate the new optional NR rotation parameters introduced in interface version 3.8.

> **Note:** The rotation parameters `rx`, `ry`, `rz` may only be used after **consultation with WEINMANN**, as they depend on the specific machine equipment and configuration.

---

## Files overview

| File | NR rotation used | Use case |
|:---|:---|:---|
| `counterlatten_tilted_rx.wup` | `rx = ±15°` | Counter-battens (Konterlatten) on exterior wall. Alternating tilt for improved nail pull-out resistance. |
| `roof_battens_ry.wup` | `ry = 30°` | Roof battens (Dachlatten) on a 30° pitched roof. Nails tilted to enter the rafter perpendicular to its sloped surface. |
| `diagonal_bracing_rz.wup` | `rz = ±45°` | Diagonal wind bracing slats. Nails rotated in the panel plane to align with the slat's grain direction. |
| `full_wall_mixed_rotation.wup` | `rx`, `ry`, `rz` | Complete prefab wall element with window opening. Combines all three rotation types across three batten layers. |

---

## NR parameter syntax (version 3.8)

```
NR xa, ya, xe, ye, a, i [, rx [, ry [, rz]]];
```

| Parameter | Meaning | Default |
|:---|:---|:---|
| `xa, ya` | Start point of nail line | — |
| `xe, ye` | End point of nail line | — |
| `a` | Nail spacing in mm | — |
| `i` | Control code | — |
| `rx` | Rotation around X axis (tilt toward Y) | 0 |
| `ry` | Rotation around Y axis (tilt toward X) | 0 |
| `rz` | Rotation in panel plane (around Z) | 0 |

For a nail **point** (not a line): `xa = xe`, `ya = ye`, `a = 1` (spacing is ignored).

---

## Construction scenarios explained

### `counterlatten_tilted_rx.wup`
```
Wall cross-section (outside):
  [OSB 15mm] [Counter-batten 40x28mm]
                   |  / nail rx=+15°
                   | /
                   |\ nail rx=-15° (next position)
                   | \
```
Alternating the tilt direction creates a mechanical interlock that significantly increases the pull-out load compared to straight nailing.

### `roof_battens_ry.wup`
```
Roof cross-section (side view):
  Rafter (sloped 30°)
    \
     \  Sarking board
      \___________
       [batten]
          |
          | nail ry=30° → nail goes perpendicular to rafter face
```
Without ry rotation, nails driven straight down would enter the rafter at a 30° angle, reducing effective embedment depth. With ry=30°, the nail enters perpendicular to the rafter surface.

### `diagonal_bracing_rz.wup`
```
Panel view (from front):
  +------------------+
  |\                /|
  | \  rz=+45°    / |
  |  \          /   |
  |   X (nail) /    |
  |    \      /     |
  |     \    /      |
  +------\--/-------+
          \/
```
The nail is rotated 45° in the panel plane, aligning it with the slat axis for maximum shear resistance along the bracing direction.

### `full_wall_mixed_rotation.wup`
Layer build-up (inside → outside):
1. PLI1: Gypsum board 12.5mm (standard NR, no rotation)
2. *Frame: 80×160mm studs*
3. PLA1: OSB sheathing 15mm (standard NR, no rotation)
4. PLA2: Horizontal ventilation battens 40×28mm — `ry=10°`
5. PLA3: Vertical counter-battens 40×28mm — `rx=±15°` alternating
