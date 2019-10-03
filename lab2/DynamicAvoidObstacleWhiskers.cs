using UnityEngine;
using Assets.Scripts.IAJ.Unity.Util;

namespace Assets.Scripts.IAJ.Unity.Movement.DynamicMovement
{

    public class DynamicAvoidObstacleWhiskers : DynamicSeek
    {
        public float AvoidMargin { get; set; }

        public float MaxLookAhead { get; set; }

        public Collider Collider { get; set; }

        public DynamicAvoidObstacleWhiskers(GameObject obstacle)
        {
            this.Collider = obstacle.GetComponent<Collider>();
        }

        public override MovementOutput GetMovement()
        {
            Vector3 rayVector = this.Character.velocity.normalized; //rayVector = character.velocity.normalized() * lookAhead;
            Vector3 rightWhisker = MathHelper.Rotate2D(this.Character.velocity.normalized, Mathf.PI / 6);
            Vector3 leftWhisker = MathHelper.Rotate2D(this.Character.velocity.normalized, Mathf.PI/ 6);
            RaycastHit CollisionInfo;
            RaycastHit RCollisionInfo;
            RaycastHit LCollisionInfo;

            bool collision = this.Collider.Raycast(new Ray(this.Character.Position, rayVector), out CollisionInfo, this.MaxLookAhead); //RayCastcollisionDetector.getCollision(character.position, rayVector);
            bool rcollision = this.Collider.Raycast(new Ray(this.Character.Position, rightWhisker), out RCollisionInfo, this.MaxLookAhead);
            bool lcollision = this.Collider.Raycast(new Ray(this.Character.Position, leftWhisker), out LCollisionInfo, this.MaxLookAhead);
            
            if (collision)
            {
                //Debug.Log(CollisionInfo.normal + " wow " + MathHelper.Rotate2D(this.Character.velocity.normalized, Mathf.PI));
                //frontal collision will go to the right
                if (CollisionInfo.normal == MathHelper.Rotate2D(this.Character.velocity.normalized, Mathf.PI))
                {
                   Debug.Log("exactly the same");
                    base.Target.Position = CollisionInfo.point + CollisionInfo.normal * this.AvoidMargin + Vector3.right;
                }
                else
                {
                    base.Target.Position = CollisionInfo.point + CollisionInfo.normal * this.AvoidMargin;
                }
                return base.GetMovement();
            }
            if (rcollision)
            {
                base.Target.Position = RCollisionInfo.point + RCollisionInfo.normal * this.AvoidMargin;
                //Debug.Log("rcollisnion");
                return base.GetMovement();
            }
            if (lcollision)
            {
                base.Target.Position = LCollisionInfo.point + LCollisionInfo.normal * this.AvoidMargin;
                //Debug.Log("lcollisnion");
                return base.GetMovement();
            }

            return new MovementOutput(); //empty movement output;
        }


    }
}