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
        public int Runs { get; set; }
        public int CurrentMCTS { get; set; }
        public int MaxPlayouts { get; set; }
        public MCTSNode BestFirstChild { get; set; }
        public List<Action> BestActionSequence { get; private set; }


        protected int CurrentIterations { get; set; }
        protected int CurrentIterationsInFrame { get; set; }
        protected int CurrentDepth { get; set; }

        protected CurrentStateWorldModel CurrentStateWorldModel { get; set; }
        protected MCTSNode InitialNode { get; set; }
        protected MCTSNode[] InitialNodes { get; set; }
        protected System.Random RandomGenerator { get; set; }



        public MCTS(CurrentStateWorldModel currentStateWorldModel)
        {
            this.InProgress = false;
            this.CurrentStateWorldModel = currentStateWorldModel;
            this.MaxIterations = 1000;
            this.MaxIterationsProcessedPerFrame = 100;
            this.Runs = 5;
            this.CurrentMCTS = 0;
            this.MaxPlayouts = 5;
            this.RandomGenerator = new System.Random();
        }


        public void InitializeMCTSearch()
        {
            this.MaxPlayoutDepthReached = 0;
            this.MaxSelectionDepthReached = 0;
            this.CurrentIterations = 0;
            this.CurrentIterationsInFrame = 0;
            this.TotalProcessingTime = 0.0f;
            this.CurrentMCTS = 0;
            this.CurrentStateWorldModel.Initialize();
            //this.InitialNode = new MCTSNode(this.CurrentStateWorldModel)
            //{
            //    Action = null,
            //    Parent = null,
            //    PlayerID = 0
            //};
            this.InitialNodes = new MCTSNode[this.Runs];
            for (int i = 0; i < this.Runs; i++)
            {
                InitialNodes[i] = new MCTSNode(this.CurrentStateWorldModel)
                {
                    Action = null,
                    Parent = null,
                    PlayerID = 0
                };
            }
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
                if (this.CurrentIterationsInFrame > this.MaxIterationsProcessedPerFrame)
                {
                    TotalProcessingTime += Time.realtimeSinceStartup - startTime;
                    return null;
                }

                //selectedNode = Selection(this.InitialNode);
                selectedNode = Selection(this.InitialNodes[this.CurrentMCTS]);
                reward = Playout(selectedNode.State);
                Backpropagate(selectedNode, reward);
                this.CurrentIterations++;
                this.CurrentIterationsInFrame++;
                this.CurrentMCTS = (this.CurrentMCTS + 1) % this.Runs;
            }

            if (this.CurrentIterations >= this.MaxIterations)
                this.InProgress = false;

            TotalProcessingTime += Time.realtimeSinceStartup - startTime;
            //return BestFinalAction(this.InitialNode);
            return BestAverageChildAction(this.InitialNodes);
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
            WorldModel currState = initialPlayoutState;

            while (!currState.IsTerminal())
            {
                var actions = currState.GetExecutableActions();
                if (actions.Length > 0)
                {
                    //currState = currState.GenerateChildWorldModel();
                    int next = this.RandomGenerator.Next(0, actions.Length);
                    currState = StochasticPlayout(actions[next], currState);
                    currState.CalculateNextPlayer();
                    //actions[next].ApplyActionEffects(currState);
                    //currState.CalculateNextPlayer();
                }
                else
                {
                    break;
                }
            }
            return new Reward
            {
                PlayerID = currState.GetNextPlayer(),
                Value = currState.GetScore()
            };
        }

        // Only worth running multiple playouts in case action is Sword Attack
        protected virtual WorldModel StochasticPlayout(Action action, WorldModel currState)
        {
            if (action.Name.Equals("SwordAttack") && this.MaxPlayouts > 0)
            {
                WorldModel[] tests = new WorldModel[this.MaxPlayouts];
                for (int i = 0; i < this.MaxPlayouts; i++)
                {
                    tests[i] = currState.GenerateChildWorldModel();
                    action.ApplyActionEffects(tests[i]);
                }

                currState = AverageState(tests, (SwordAttack)action);
            }
            else
            {
                currState = currState.GenerateChildWorldModel();
                action.ApplyActionEffects(currState);
            }

            return currState;
        }

        protected virtual WorldModel AverageState(WorldModel[] tests, SwordAttack enemy)
        {
            int hp = 0;
            int shieldHp = 0;
            int xp = 0;
            int baseXp = 0;
            int deadEnemies = 0;
            bool enemyAlive = true;

            for (int i = 0; i < this.MaxPlayouts; i++)
            {
                hp += (int)tests[i].GetProperty(Properties.HP);
                shieldHp += (int)tests[i].GetProperty(Properties.ShieldHP);

                if ((bool)tests[i].GetProperty(enemy.Target.name) == false)
                {
                    xp += (int)tests[i].GetProperty(Properties.XP);
                    
                    switch(enemy.Target.tag)
                    {
                        case "Skeleton":
                            baseXp = xp - 3;
                            break;
                        case "Orc":
                            baseXp = xp - 10;
                            break;
                        case "Dragon":
                            baseXp = xp - 20;
                            break;
                    }

                    deadEnemies++;
                }
            }

            hp /= this.MaxPlayouts;
            shieldHp /= this.MaxPlayouts;

            if (deadEnemies > this.MaxPlayouts / 2)
            {
                xp /= deadEnemies;
                enemyAlive = false;
            }
            else
            {
                xp = baseXp;
            }

            WorldModel average = tests[0];
            average.SetProperty(Properties.HP, hp);
            average.SetProperty(Properties.ShieldHP, shieldHp);
            average.SetProperty(enemy.Target.name, enemyAlive);
            average.SetProperty(Properties.XP, xp);
            return average;
        }

        protected virtual void Backpropagate(MCTSNode node, Reward reward)
        {
            while (node != null)
            {
                node.N++;
                node.Q += reward.Value;
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
            float score = 0;
            foreach (MCTSNode child in node.ChildNodes)
            {
                score = child.Q / child.N + C * (float)Math.Sqrt(Math.Log(node.N) / child.N);
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

            while (!node.State.IsTerminal())
            {
                bestChild = this.BestChild(node);
                if (bestChild == null)
                {
                    break;
                }
                this.BestActionSequence.Add(bestChild.Action);
                node = bestChild;
            }

            return this.BestFirstChild.Action;
        }

        protected virtual Action BestAverageChildAction(MCTSNode[] nodes)
        {
            if (nodes[0].ChildNodes.Count == 0) return null;

            List<float> results = new List<float>(nodes[0].ChildNodes.Count);

            float bestReward = float.MinValue;

            this.BestFirstChild = nodes[0].ChildNodes[0];

            for (int i = 0; i < results.Count; i++) {

                foreach (MCTSNode n in nodes)
                {
                    results[i] += n.ChildNodes[i].Q / n.ChildNodes[i].N;
                }

                results[i] /= results.Count;

                if (results[i] > bestReward)
                {
                    bestReward = results[i];
                    this.BestFirstChild = nodes[0].ChildNodes[i];
                }
            }

            MCTSNode child = this.BestFirstChild;
            this.BestActionSequence.Clear();
            while (child != null)
            {
                this.BestActionSequence.Add(child.Action);
                child = BestChild(child);
            }

            return this.BestFirstChild.Action;
        }

    }
}
