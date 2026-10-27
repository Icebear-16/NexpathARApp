using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "ARNav/DuplexGraph")]
public class GraphDatabase : ScriptableObject
{
    public List<Node> nodes = new();
    public List<Edge> edges = new();

    public Node Get(string id)
    {
        foreach (var n in nodes) if (n.Id == id) return n;
        return null;
    }

    public List<Edge> From(string id)
    {
        var list = new List<Edge>();
        foreach (var e in edges) if (e.From == id && e.Enabled) list.Add(e);
        return list;
    }

    public void LoadFromDatabase()
    {
        try
        {
            if (DatabaseManager.I != null && DatabaseManager.I.IsReady)
            {
                var dbNodes = DbQueries.LoadAllNodes(DatabaseManager.I);
                var dbEdges = DbQueries.LoadAllEdges(DatabaseManager.I);

                if (dbNodes != null && dbNodes.Count > 0)
                {
                    if (nodes == null || nodes.Count == 0)
                    {
                        nodes = dbNodes;
                    }
                    else
                    {
                        // Merge database nodes into existing graph nodes, never erasing authored nodes not in DB
                        foreach (var dbN in dbNodes)
                        {
                            var existing = nodes.Find(n => n.Id == dbN.Id);
                            if (existing != null)
                            {
                                existing.Position = dbN.Position;
                                existing.Floor = dbN.Floor;
                                existing.AuthoritativeArea = dbN.AuthoritativeArea;
                                existing.IsTransition = dbN.IsTransition;
                            }
                            else
                            {
                                nodes.Add(dbN);
                            }
                        }
                    }
                }

                if (dbEdges != null && dbEdges.Count > 0)
                {
                    if (edges == null || edges.Count == 0)
                    {
                        edges = dbEdges;
                    }
                    else
                    {
                        // Merge database edges into existing graph edges, preserving any authored edges not in DB
                        foreach (var dbE in dbEdges)
                        {
                            var existing = edges.Find(e => e.From == dbE.From && e.To == dbE.To);
                            if (existing != null)
                            {
                                existing.Cost = dbE.Cost;
                                existing.IsStairs = dbE.IsStairs;
                                existing.IsElevator = dbE.IsElevator;
                            }
                            else
                            {
                                edges.Add(dbE);
                            }
                        }
                    }
                }

                Debug.Log($"[GraphDB] Active graph data: {nodes.Count} nodes / {edges.Count} edges");
                return;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GraphDB] LoadFromDatabase encountered an issue ({ex.Message}), retaining asset data ({nodes.Count} nodes, {edges.Count} edges).");
        }
    }
}
