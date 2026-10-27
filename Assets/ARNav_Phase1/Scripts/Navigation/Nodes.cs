using UnityEngine;

[System.Serializable]
public class Node
{
    public string Id; // example: L1_R101, STAIR_L1_BOT
    public Vector3 Position; // in AT_01 Master space
    public int Floor; // 1 or 2
    public string AuthoritativeArea; // AT_01, AT_02, AT_03, AT_04
    public bool IsTransition; // true for stairs/elevator
    public Node(string id, Vector3 pos, int floor, string area, bool trans = false)
    { Id = id; Position = pos; Floor = floor; AuthoritativeArea = area; IsTransition = trans; }
}