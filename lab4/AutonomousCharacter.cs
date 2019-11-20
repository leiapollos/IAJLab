using System.Collections.Generic;
using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel.ForwardModelActions;
using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using Assets.Scripts.IAJ.Unity.DecisionMaking.GOB;
using Assets.Scripts.IAJ.Unity.DecisionMaking.MCTS;
using Assets.Scripts.IAJ.Unity.Movement.DynamicMovement;
using Assets.Scripts.IAJ.Unity.Pathfinding;
using Assets.Scripts.IAJ.Unity.Pathfinding.Heuristics;
using RAIN.Navigation;
using RAIN.Navigation.NavMesh;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.IAJ.Unity.Pathfinding.DataStructures.HPStructures;
using Assets.Scripts.IAJ.Unity.Pathfinding.Path;
using Assets.Scripts.GameManager;

namespace Assets.Scripts
{
    public class AutonomousCharacter : MonoBehaviour
    {
        //constants
        public const string SURVIVE_GOAL = "Survive";
        public const string GAIN_LEVEL_GOAL = "GainXP";
        public const string BE_QUICK_GOAL = "BeQuick";
        public const string GET_RICH_GOAL = "GetRich";

        public const float DECISION_MAKING_INTERVAL = 15.0f;
        //public fields to be set in Unity Editor
        public GameManager.GameManager GameManager;
        public Text SurviveGoalText;
        public Text GainXPGoalText;
        public Text BeQuickGoalText;
        public Text GetRichGoalText;
        public Text DiscontentmentText;
        public Text TotalProcessingTimeText;
        public Text BestDiscontentmentText;
        public Text ProcessedActionsText;
        public Text BestActionText;
        public Text DiaryText;
        public bool MCTSActive;


        public Goal BeQuickGoal { get; private set; }
        public Goal SurviveGoal { get; private set; }
        public Goal GetRichGoal { get; private set; }
        public Goal GainLevelGoal { get; private set; }
        public List<Goal> Goals { get; set; }
        public List<Action> Actions { get; set; }
        public Action CurrentAction { get; private set; }
        public DynamicCharacter Character { get; private set; }
        public DepthLimitedGOAPDecisionMaking GOAPDecisionMaking { get; set; }
        public MCTS MCTS { get; set; }
        public AStarPathfinding AStarPathFinding;

        //private fields for internal use only
        private Vector3 startPosition;
        private GlobalPath currentSolution;
        private GlobalPath currentSmoothedSolution;
        private NavMeshPathGraph navMesh;

        private bool draw;
        private float nextUpdateTime = 0.0f;
        private float previousGold = 0.0f;
        private int previousLevel = 1;
        private Vector3 previousTarget;

        private Animator characterAnimator;



        public void Initialize(NavMeshPathGraph navMeshGraph, AStarPathfinding pathfindingAlgorithm)
        {
            this.draw = true;
            this.navMesh = navMeshGraph;
            this.AStarPathFinding = pathfindingAlgorithm;
            this.AStarPathFinding.NodesPerSearch = uint.MaxValue;

            this.characterAnimator = this.GetComponentInChildren<Animator>();
        }

        public void Start()
        {
            this.draw = true;

            this.navMesh = NavigationManager.Instance.NavMeshGraphs[0];
            this.Character = new DynamicCharacter(this.gameObject);

            //initialize your pathfinding algorithm here!
            this.Initialize(NavigationManager.Instance.NavMeshGraphs[0], new NodeArrayAStarPathFinding(NavigationManager.Instance.NavMeshGraphs[0], new EuclideanDistanceHeuristic()));


            //initialization of the GOB decision making
            //let's start by creating 4 main goals

            this.SurviveGoal = new Goal(SURVIVE_GOAL, 2.0f);

            this.GainLevelGoal = new Goal(GAIN_LEVEL_GOAL, 2.0f)
            {
                ChangeRate = 0.2f
            };

            this.GetRichGoal = new Goal(GET_RICH_GOAL, 1.0f)
            {
                InsistenceValue = 4.0f,
                ChangeRate = 0.2f
            };

            this.BeQuickGoal = new Goal(BE_QUICK_GOAL, 1.0f)
            {
                ChangeRate = 0.1f
            };

            this.Goals = new List<Goal>();
            this.Goals.Add(this.SurviveGoal);
            this.Goals.Add(this.BeQuickGoal);
            this.Goals.Add(this.GetRichGoal);
            this.Goals.Add(this.GainLevelGoal);

            //initialize the available actions
            //Uncomment commented actions after you implement them

            this.Actions = new List<Action>();

            this.Actions.Add(new ShieldOfFaith(this));

            foreach (var chest in GameObject.FindGameObjectsWithTag("Chest"))
            {
                this.Actions.Add(new PickUpChest(this, chest));
            }

            foreach (var potion in GameObject.FindGameObjectsWithTag("ManaPotion"))
            {
                this.Actions.Add(new GetManaPotion(this, potion));
            }

            foreach (var potion in GameObject.FindGameObjectsWithTag("HealthPotion"))
            {
                this.Actions.Add(new GetHealthPotion(this, potion));
            }

            foreach (var enemy in GameObject.FindGameObjectsWithTag("Skeleton"))
            {
                this.Actions.Add(new SwordAttack(this, enemy));
                this.Actions.Add(new DivineSmite(this, enemy));
            }

            foreach (var enemy in GameObject.FindGameObjectsWithTag("Orc"))
            {
                this.Actions.Add(new SwordAttack(this, enemy));
            }

            foreach (var enemy in GameObject.FindGameObjectsWithTag("Dragon"))
            {
                this.Actions.Add(new SwordAttack(this, enemy));
            }

            var worldModel = new CurrentStateWorldModel(this.GameManager, this.Actions, this.Goals);
            this.GOAPDecisionMaking = new DepthLimitedGOAPDecisionMaking(worldModel, this.Actions, this.Goals);
            this.MCTS = new MCTS(worldModel);

            this.DiaryText.text = "My Diary \n I awoke. What a wonderful day to kill Monsters!\n";
        }

