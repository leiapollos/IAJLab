using Assets.Scripts.GameManager;
using Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel;
using UnityEngine;

namespace Assets.Scripts.IAJ.Unity.DecisionMaking.ForwardModel.ForwardModelActions
{
    public class Rest : Action
    {
        protected AutonomousCharacter Character { get; set; }

        private int hpChange;
        private int timeChange;

        public Rest(AutonomousCharacter character) : base("ShieldOfFaith")
        {
            this.Character = character;
            this.hpChange = 2;
            this.timeChange = 5;
        }

        public override float GetGoalChange(Goal goal)
        {
            var change = base.GetGoalChange(goal);

            if (goal.Name == AutonomousCharacter.SURVIVE_GOAL)
            {
                change -= this.hpChange;
            }
            else if (goal.Name == AutonomousCharacter.BE_QUICK_GOAL)
            {
                change += this.timeChange;
            }

            return change;
        }

        public override bool CanExecute()
        {
            if (!base.CanExecute()) return false;
            return this.Character.GameManager.characterData.HP >= this.Character.GameManager.characterData.MaxHP - this.hpChange && this.Character.GameManager.characterData.Time >= this.timeChange;
        }

        public override bool CanExecute(WorldModel worldModel)
        {
            if (!base.CanExecute(worldModel)) return false;

            var hp = (int)worldModel.GetProperty(Properties.HP);
            var maxHp = (int)worldModel.GetProperty(Properties.MAXHP);
            var time = (int)worldModel.GetProperty(Properties.TIME);
            return hp >= maxHp - this.hpChange && time >= this.timeChange;
        }

        public override void Execute()
        {
            base.Execute();
            this.Character.GameManager.Rest();
        }

        public override void ApplyActionEffects(WorldModel worldModel)
        {
            base.ApplyActionEffects(worldModel);

            var surviveValue = worldModel.GetGoalValue(AutonomousCharacter.SURVIVE_GOAL);
            worldModel.SetGoalValue(AutonomousCharacter.SURVIVE_GOAL, surviveValue - this.hpChange);

            var hp = (int)worldModel.GetProperty(Properties.HP);
            worldModel.SetProperty(Properties.HP, hp + this.hpChange);

            var quickValue = worldModel.GetGoalValue(AutonomousCharacter.BE_QUICK_GOAL);
            worldModel.SetGoalValue(AutonomousCharacter.SURVIVE_GOAL, quickValue + this.timeChange);

            var time = (int)worldModel.GetProperty(Properties.TIME);
            worldModel.SetGoalValue(Properties.TIME, time - this.timeChange);
        }
    }
}

