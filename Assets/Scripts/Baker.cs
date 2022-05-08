using System;
using System.Collections.Generic;
using System.Diagnostics;
using JetBrains.Annotations;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Baker : MonoBehaviour
{
    private const long MaxMsPerFrame = 16;
    [CanBeNull]
    private static Baker _baker;
    private readonly Queue<Action> _queue = new Queue<Action>();
    private float _cumulativeBakingTime;

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
        if (_queue.Count == 0) return;

        var totalTime = 0L;

        while (totalTime < MaxMsPerFrame && _queue.Count > 0)
        {
            var stopwatch = Stopwatch.StartNew();
            var action = _queue.Dequeue();
            action();
            stopwatch.Stop();
            totalTime += stopwatch.ElapsedMilliseconds;
        }

        _cumulativeBakingTime += totalTime / 1000f;

        if (_queue.Count > 0) return;

        Debug.Log($"Baking session finished in {_cumulativeBakingTime:F2} seconds");
        _cumulativeBakingTime = 0f;
    }

    public void Schedule(Action action)
    {
        _queue.Enqueue(action);
    }
}