        void Update()
        {
            if (GameManager.gameEnded) return;

            if (Time.time > this.nextUpdateTime || this.GameManager.WorldChanged)
            {
                this.GameManager.WorldChanged = false;
                this.nextUpdateTime = Time.time + DECISION_MAKING_INTERVAL;

                //first step, perceptions
                //update the agent's goals based on the state of the world


                this.SurviveGoal.InsistenceValue = this.GameManager.characterData.MaxHP - this.GameManager.characterData.HP;

                this.BeQuickGoal.InsistenceValue += DECISION_MAKING_INTERVAL * this.BeQuickGoal.ChangeRate;
                if (this.BeQuickGoal.InsistenceValue > 10.0f)
                {
                    this.BeQuickGoal.InsistenceValue = 10.0f;
                }

                this.GainLevelGoal.InsistenceValue += this.GainLevelGoal.ChangeRate; //increase in goal over time
                if (this.GameManager.characterData.Level > this.previousLevel)
                {
                    this.GainLevelGoal.InsistenceValue -= this.GameManager.characterData.Level - this.previousLevel;
                    this.previousLevel = this.GameManager.characterData.Level;
                }

                this.GetRichGoal.InsistenceValue += this.GetRichGoal.ChangeRate; //increase in goal over time
                if (this.GetRichGoal.InsistenceValue > 10)
                {
                    this.GetRichGoal.InsistenceValue = 10.0f;
                }

                if (this.GameManager.characterData.Money > this.previousGold)
                {
                    this.GetRichGoal.InsistenceValue -= this.GameManager.characterData.Money - this.previousGold;
                    this.previousGold = this.GameManager.characterData.Money;
                }



                this.SurviveGoalText.text = "Survive: " + this.SurviveGoal.InsistenceValue;
                this.GainXPGoalText.text = "Gain Level: " + this.GainLevelGoal.InsistenceValue.ToString("F1");
                this.BeQuickGoalText.text = "Be Quick: " + this.BeQuickGoal.InsistenceValue.ToString("F1");
                this.GetRichGoalText.text = "GetRich: " + this.GetRichGoal.InsistenceValue.ToString("F1");
                this.DiscontentmentText.text = "Discontentment: " + this.CalculateDiscontentment().ToString("F1");

                //initialize Decision Making Proccess
                this.CurrentAction = null;
                this.GOAPDecisionMaking.InitializeDecisionMakingProcess();
                this.MCTS.InitializeMCTSearch();
            }

            //this.UpdateDLGOAP();
            if (MCTSActive)
            {
                if (this.CurrentAction == null)
                {
                    this.UpdateMCTS();
                }
            }

            if (this.CurrentAction != null)
            {
                if (this.CurrentAction.CanExecute())
                {
                    this.CurrentAction.Execute();
                }
            }

            //call the pathfinding method if the user specified a new goal
            if (this.AStarPathFinding.InProgress)
            {
                var finished = this.AStarPathFinding.Search(out this.currentSolution);
                if (finished && this.currentSolution != null)
                {
                    //lets smooth out the Path
                    this.startPosition = this.Character.KinematicData.position;
                    this.currentSmoothedSolution = StringPullingPathSmoothing.SmoothPath(this.Character.KinematicData.position, this.currentSolution);
                    //this.currentSolution.P
                    this.currentSmoothedSolution = this.smoothPath(this.Character.KinematicData.position, this.currentSolution);
                    this.currentSmoothedSolution.CalculateLocalPathsFromPathPositions(this.Character.KinematicData.position);
                    this.Character.Movement = new DynamicFollowPath(this.Character.KinematicData, this.currentSmoothedSolution)
                    {
                        MaxAcceleration = 200.0f,
                        MaxSpeed = 40.0f
                    };
                }
            }


            this.Character.Update();
            //manage the character's animation
            if (this.Character.KinematicData.velocity.sqrMagnitude > 0.1)
            {
                this.characterAnimator.SetBool("Walking", true);
            }
            else
            {
                this.characterAnimator.SetBool("Walking", false);
            }
        }

