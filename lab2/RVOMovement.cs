//class adapted from the HRVO library http://gamma.cs.unc.edu/HRVO/
//adapted to IAJ classes by João Dias
using System;
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

        protected List<KinematicData> Characters { get; set; }
        protected List<StaticData> Obstacles { get; set; }
        public float CharacterSize { get; set; }
        public float ObjectSize { get; set; }
        public float IgnoreDistance { get; set; }
        public float MaxSpeed { get; set; }
        public float Weight { get; set; }

        protected DynamicMovement.DynamicMovement DesiredMovement { get; set; }

        public RVOMovement(DynamicMovement.DynamicMovement goalMovement, List<KinematicData> movingCharacters, List<StaticData> obstacles)
        {
            this.DesiredMovement = goalMovement;
            this.Characters = movingCharacters;
            this.Obstacles = obstacles;
            base.Target = new KinematicData();
            this.IgnoreDistance = 13.0f;
            this.Weight = 55.0f;
            this.CharacterSize = 1.5f;
            this.ObjectSize = 3.0f;
        }

        public override MovementOutput GetMovement()
        {
            MovementOutput desiredMovementOutput = DesiredMovement.GetMovement();

            Vector3 desiredVelocity = this.Character.velocity + desiredMovementOutput.linear;

            if (desiredVelocity.magnitude > MaxSpeed) {
                desiredVelocity = desiredVelocity.normalized;
                desiredVelocity *= MaxSpeed;
            }

            List<Vector3> samples = new List<Vector3>();

            samples.Add(desiredVelocity);
            for(int i = 0; i< 300; i++)
            {
                float angle = UnityEngine.Random.Range(0, (float)(2 * Math.PI));
                float magnitude = UnityEngine.Random.Range(0, MaxSpeed);
                Vector3 velocitySample = MathHelper.ConvertOrientationToVector(angle) * magnitude;
                samples.Add(velocitySample);
            }
            base.Target.velocity = getBestSample(desiredVelocity, samples);
            return base.GetMovement();
        }

        public Vector3 getBestSample(Vector3 desiredVelocity, List<Vector3> samples)
        {
            Vector3 bestSample = new Vector3(0, 0, 0);
            float minimumPenalty = float.PositiveInfinity;

            foreach(Vector3 sample in samples)
            {
                float distancePenalty = (desiredVelocity - sample).magnitude;
                float maximumTimePenalty = 0.0f;
                float timePenalty = 0.0f;
                foreach (StaticData b in this.Obstacles)
                {
                    Vector3 deltaP = b.Position - this.Character.Position;
                    if (deltaP.magnitude > IgnoreDistance)
                        continue;
                    Vector3 rayVector = 2 * sample - this.Character.velocity;
                    float tc = MathHelper.TimeToCollisionBetweenRayAndCircle(this.Character.Position, rayVector, b.Position, this.ObjectSize);
                    timePenalty = 0.0f;
                    if (tc > 0) //future collision
                        timePenalty = Weight / tc;
                    else if (tc == 0) //immediate collision
                        timePenalty = float.PositiveInfinity;
                    else //no collision
                        timePenalty = 0;

                    if (timePenalty > maximumTimePenalty) //opportunity for optimization here (teacher)
                        maximumTimePenalty = timePenalty;
                }

                foreach (KinematicData b in this.Characters)
                {
                    Vector3 deltaP = b.Position - this.Character.Position;
                    if (deltaP.magnitude > IgnoreDistance)
                        continue;

                    if ((this.Character.Position - b.Position).magnitude == 0) //Make sure we are not testing each charatcer with itself
                        continue;

                    Vector3 rayVector = 2 * sample - this.Character.velocity - b.velocity;
                    float tc = MathHelper.TimeToCollisionBetweenRayAndCircle(this.Character.Position, rayVector, b.Position, this.CharacterSize * 2);
                    timePenalty = 0.0f;
                    if (tc > 0) //future collision
                        timePenalty = Weight / tc;
                    else if (tc == 0) //immediate collision
                        timePenalty = float.PositiveInfinity;
                    else //no collision
                        timePenalty = 0.0f;

                    if (timePenalty > maximumTimePenalty) //opportunity for optimization here (teacher)
                        maximumTimePenalty = timePenalty;
                }

                float penalty = distancePenalty + maximumTimePenalty;

                if (penalty < minimumPenalty)
                { //opportunity for optimization here (teacher)
                    minimumPenalty = penalty;
                    bestSample = sample;
                    if (penalty == 0)
                        return bestSample;
                }
            }
            return bestSample;
        }
    }
}
