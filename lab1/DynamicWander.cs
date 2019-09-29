using Assets.Scripts.IAJ.Unity.Util;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.Movement.DynamicMovement
{

    public class DynamicWander : DynamicSeek
    {
        public float WanderOffset { get; set; }
        public float WanderRadius { get; set; }
        public float WanderRate { get; set; }

        public float WanderAngle { get; protected set; }

        public Vector3 CircleCenter { get; private set; }

        public GameObject DebugTarget { get; set; }

        public DynamicWander()
        {
            this.Target = new KinematicData();
            this.WanderAngle = 0;
            this.WanderRadius = 5.0f;
        }

        public override string Name
        {
            get { return "Wander"; }
        }


        public override MovementOutput GetMovement()
        {
            this.WanderAngle += RandomHelper.RandomBinomial() * this.WanderRate;

            float TargetOrientation = this.WanderAngle + this.Character.Orientation;

            CircleCenter = this.Character.Position + WanderOffset * Character.GetOrientationAsVector();

            base.Target.Position = CircleCenter + WanderRadius * MathHelper.ConvertOrientationToVector(TargetOrientation);

            if (this.DebugTarget != null)
            {
                this.DebugTarget.transform.position = this.Target.Position;
                
            }
            return base.GetMovement();
        }
    }
}
