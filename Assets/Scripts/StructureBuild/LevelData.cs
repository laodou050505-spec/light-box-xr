using System;
using System.Collections.Generic;
using UnityEngine;

namespace StructureBuild
{
    [Serializable]
    public class GridCellData
    {
        public int x;
        public int y;
        public int z;

        public GridCellData() { }
        public GridCellData(int x, int y, int z) { this.x = x; this.y = y; this.z = z; }
        public Vector3Int ToVector3Int() => new Vector3Int(x, y, z);
    }

    [Serializable]
    public class LevelDefinition
    {
        public string id;
        public string displayName;
        public string rule;
        public int width = 4;
        public int height = 3;
        public int depth = 4;
        public int cubeLimit;
        public int[] frontHeights;
        public int[] sideHeights;
        public List<GridCellData> referenceSolution = new List<GridCellData>();

        public Vector3Int Size => new Vector3Int(width, height, depth);
    }

    public static class CampaignData
    {
        public static readonly LevelDefinition[] Levels =
        {
            // Teaching level: two ground cubes expose the same two columns
            // from the front and side, so the player can immediately see
            // that both projection boards must turn green together.
            Level("archive-001", "First Calibration", "Place two ground blocks to illuminate both projections at once", 2, new[] {1,1,0,0}, new[] {1,1,0,0},
                (0,0,0),(1,0,1)),
            Level("archive-002", "First Ascent", "Use vertical stacking for the first time", 4, new[] {1,2,1,0}, new[] {2,1,1,0},
                (0,0,1),(1,0,0),(1,1,0),(2,0,2)),
            Level("archive-003", "Twin Peaks", "Offset two high points so they align in the side view", 5, new[] {2,1,2,0}, new[] {1,2,2,0},
                (0,0,1),(0,1,1),(1,0,0),(2,0,2),(2,1,2)),
            Level("archive-004", "Staircase Beacon", "Match four columns of different heights", 7, new[] {1,2,3,1}, new[] {2,3,1,1},
                (0,0,2),(1,0,0),(1,1,0),(2,0,1),(2,1,1),(2,2,1),(3,0,3)),
            Level("archive-005", "Three-Peak Prism", "Map the three-tier tower against the two-tier tower", 8, new[] {3,2,2,1}, new[] {2,3,2,1},
                (0,0,1),(0,1,1),(0,2,1),(1,0,0),(1,1,0),(2,0,2),(2,1,2),(3,0,3)),
            Level("archive-006", "Twin Tower Echo", "Two three-tier towers must swap positions correctly", 9, new[] {3,3,2,1}, new[] {2,3,3,1},
                (0,0,1),(0,1,1),(0,2,1),(1,0,2),(1,1,2),(1,2,2),(2,0,0),(2,1,0),(3,0,3)),
            Level("archive-007", "Terminal Gate", "Complete the twin-tower pair to unlock the deep archive", 10, new[] {3,3,2,2}, new[] {2,3,3,2},
                (0,0,1),(0,1,1),(0,2,1),(1,0,2),(1,1,2),(1,2,2),(2,0,0),(2,1,0),(3,0,3),(3,1,3)),
            Level("archive-008", "Refraction Corridor", "Three two-tier towers must share depth in the side silhouette", 11, new[] {2,3,2,2}, new[] {1,2,3,1},
                (0,0,1),(0,1,1),(1,0,2),(1,1,2),(1,2,2),(2,0,2),(2,1,2),(3,0,2),(3,1,2),(1,0,0),(1,0,3)),
            Level("archive-009", "Dual-Core Polarization", "One tall tower in the front view must hide a second core further back", 13, new[] {2,3,2,2}, new[] {3,1,3,2},
                (0,0,3),(0,1,3),(0,0,1),(1,0,0),(1,1,0),(1,2,0),(1,0,2),(1,1,2),(1,2,2),(2,0,0),(2,1,0),(3,0,2),(3,1,2)),
            Level("archive-010", "Dark Side Array", "Hide three tall towers within a single side-projection column", 14, new[] {3,2,3,3}, new[] {1,3,1,1},
                (0,0,0),(0,0,1),(0,1,1),(0,2,1),(1,0,1),(1,1,1),(1,0,2),(2,0,1),(2,1,1),(2,2,1),(2,0,3),(3,0,1),(3,1,1),(3,2,1)),
            Level("archive-011", "Four-Wall Resonance", "Use one main tower to mask three deep echoes at once", 16, new[] {3,1,2,1}, new[] {3,3,3,3},
                (0,0,0),(0,1,0),(0,2,0),(0,0,1),(0,1,1),(0,2,1),(0,0,2),(0,1,2),(0,2,2),(0,0,3),(0,1,3),(0,2,3),(1,0,1),(2,0,2),(2,1,2),(3,0,3)),
            Level("archive-012", "Final Grating", "Compress four full-height towers into a single ridge in the side view", 18, new[] {3,3,3,3}, new[] {2,3,2,2},
                (0,0,0),(0,1,0),(0,0,1),(0,1,1),(0,2,1),(1,0,1),(1,1,1),(1,2,1),(1,0,2),(1,1,2),(2,0,1),(2,1,1),(2,2,1),(2,0,3),(2,1,3),(3,0,1),(3,1,1),(3,2,1)),
        };

        private static LevelDefinition Level(string id, string name, string rule, int limit, int[] front, int[] side, params (int x,int y,int z)[] cells)
        {
            var level = new LevelDefinition
            {
                id = id, displayName = name, rule = rule, cubeLimit = limit,
                frontHeights = front, sideHeights = side,
            };
            foreach (var cell in cells) level.referenceSolution.Add(new GridCellData(cell.x, cell.y, cell.z));
            return level;
        }
    }
}
