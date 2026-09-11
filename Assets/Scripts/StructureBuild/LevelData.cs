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
            Level("archive-001", "首次校准", "先放两块地面方块，让正面与侧面同时亮起", 2, new[] {1,1,0,0}, new[] {1,1,0,0},
                (0,0,0),(1,0,1)),
            Level("archive-002", "初层阶梯", "第一次使用垂直堆叠", 4, new[] {1,2,1,0}, new[] {2,1,1,0},
                (0,0,1),(1,0,0),(1,1,0),(2,0,2)),
            Level("archive-003", "双峰折线", "让两座高点在侧面错位对应", 5, new[] {2,1,2,0}, new[] {1,2,2,0},
                (0,0,1),(0,1,1),(1,0,0),(2,0,2),(2,1,2)),
            Level("archive-004", "阶梯信标", "匹配四列不同高度", 7, new[] {1,2,3,1}, new[] {2,3,1,1},
                (0,0,2),(1,0,0),(1,1,0),(2,0,1),(2,1,1),(2,2,1),(3,0,3)),
            Level("archive-005", "三峰棱镜", "辨认三层塔与双层塔的对应", 8, new[] {3,2,2,1}, new[] {2,3,2,1},
                (0,0,1),(0,1,1),(0,2,1),(1,0,0),(1,1,0),(2,0,2),(2,1,2),(3,0,3)),
            Level("archive-006", "双塔回声", "两座三层塔需要正确换位", 9, new[] {3,3,2,1}, new[] {2,3,3,1},
                (0,0,1),(0,1,1),(0,2,1),(1,0,2),(1,1,2),(1,2,2),(2,0,0),(2,1,0),(3,0,3)),
            Level("archive-007", "终端光门", "完成双塔组合，进入深层档案", 10, new[] {3,3,2,2}, new[] {2,3,3,2},
                (0,0,1),(0,1,1),(0,2,1),(1,0,2),(1,1,2),(1,2,2),(2,0,0),(2,1,0),(3,0,3),(3,1,3)),
            Level("archive-008", "折光回廊", "让三座双层塔在侧影中共用深度", 11, new[] {2,3,2,2}, new[] {1,2,3,1},
                (0,0,1),(0,1,1),(1,0,2),(1,1,2),(1,2,2),(2,0,2),(2,1,2),(3,0,2),(3,1,2),(1,0,0),(1,0,3)),
            Level("archive-009", "双核偏振", "同一正面高塔必须在深处留下第二核心", 13, new[] {2,3,2,2}, new[] {3,1,3,2},
                (0,0,3),(0,1,3),(0,0,1),(1,0,0),(1,1,0),(1,2,0),(1,0,2),(1,1,2),(1,2,2),(2,0,0),(2,1,0),(3,0,2),(3,1,2)),
            Level("archive-010", "暗面列阵", "把三座高塔藏进同一条侧面光柱", 14, new[] {3,2,3,3}, new[] {1,3,1,1},
                (0,0,0),(0,0,1),(0,1,1),(0,2,1),(1,0,1),(1,1,1),(1,0,2),(2,0,1),(2,1,1),(2,2,1),(2,0,3),(3,0,1),(3,1,1),(3,2,1)),
            Level("archive-011", "四壁共振", "用一座主塔同时遮住三面深层回声", 16, new[] {3,1,2,1}, new[] {3,3,3,3},
                (0,0,0),(0,1,0),(0,2,0),(0,0,1),(0,1,1),(0,2,1),(0,0,2),(0,1,2),(0,2,2),(0,0,3),(0,1,3),(0,2,3),(1,0,1),(2,0,2),(2,1,2),(3,0,3)),
            Level("archive-012", "终极光栅", "让四座满高光塔在侧面压缩成一条主脊", 18, new[] {3,3,3,3}, new[] {2,3,2,2},
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
