using System.Collections.Generic;
using UnityEngine;

public static class DijkstraEngine
{
    public static List<string> FindPath(GraphDatabase g, string start, string end,
        bool allowStairs = true, bool allowElevator = true)
    {
        var dist = new Dictionary<string, float>();
        var prev = new Dictionary<string, string>();
        var unvisited = new HashSet<string>();

        foreach (var n in g.nodes) { dist[n.Id] = float.PositiveInfinity; unvisited.Add(n.Id); }
        if (!dist.ContainsKey(start) || !dist.ContainsKey(end)) return null;
        dist[start] = 0;

        while (unvisited.Count > 0)
        {
            string u = null; float best = float.PositiveInfinity;
            foreach (var id in unvisited)
                if (dist[id] < best) { best = dist[id]; u = id; }
            if (u == null || float.IsPositiveInfinity(best)) break; // disconnected
            if (u == end) break;
            unvisited.Remove(u);

            foreach (var e in g.edges)
            {
                if (!e.Enabled || e.From != u) continue;
                if (!unvisited.Contains(e.To)) continue;
                if (e.IsStairs && !allowStairs) continue;
                if (e.IsElevator && !allowElevator) continue;
                float alt = dist[u] + e.Cost;
                if (alt < dist[e.To]) { dist[e.To] = alt; prev[e.To] = u; }
            }
        }
        if (start != end && !prev.ContainsKey(end)) return null; // no route
        var path = new List<string> { end };
        while (path[path.Count - 1] != start)
        {
            if (!prev.TryGetValue(path[path.Count - 1], out var p)) return null;
            path.Add(p);
        }
        path.Reverse();
        return path;
    }

    public static List<Vector3> ToWorldPath(GraphDatabase g, List<string> ids)
    {
        var l = new List<Vector3>();
        foreach (var id in ids) l.Add(g.Get(id).Position);
        return l;
    }
}