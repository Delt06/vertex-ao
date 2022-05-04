using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine;

public class Baker : MonoBehaviour
{
    private const long MaxMsPerFrame = 16;
    [CanBeNull]
    private static Baker _baker;
    private readonly Queue<Action> _queue = new Queue<Action>();

    public static Baker Instance
    {
        get
        {
            if (_baker != null) return _baker;

            _baker = new GameObject("[Baker]").AddComponent<Baker>();
            DontDestroyOnLoad(_baker);
            return _baker;
        }
    }

    private void Update()
    {
        var totalTime = 0L;

        while (totalTime < MaxMsPerFrame && _queue.Count > 0)
        {
            var stopwatch = Stopwatch.StartNew();
            var action = _queue.Dequeue();
            action();
            stopwatch.Stop();
            totalTime += stopwatch.ElapsedMilliseconds;
        }
    }

    public void Schedule(Action action)
    {
        _queue.Enqueue(action);
    }
}