using System;
using System.Collections.Generic;
using Godot;
using HydroventSim.Enums;

namespace HydroventSim.Events
{
    public static class EventSystem
    {
        private static readonly Dictionary<string, Delegate> _eventHandlers = new Dictionary<string, Delegate>();

        public static void Subscribe<T>(string eventName, EventHandler<T> handler)
        {
            if (!_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = null;
            }
            _eventHandlers[eventName] = (EventHandler<T>)_eventHandlers[eventName] + handler;
        }

        public static void Unsubscribe<T>(string eventName, EventHandler<T> handler)
        {
            if (_eventHandlers.ContainsKey(eventName))
            {
                _eventHandlers[eventName] = (EventHandler<T>)_eventHandlers[eventName] - handler;
            }
        }

        public static void Raise<T>(string eventName, object sender, T args)
        {
            if (_eventHandlers.TryGetValue(eventName, out var handlers))
            {
                ((EventHandler<T>)handlers)?.Invoke(sender, args);
            }
        }

        public static void Clear()
        {
            _eventHandlers.Clear();
        }
    }

    public class OrganismEventArgs : EventArgs
    {
        public OrganismType OrganismType { get; }
        public int Count { get; }
        public float Energy { get; }

        public OrganismEventArgs(OrganismType type, int count, float energy)
        {
            OrganismType = type;
            Count = count;
            Energy = energy;
        }
    }

    public class EcosystemEventArgs : EventArgs
    {
        public string EventType { get; }
        public string Message { get; }
        public float Severity { get; }

        public EcosystemEventArgs(string eventType, string message, float severity)
        {
            EventType = eventType;
            Message = message;
            Severity = severity;
        }
    }

    public class EruptionEventArgs : EventArgs
    {
        public Vector2 Center { get; }
        public float Radius { get; }
        public float Intensity { get; }
        public string Message { get; }

        public EruptionEventArgs(Vector2 center, float radius, float intensity, string message)
        {
            Center = center;
            Radius = radius;
            Intensity = intensity;
            Message = message;
        }
    }

    public static class EventNames
    {
        public const string OrganismBorn = "OrganismBorn";
        public const string OrganismDied = "OrganismDied";
        public const string PopulationChanged = "PopulationChanged";
        public const string EruptionEvent = "EruptionEvent";
        public const string EnergyTransfer = "EnergyTransfer";
        public const string EcosystemHealthChanged = "EcosystemHealthChanged";
    }
}
