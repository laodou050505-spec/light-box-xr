using System.Collections.Generic;
using UnityEngine;

namespace StructureBuild
{
    public struct ProjectionScore
    {
        public int matched;
        public int targetCount;
        public int extras;
        public int missing;
        public bool Complete => extras == 0 && missing == 0;
    }

    public sealed class EvaluationResult
    {
        public ProjectionScore front;
        public ProjectionScore side;
        public bool withinBudget;
        public bool complete;
        public bool[,] frontCurrent;
        public bool[,] sideCurrent;
    }

    public static class ProjectionRules
    {
        public static EvaluationResult Evaluate(ICollection<Vector3Int> cells, LevelDefinition level)
        {
            var front = new bool[level.height, level.width];
            var side = new bool[level.height, level.depth];
            foreach (var cell in cells)
            {
                if (cell.x < 0 || cell.x >= level.width || cell.y < 0 || cell.y >= level.height || cell.z < 0 || cell.z >= level.depth) continue;
                front[cell.y, cell.x] = true;
                side[cell.y, cell.z] = true;
            }
            var targetFront = new bool[level.height, level.width];
            var targetSide = new bool[level.height, level.depth];
            for (var y = 0; y < level.height; y++)
            {
                for (var x = 0; x < level.width; x++) targetFront[y, x] = y < level.frontHeights[x];
                for (var z = 0; z < level.depth; z++) targetSide[y, z] = y < level.sideHeights[z];
            }
            var frontScore = Compare(front, targetFront);
            var sideScore = Compare(side, targetSide);
            var withinBudget = cells.Count <= level.cubeLimit;
            return new EvaluationResult { front = frontScore, side = sideScore, withinBudget = withinBudget, complete = withinBudget && frontScore.Complete && sideScore.Complete, frontCurrent = front, sideCurrent = side };
        }

        private static ProjectionScore Compare(bool[,] current, bool[,] target)
        {
            var score = new ProjectionScore();
            var height = target.GetLength(0);
            var width = target.GetLength(1);
            for (var y = 0; y < height; y++) for (var x = 0; x < width; x++)
            {
                var wants = target[y, x]; var has = current[y, x];
                if (wants) score.targetCount++;
                if (wants && has) score.matched++;
                if (wants && !has) score.missing++;
                if (!wants && has) score.extras++;
            }
            return score;
        }

        public static bool IsSupported(Vector3Int cell, HashSet<Vector3Int> occupied) => cell.y == 0 || occupied.Contains(new Vector3Int(cell.x, cell.y - 1, cell.z));
        public static int ColumnTop(HashSet<Vector3Int> occupied, int x, int z, int maxHeight)
        {
            for (var y = 0; y < maxHeight; y++) if (!occupied.Contains(new Vector3Int(x, y, z))) return y;
            return -1;
        }
    }
}
