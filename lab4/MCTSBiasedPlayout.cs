using Assets.Scripts.GameManager;
using System;
using System.Collections.Generic;
using Assets.Scripts.IAJ.Unity.DecisionMaking;
using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using System.Linq;
using UnityEngine;
using Action = Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel.Action;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.MCTS
{
    public class MCTSBiasedPlayout : MCTS
    {
        public int DEPTH_LIMIT = 6;

        public MCTSBiasedPlayout(CurrentStateWorldModel currentStateWorldModel) : base(currentStateWorldModel)
        {
        }

        protected override Reward Playout(WorldModel initialPlayoutState)
        {
            //WorldModel newState = initialPlayoutState.GenerateChildWorldModel();
            FutureStateWorldModel newState = new FutureStateWorldModel((FutureStateWorldModel)initialPlayoutState);
            Reward reward = new Reward();
            int numberOfIterations = 0;
            while (!newState.IsTerminal() && numberOfIterations <= DEPTH_LIMIT)
            {
                Action[] possibleActions = newState.GetExecutableActions();
                List<float> heuristics = new List<float>();
                for (int i = 0; i < possibleActions.Length; i++)
                {
                    heuristics.Add(possibleActions[i].GetHValue(newState));
                }

                int index = chooseAction(possibleActions, heuristics);
                Action bestAction = possibleActions[index];
                bestAction.ApplyActionEffects(newState);
                newState.CalculateNextPlayer();
                reward.PlayerID = newState.GetNextPlayer();
                reward.Value = heuristics[index];
                if (DEPTH_LIMIT > 0) numberOfIterations++;
            }
            return reward;
        }

        protected int chooseAction(Action[] possibleActions, List<float> heuristics)
        {
            Action bestAction = null;
            float maxHeuristic = -1.0f;
            int best = 0;
            for (int i = 0; i < possibleActions.Length; i++)
            {
                if (heuristics[i] > maxHeuristic)
                {
                    maxHeuristic = heuristics[i];
                    bestAction = possibleActions[i];
                    best = i;

                }
            }
            return best;
        }
    }
}