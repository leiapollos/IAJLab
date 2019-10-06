using UnityEngine;
using Assets.Scripts.IAJ.Unity.Util;

namespace Assets.Scripts.IAJ.Unity.Movement.DynamicMovement
{
    public class DynamicAvoidObstacle : DynamicSeek
    {

        public float MaxLookAhead { get; set; }

        public float WhiskerLookAhead { get; set; }

        public float AvoidMargin { get; set; }

        public Collider Collider { get; set; }

        public DynamicAvoidObstacle(GameObject obstacle) {
            this.Collider = obstacle.GetComponent<Collider>();
        }

        public override MovementOutput GetMovement()
        {
            Vector3 rayVector    = this.Character.velocity.normalized;
            Vector3 leftWhisker  = MathHelper.Rotate2D(rayVector, MathConstants.MATH_PI / 6);
            Vector3 rightWhisker = MathHelper.Rotate2D(rayVector, -MathConstants.MATH_PI / 6);
            Vector3[] rayCasts = { rayVector, leftWhisker, rightWhisker };

            // Debug.DrawRay(this.Character.Position, rayVector * this.MaxLookAhead, Color.red);
            // Debug.DrawRay(this.Character.Position, leftWhisker * this.WhiskerLookAhead, Color.red);
            // Debug.DrawRay(this.Character.Position, rightWhisker * this.WhiskerLookAhead, Color.red);

            for (int i = 0; i < rayCasts.Length; i++)
            {
                RaycastHit info;
                bool collision;

                if (i == 0)
                    collision = this.Collider.Raycast(new Ray(this.Character.Position, rayCasts[i]), out info, this.MaxLookAhead);
                else
                    collision = this.Collider.Raycast(new Ray(this.Character.Position, rayCasts[i]), out info, this.WhiskerLookAhead);

                if (collision)
                {
                    base.Target.Position = info.point + info.normal * this.AvoidMargin;
                    return base.GetMovement();
                }
            }

            return new MovementOutput();
        }
    }
}