        private void UpdateMCTS()
        {
            bool newDecision = false;
            if (this.MCTS.InProgress)
            {
                //choose an action using the GOB Decision Making process
                var action = this.MCTS.Run();
                if (action != null)
                {
                    this.CurrentAction = action;
                    newDecision = true;
                }
            }
        }

            private void UpdateDLGOAP()
        {
            bool newDecision = false;
            if (this.GOAPDecisionMaking.InProgress)
            {
                //choose an action using the GOB Decision Making process
                var action = this.GOAPDecisionMaking.ChooseAction();
                if (action != null)
                {
                    this.CurrentAction = action;
                    newDecision = true;
                }
            }

            this.TotalProcessingTimeText.text = "Process. Time: " + this.GOAPDecisionMaking.TotalProcessingTime.ToString("F");
            this.BestDiscontentmentText.text = "Best Discontentment: " + this.GOAPDecisionMaking.BestDiscontentmentValue.ToString("F");
            this.ProcessedActionsText.text = "Act. comb. processed: " + this.GOAPDecisionMaking.TotalActionCombinationsProcessed;

            if (this.GOAPDecisionMaking.BestAction != null)
            {
                if (newDecision)
                {
                    this.DiaryText.text += Time.time + " I decided to " + GOAPDecisionMaking.BestAction.Name + "\n";
                }
                var actionText = "";
                foreach (var action in this.GOAPDecisionMaking.BestActionSequence)
                {
                    actionText += "\n" + action.Name;
                }
                this.BestActionText.text = "Best Action Sequence: " + actionText;
            }
            else
            {
                this.BestActionText.text = "Best Action Sequence:\nNone";
            }
        }

        public void StartPathfinding(Vector3 targetPosition)
        {
            //if the targetPosition received is the same as a previous target, then this a request for the same target
            //no need to redo the pathfinding search
            if (!this.previousTarget.Equals(targetPosition))
            {
                this.AStarPathFinding.InitializePathfindingSearch(this.Character.KinematicData.position, targetPosition);
                this.previousTarget = targetPosition;
            }
        }

        public void OnDrawGizmos()
        {
            if (this.draw)
            {
                //draw the current Solution Path if any (for debug purposes)
                if (this.currentSolution != null)
                {
                    var previousPosition = this.startPosition;
                    foreach (var pathPosition in this.currentSolution.PathPositions)
                    {
                        Debug.DrawLine(previousPosition, pathPosition, Color.red);
                        previousPosition = pathPosition;
                    }

                    previousPosition = this.startPosition;
                    foreach (var pathPosition in this.currentSmoothedSolution.PathPositions)
                    {
                        Debug.DrawLine(previousPosition, pathPosition, Color.green);
                        previousPosition = pathPosition;
                    }
                }


            }
        }

        public float CalculateDiscontentment()
        {
            var discontentment = 0.0f;

            foreach (var goal in this.Goals)
            {
                discontentment += goal.GetDiscontentment();
            }
            return discontentment;
        }


        protected GlobalPath smoothPath(Vector3 position, GlobalPath actual)
        {
            var smoothedPath = new GlobalPath();
            smoothedPath.PathPositions.Add(position);
            smoothedPath.PathPositions.AddRange(actual.PathPositions);

            int i = 0;
            while (i < smoothedPath.PathPositions.Count - 2)
            {
                if (walkable(smoothedPath.PathPositions[i], smoothedPath.PathPositions[i + 2]))
                {
                    smoothedPath.PathPositions.RemoveAt(i + 1);
                }
                else
                {
                    i++;
                }
            }
            return smoothedPath;
        }

        protected bool walkable(Vector3 p1, Vector3 p2)
        {
            Vector3 direction = p2 - p1;
            return !Physics.Raycast(p1, direction, direction.magnitude);
        }
    }
}
