using System;
using UnityEngine;
using Assets.Scripts.IAJ.Unity.Pathfinding.Path;

namespace Assets.Scripts.IAJ.Unity.Movement.DynamicMovement
{
    public class DynamicFollowPath : DynamicArrive
    {
        public override string Name
        {
            get
            {
                return "Follow Path";
            }
        }

        public GlobalPath Path { get; set; }

        public float Param { get; set; }

        public DynamicFollowPath(KinematicData character, GlobalPath path)
        {
            this.Character = character;
            this.Path = path;
            this.Param = 0.0f;
            this.MaxAcceleration = 15.0f;
        }

        public override MovementOutput GetMovement()
        {
            if (this.Path.PathEnd(this.Param))
            {
                this.Character.velocity = Vector3.zero;
                return new MovementOutput();
            }

            this.Param = this.Path.GetParam(this.Character.Position, this.Param);
            this.DestinationTarget = new KinematicData();
            this.DestinationTarget.Position = this.Path.GetPosition(this.Param);

            return base.GetMovement();
        }
    }
}