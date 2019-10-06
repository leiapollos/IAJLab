using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*
namespace Assets.Scripts.IAJ.Unity.Movement.DynamicMovement
{
    public class DynamicAvoidCharacter : DynamicMovement
    {

        public override string Name
        {
            get { return "Dynamic Avoid"; }
        }

        public float CollisionRadius { get; set; }

        public float MaxTimeLookAhead { get; set; }

        public DynamicAvoidCharacter(KinematicData otherCharacter)
        {
            base.Target = otherCharacter;
            this.Output = new MovementOutput();
        }

        public override MovementOutput GetMovement()
        {
            Vector3 deltaPos = base.Target.Position - base.Character.Position;
            Vector3 deltaVel = base.Target.velocity - base.Character.velocity;
            float deltaSqrSpeed = deltaVel.sqrMagnitude;

            if (deltaSqrSpeed == 0)
                return new MovementOutput();

            float timeToClosest = -Vector3.Dot(deltaPos, deltaVel) / deltaSqrSpeed;

            if (timeToClosest > this.MaxTimeLookAhead)
                return new MovementOutput();

            Vector3 futureDeltaPos = deltaPos + deltaVel * timeToClosest;
            float futureDistance = futureDeltaPos.magnitude;

            if (futureDistance > 2 * this.CollisionRadius)
                return new MovementOutput();

            if (futureDistance <= 0 || deltaPos.magnitude < 2 * this.CollisionRadius)
                base.Output.linear = base.Character.Position - base.Target.Position;
            else
                base.Output.linear = -futureDeltaPos;

            base.Output.linear = this.Output.linear.normalized * this.MaxAcceleration;
            return this.Output;
        }

    }
}
*/

namespace Assets.Scripts.IAJ.Unity.Movement.DynamicMovement
{
    public class DynamicAvoidCharacter : DynamicMovement
    {

        public override string Name
        {
            get { return "Dynamic Avoid"; }
        }

        public List<KinematicData> Targets { get; set; }
        public float CollisionRadius { get; set; }

        public float MaxTimeLookAhead { get; set; }

        public DynamicAvoidCharacter(List<KinematicData> otherCharacters)
        {
            this.Targets = otherCharacters;
            this.Output = new MovementOutput();
        }

        public override MovementOutput GetMovement()
        {
            float shortestTime = float.PositiveInfinity;
            KinematicData closestTarget = new KinematicData();
            float closestFutureDistance = 0f;
            Vector3 closestFutureDeltaPos = Vector3.zero;
            Vector3 closestDeltaPos = Vector3.zero;
            Vector3 closestDeltaVel = Vector3.zero;

            foreach (KinematicData ch in this.Targets)
            {
                Vector3 deltaPos = ch.Position - this.Character.Position;
                Vector3 deltaVel = ch.velocity - this.Character.velocity;
                float deltaSqrSpeed = deltaVel.sqrMagnitude;

                if (deltaSqrSpeed == 0) continue;

                float timeToClosest = -Vector3.Dot(deltaPos, deltaVel) / deltaSqrSpeed;

                if (timeToClosest > this.MaxTimeLookAhead) continue;

                Vector3 futureDeltaPos = deltaPos + deltaVel * timeToClosest;
                float futureDistance = futureDeltaPos.magnitude;

                if (futureDistance > 2 * this.CollisionRadius) continue;

                if (timeToClosest > 0 && timeToClosest < shortestTime)
                {
                    shortestTime = timeToClosest;
                    closestTarget = ch;
                    closestFutureDistance = futureDistance;
                    closestFutureDeltaPos = futureDeltaPos;
                    closestDeltaPos = deltaPos;
                    closestDeltaVel = deltaVel;
                }
            }

            if (shortestTime.Equals(float.PositiveInfinity))
                return new MovementOutput();

            Vector3 avoidanceDirection;
            if (closestFutureDistance <= 0 || closestDeltaPos.magnitude < 2 * this.CollisionRadius)
                avoidanceDirection = this.Character.Position - closestTarget.Position;
            else
                avoidanceDirection = -closestFutureDeltaPos;

            base.Output = new MovementOutput();
            base.Output.linear = avoidanceDirection.normalized * base.MaxAcceleration;
            return base.Output;
        }

    }
}
