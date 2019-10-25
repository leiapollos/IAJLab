using Assets.Scripts.IAJ.Unity.Utils;
using UnityEngine;
using System;

namespace Assets.Scripts.IAJ.Unity.Pathfinding.Path
{
    public class LineSegmentPath : LocalPath
    {
        protected Vector3 LineVector;
        public LineSegmentPath(Vector3 start, Vector3 end)
        {
            this.StartPosition = start;
            this.EndPosition = end;
            this.LineVector = end - start;
        }

        public override Vector3 GetPosition(float param)
        {
            float done = param - (float)Math.Truncate(param);

            Vector3 pos;
            pos.x = this.StartPosition.x + this.LineVector.x * done;
            pos.y = this.StartPosition.y + this.LineVector.y * done;
            pos.z = this.StartPosition.z + this.LineVector.z * done;

            return pos;
        }

        public override bool PathEnd(float param)
        {
            float done = param - (float)Math.Truncate(param);
            return done > 0.95;
        }

        public override float GetParam(Vector3 position, float lastParam)
        {
            return lastParam + MathHelper.closestParamInLineSegmentToPoint(this.StartPosition, this.EndPosition, position);
        }
    }
}
