using System;
using System.Collections.Generic;

namespace HydroventSim.AI
{
    public enum NodeStatus
    {
        Success,
        Failure,
        Running
    }

    public abstract class BehaviorNode
    {
        protected List<BehaviorNode> Children = new List<BehaviorNode>();

        public abstract NodeStatus Execute(float deltaTime);

        public void AddChild(BehaviorNode child)
        {
            Children.Add(child);
        }
    }

    public class SelectorNode : BehaviorNode
    {
        public override NodeStatus Execute(float deltaTime)
        {
            foreach (var child in Children)
            {
                var status = child.Execute(deltaTime);
                if (status != NodeStatus.Failure)
                {
                    return status;
                }
            }
            return NodeStatus.Failure;
        }
    }

    public class SequenceNode : BehaviorNode
    {
        public override NodeStatus Execute(float deltaTime)
        {
            foreach (var child in Children)
            {
                var status = child.Execute(deltaTime);
                if (status != NodeStatus.Success)
                {
                    return status;
                }
            }
            return NodeStatus.Success;
        }
    }

    public class ParallelNode : BehaviorNode
    {
        private int _requiredSuccess;

        public ParallelNode(int requiredSuccess)
        {
            _requiredSuccess = requiredSuccess;
        }

        public override NodeStatus Execute(float deltaTime)
        {
            int successCount = 0;
            int failureCount = 0;

            foreach (var child in Children)
            {
                var status = child.Execute(deltaTime);
                if (status == NodeStatus.Success)
                {
                    successCount++;
                    if (successCount >= _requiredSuccess)
                    {
                        return NodeStatus.Success;
                    }
                }
                else if (status == NodeStatus.Failure)
                {
                    failureCount++;
                    if (failureCount > Children.Count - _requiredSuccess)
                    {
                        return NodeStatus.Failure;
                    }
                }
            }

            return NodeStatus.Running;
        }
    }

    public class InverterNode : BehaviorNode
    {
        public InverterNode(BehaviorNode child)
        {
            Children.Add(child);
        }

        public override NodeStatus Execute(float deltaTime)
        {
            var status = Children[0].Execute(deltaTime);
            if (status == NodeStatus.Success) return NodeStatus.Failure;
            if (status == NodeStatus.Failure) return NodeStatus.Success;
            return status;
        }
    }

    public class ConditionNode : BehaviorNode
    {
        private Func<bool> _condition;

        public ConditionNode(Func<bool> condition)
        {
            _condition = condition;
        }

        public override NodeStatus Execute(float deltaTime)
        {
            return _condition() ? NodeStatus.Success : NodeStatus.Failure;
        }
    }

    public class ActionNode : BehaviorNode
    {
        private Func<float, NodeStatus> _action;

        public ActionNode(Func<float, NodeStatus> action)
        {
            _action = action;
        }

        public override NodeStatus Execute(float deltaTime)
        {
            return _action(deltaTime);
        }
    }

    public class WaitNode : BehaviorNode
    {
        private float _waitTime;
        private float _currentTime;

        public WaitNode(float waitTime)
        {
            _waitTime = waitTime;
            _currentTime = 0f;
        }

        public override NodeStatus Execute(float deltaTime)
        {
            _currentTime += deltaTime;
            if (_currentTime >= _waitTime)
            {
                _currentTime = 0f;
                return NodeStatus.Success;
            }
            return NodeStatus.Running;
        }
    }
}
