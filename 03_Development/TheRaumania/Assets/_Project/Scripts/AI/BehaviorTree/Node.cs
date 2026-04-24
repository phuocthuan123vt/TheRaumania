namespace BehaviorTree
{
    public enum NodeState
    {
        Running,
        Success,
        Failure
    }
    public abstract class Node
    {
        protected NodeState state;
        public NodeState State { get { return state; } }
        public abstract NodeState Evaluate();
    }
}
