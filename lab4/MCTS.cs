using Assets.Scripts.GameManager;
using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel.ForwardModelActions;
using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using System;
using System.Collections.Generic;
using UnityEngine;
using Action = Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel.Action;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.MCTS
{
    public class MCTS
    {
        public const float C = 1.4f;
        public bool InProgress { get; private set; }
        public int MaxIterations { get; set; }
        public int MaxIterationsProcessedPerFrame { get; set; }
        public int MaxPlayoutDepthReached { get; private set; }
        public int MaxSelectionDepthReached { get; private set; }
        public float TotalProcessingTime { get; private set; }
        public MCTSNode BestFirstChild { get; set; }
        public List<Action> BestActionSequence { get; private set; }


        protected int CurrentIterations { get; set; }
        protected int CurrentIterationsInFrame { get; set; }
        protected int CurrentDepth { get; set; }

        protected CurrentStateWorldModel CurrentStateWorldModel { get; set; }
        protected MCTSNode InitialNode { get; set; }
        protected System.Random RandomGenerator { get; set; }
        
        

        public MCTS(CurrentStateWorldModel currentStateWorldModel)
        {
            this.InProgress = false;
            this.CurrentStateWorldModel = currentStateWorldModel;
            this.MaxIterations = 1000;
            this.MaxIterationsProcessedPerFrame = 10;
            this.RandomGenerator = new System.Random();
        }


        public void InitializeMCTSearch()
        {
            this.MaxPlayoutDepthReached = 0;
            this.MaxSelectionDepthReached = 0;
            this.CurrentIterations = 0;
            this.CurrentIterationsInFrame = 0;
            this.TotalProcessingTime = 0.0f;
            this.CurrentStateWorldModel.Initialize();
            this.InitialNode = new MCTSNode(this.CurrentStateWorldModel)
            {
                Action = null,
                Parent = null,
                PlayerID = 0
            };
            this.InProgress = true;
            this.BestFirstChild = null;
            this.BestActionSequence = new List<Action>();
        }

        public Action Run()
        {
            MCTSNode selectedNode;
            Reward reward;

            var startTime = Time.realtimeSinceStartup;

            this.CurrentIterationsInFrame = 0;

            while (this.CurrentIterations < this.MaxIterations)     
            {
                selectedNode = Selection(this.InitialNode);
                reward = Playout(selectedNode.State);                       
                Backpropagate(selectedNode, reward);
                this.CurrentIterations++;
            }
            this.InProgress = false;
            this.TotalProcessingTime += startTime;
            return BestFinalAction(this.InitialNode);
        }

        protected MCTSNode Selection(MCTSNode initialNode)
        {
            Action nextAction;
            MCTSNode currentNode = initialNode;
            MCTSNode bestChild;

            while (!currentNode.State.IsTerminal())
            {
                nextAction = currentNode.State.GetNextAction();
                if (nextAction != null)
                    return Expand(currentNode, nextAction);
                else
                {
                    bestChild = BestUCTChild(currentNode);
                    if (bestChild == null)
                    {
                        return currentNode;
                    }
                    currentNode = bestChild;
                }
            }
            return currentNode;
        }

        protected virtual Reward Playout(WorldModel initialPlayoutState)
        {
            WorldModel newState = initialPlayoutState;
            while (!newState.IsTerminal())
            {
                var actions = newState.GetExecutableActions();
                int index = this.RandomGenerator.Next(0, actions.Length);
                Action action = actions[index];
                action.ApplyActionEffects(newState);
                newState.CalculateNextPlayer();
            }
            return new Reward
            {
                PlayerID = newState.GetNextPlayer(),
                Value = newState.GetScore()
            };
        }

        protected virtual void Backpropagate(MCTSNode node, Reward reward)
        {
            while (node != null)
            {
                node.N = node.N + 1;
                node.Q = node.Q + reward.Value;
                node = node.Parent;
            }
        }

        protected MCTSNode Expand(MCTSNode parent, Action action)
        {
            WorldModel newModel = parent.State.GenerateChildWorldModel();
            action.ApplyActionEffects(newModel);
            newModel.CalculateNextPlayer();
            MCTSNode node = new MCTSNode(newModel)
            {
                Action = action,
                Parent = parent,
                PlayerID = newModel.GetNextPlayer(),
                Q = 0,
                N = 0
            };
            parent.ChildNodes.Add(node);
            return node;
        }

        //gets the best child of a node, using the UCT formula
        protected virtual MCTSNode BestUCTChild(MCTSNode node)
        {
            float maxValue = float.NegativeInfinity;
            MCTSNode best = null;
            foreach (MCTSNode child in node.ChildNodes)
            {
                float score = child.Q / child.N;
                score += C * (float)Math.Sqrt(Math.Log10((double)(node.N)) / child.N);
                if (score > maxValue)
                {
                    maxValue = score;
                    best = child;
                }
            }
            return best;
        }

        //this method is very similar to the bestUCTChild, but it is used to return the final action of the MCTS search, and so we do not care about
        //the exploration factor
        protected MCTSNode BestChild(MCTSNode node)
        {
            float maxValue = float.NegativeInfinity;
            MCTSNode best = null;
            float score = 0;
            foreach (MCTSNode child in node.ChildNodes)
            {
                Debug.Log("Q: " + child.Q + "  N: " + child.N);
                score = child.Q / child.N;
                if (score > maxValue)
                {
                    maxValue = score;
                    best = child;
                }
            }
            return best;
        }


        protected Action BestFinalAction(MCTSNode node)
        {
            var bestChild = this.BestChild(node);
            if (bestChild == null) return null;

            this.BestFirstChild = bestChild;

            //this is done for debugging proposes only
            this.BestActionSequence = new List<Action>();
            this.BestActionSequence.Add(bestChild.Action);
            node = bestChild;

            while(!node.State.IsTerminal())
            {
                Debug.Log("i am here");
                bestChild = this.BestChild(node);
                if (bestChild == null) break;
                this.BestActionSequence.Add(bestChild.Action);
                node = bestChild;    
            }

            return this.BestFirstChild.Action;
        }

    }
}
