using System.Collections.Generic;
using UnityEngine;

public enum SocketType { Forward, Back, Left, Right }

public class RoadModule : MonoBehaviour
{
    [Header("Sockets (empty child transforms on connection edges)")]
    public Transform forward; // +Z points outward
    public Transform back;    // +Z points outward
    public Transform left;    // +Z points outward
    public Transform right;   // +Z points outward

    public Transform GetSocket(SocketType t)
    {
        return t switch
        {
            SocketType.Forward => forward,
            SocketType.Back    => back,
            SocketType.Left    => left,
            SocketType.Right   => right,
            _ => null
        };
    }

    public static SocketType Opposite(SocketType t)
    {
        return t switch
        {
            SocketType.Forward => SocketType.Back,
            SocketType.Back    => SocketType.Forward,
            SocketType.Left    => SocketType.Right,
            SocketType.Right   => SocketType.Left,
            _ => t
        };
    }

    // Helper to iterate sockets safely
    public IEnumerable<(Transform tf, SocketType type)> AllSockets()
    {
        if (forward) yield return (forward, SocketType.Forward);
        if (back)    yield return (back,    SocketType.Back);
        if (left)    yield return (left,    SocketType.Left);
        if (right)   yield return (right,   SocketType.Right);
    }
}