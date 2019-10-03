using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.Movement.DynamicMovement
{
    public class DynamicAvoidObstacle : DynamicSeek
    {

        public float MaxLookAhead { get; set; }

        public float AvoidMargin { get; set; }

        public Collider Collider { get; set; }

        public DynamicAvoidObstacle(GameObject obstacle) {
            this.Collider = obstacle.GetComponent<Collider>();
        }

        public override MovementOutput GetMovement()
        {
            Vector3 rayVector = this.Character.velocity.normalized;
            RaycastHit info;

            bool collision = this.Collider.Raycast(new Ray(this.Character.Position, rayVector), out info, this.MaxLookAhead);

            if (!collision)
                return new MovementOutput();

            base.Target.Position = info.point + info.normal * this.AvoidMargin;

            return base.GetMovement();
        }
    }
}
