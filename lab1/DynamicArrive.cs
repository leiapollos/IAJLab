using UnityEngine;
using System.Collections;

namespace Assets.Scripts.IAJ.Unity.Movement.DynamicMovement
{
    public class DynamicArrive : DynamicVelocityMatch
    {

        public float MaxSpeed   { get; set; }
        public float StopRadius { get; set; }
        public float SlowRadius { get; set; }

        public KinematicData DesiredTarget { get; set; } 

        public DynamicArrive()
        {
            this.MaxSpeed   = 40.0f;
            this.StopRadius = 10.0f;
            this.SlowRadius = 40.0f;
            this.Output     = new MovementOutput();
        }

        public override MovementOutput GetMovement()
        {
            base.Target = new KinematicData(this.DesiredTarget, this.DesiredTarget.velocity);

            Vector3 direction = this.DesiredTarget.Position - this.Character.Position;
            float distance    = direction.magnitude;
            float desiredSpeed;

            if (distance < this.StopRadius) desiredSpeed = 0;
            else if (distance > this.SlowRadius) desiredSpeed = this.MaxSpeed;
            else desiredSpeed = this.MaxSpeed * (distance / this.SlowRadius);

            base.Target.velocity = direction.normalized * desiredSpeed;

            return base.GetMovement();
        }
    }
}