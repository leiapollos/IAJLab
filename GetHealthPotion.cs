using Assets.Scripts.GameManager;
using Assets.Scripts.IAJ.Unity.DecisionMaking.GOB;
using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel.ForwardModelActions
{
    public class GetHealthPotion : Action
    {
        public GetHealthPotion(string actionName, AutonomousCharacter character) : base("GetHealthPotion") //target is self
        {
            this.Character = character;
        }
        public override float GetGoalChange(Goal goal)
        {
            var change = base.GetGoalChange(goal);
            if (goal.Name == AutonomousCharacter.SURVIVE_GOAL) change -= 5.0f;
            return change;
        }
        public override bool CanExecute()
        {
            if (!base.CanExecute()) return false;
            return true;
        }

        public override bool CanExecute(WorldModel worldModel)
        {
            if (!base.CanExecute(worldModel)) return false;
            return true;
        }

        public override void Execute()
        {
            base.Execute();
            this.Character.GameManager.GetHealtPotion(this.Target);
        }

        public override void ApplyActionEffects(WorldModel worldModel)
        {
            base.ApplyActionEffects(worldModel);

            var goalValue = worldModel.GetGoalValue(AutonomousCharacter.SURVIVE_GOAL);
            worldModel.SetGoalValue(AutonomousCharacter.GET_RICH_GOAL, goalValue - 5.0f);

            var health = (int)worldModel.GetProperty(Properties.HP);
            worldModel.SetProperty(Properties.HP, Properties.MAXHP);

            //disables the target object so that it can't be reused again
            worldModel.SetProperty(this.Target.name, false);
        }

        public override float GetHValue(WorldModel worldModel)
        {
            return base.GetHValue(worldModel);
        }
    }
}
