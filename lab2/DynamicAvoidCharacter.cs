using UnityEngine;
using Assets.Scripts.IAJ.Unity.Util;

namespace Assets.Scripts.IAJ.Unity.Movement.DynamicMovement
{

    public class DynamicAvoidCharacter : DynamicSeek
    {
        public float AvoidMargin { get; set; }

        public KinematicData Character { get; set; }

        public float MaxAcceleration { get; set; }

        public KinematicData OtherCharacter {get;set;}

        public float MaxTimeLookAhead { get; set; }

        public DynamicAvoidCharacter(KinematicData otherCharacter)
        {
            this.OtherCharacter = otherCharacter;
            this.MaxTimeLookAhead = 0.005f;
        }

        public override MovementOutput GetMovement()
        {
            MovementOutput output = new MovementOutput();

            Vector3 deltaPos = OtherCharacter.Position - Character.Position;
            Vector3 deltaVel = OtherCharacter.velocity - Character.velocity;
            float deltaSqrSpeed = deltaVel.sqrMagnitude;

            if (deltaSqrSpeed == 0)
                return new MovementOutput(); //Empty movement output

            float timeToClosest = -Vector3.Dot(deltaPos, deltaVel) / deltaSqrSpeed;

            if (timeToClosest > MaxTimeLookAhead)
                return new MovementOutput(); //Empty movement output

            Vector3 futureDeltaPos = deltaPos + deltaVel * timeToClosest;
            float futureDistance = futureDeltaPos.magnitude;

            if(futureDistance > AvoidMargin/5)
                return new MovementOutput(); //Empty movement output

            if (futureDistance <= 0 || deltaPos.magnitude < AvoidMargin/5)
                output.linear = Character.Position - OtherCharacter.Position;
            else
                output.linear = futureDeltaPos * -1;

            output.linear = output.linear.normalized * MaxAcceleration;
            return output;
        }
    }
}
