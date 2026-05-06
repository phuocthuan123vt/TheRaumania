using System;

namespace BehaviorTree
{
    public class ActionNode : Node
    {
        private Func<NodeState> action;

        public ActionNode(Func<NodeState> action)
        {
            this.action = action;
        }

        public override NodeState Evaluate()
        {
            state = action.Invoke();
            return state;
        }
    }
}