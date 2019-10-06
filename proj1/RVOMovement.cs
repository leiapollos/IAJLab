//class adapted from the HRVO library http://gamma.cs.unc.edu/HRVO/
//adapted to IAJ classes by João Dias

using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.Scripts.IAJ.Unity.Movement.DynamicMovement;
using Assets.Scripts.IAJ.Unity.Util;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.Movement.VO
{
    public class RVOMovement : DynamicMovement.DynamicVelocityMatch
    {
        public override string Name
        {
            get { return "RVO"; }
        }

        private const float TTC_EPSILON = 0.001f;
        protected List<KinematicData> Characters { get; set; }
        protected List<StaticData> Obstacles { get; set; }
        public int NumSamples { get; set; }
        public float CharacterSize { get; set; }
        public float ObstacleSize { get; set; }
        public float IgnoreDistance { get; set; }
        public float MaxSpeed { get; set; }
        public float CharWeight { get; set; }
        public float ObsWeight { get; set; }

        protected DynamicMovement.DynamicMovement DesiredMovement { get; set; }

        public RVOMovement(DynamicMovement.DynamicMovement goalMovement, List<KinematicData> movingCharacters, List<StaticData> obstacles)
        {
            this.DesiredMovement = goalMovement;

            this.Characters = movingCharacters;
            this.Characters.Remove(this.Character);
            this.Obstacles = obstacles;

            base.Target = new KinematicData();

            this.CharWeight = 5f;
            this.ObsWeight = 10f;
            this.NumSamples = 10;
            this.CharacterSize = 1.5f;
            this.ObstacleSize = 3f;
            this.IgnoreDistance = 10f;
        }

        public override MovementOutput GetMovement()
        {
            MovementOutput desiredOutput = this.DesiredMovement.GetMovement();
            Vector3 desiredVelocity = this.Character.velocity + desiredOutput.linear;

            if (desiredVelocity.magnitude > this.MaxSpeed)
            {
                desiredVelocity = desiredVelocity.normalized;
                desiredVelocity *= this.MaxSpeed;
            }

            List<Vector3> samples = new List<Vector3> { desiredVelocity };

            for (int i = 0; i < this.NumSamples; i++)
            {
                float angle = Random.Range(0, MathConstants.MATH_2PI);
                float magnitude = Random.Range(0, this.MaxSpeed);
                Vector3 velocitySample = MathHelper.ConvertOrientationToVector(angle) * magnitude;
                samples.Add(velocitySample);
            }

            base.Target.velocity = GetBestSample(desiredVelocity, samples);

            return base.GetMovement();
        }

        public Vector3 GetBestSample(Vector3 desiredVelocity, List<Vector3> samples)
        {
            Vector3 bestSample = Vector3.zero;
            float minimumPenalty = float.PositiveInfinity;

            foreach (Vector3 sample in samples)
            {
                float distancePenalty = (desiredVelocity - sample).magnitude;

                if (distancePenalty > minimumPenalty)
                    continue;

                float maximumPenalty = 0;

                foreach (KinematicData ch in this.Characters)
                {
                    Vector3 deltaP = ch.Position - this.Character.Position;

                    if (deltaP.sqrMagnitude > this.IgnoreDistance * this.IgnoreDistance)
                        continue;

                    Vector3 rayVector = 2 * sample - this.Character.velocity - ch.velocity;
                    float timeToCollision = MathHelper.TimeToCollisionBetweenRayAndCircle(this.Character.Position, rayVector, ch.Position, this.CharacterSize * 2);

                    float timePenalty;
                    if (timeToCollision > TTC_EPSILON)
                        timePenalty = this.CharWeight / timeToCollision;
                    else if (timeToCollision >= 0f)
                        timePenalty = float.PositiveInfinity;
                    else
                        timePenalty = 0;

                    if (timePenalty > maximumPenalty)
                        maximumPenalty = timePenalty;
                }

                foreach (StaticData obs in this.Obstacles)
                {
                    Vector3 deltaP = obs.Position - this.Character.Position;

                    if (deltaP.sqrMagnitude > this.IgnoreDistance * this.IgnoreDistance)
                        continue;

                    Vector3 rayVector = 2 * sample - this.Character.velocity;
                    float timeToCollision = MathHelper.TimeToCollisionBetweenRayAndCircle(this.Character.Position, rayVector, obs.Position, this.ObstacleSize);

                    float timePenalty;
                    if (timeToCollision > TTC_EPSILON)
                        timePenalty = this.ObsWeight / timeToCollision;
                    else if (timeToCollision >= 0f)
                        timePenalty = float.PositiveInfinity;
                    else
                        timePenalty = 0;

                    if (timePenalty > maximumPenalty)
                        maximumPenalty = timePenalty;
                }

                float penalty = distancePenalty + maximumPenalty;

                if (penalty < minimumPenalty)
                {
                    minimumPenalty = penalty;
                    bestSample = sample;

                    if (penalty < TTC_EPSILON)
                        break;
                }
            }

            return bestSample;
        }

    }
}
