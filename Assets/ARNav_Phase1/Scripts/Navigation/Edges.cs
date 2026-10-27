[System.Serializable]
public class Edge
{
    public string From; public string To; public float Cost;
    public bool IsStairs; public bool IsElevator; public bool Enabled = true;
    public Edge(string f, string t, float c, bool stairs = false, bool elev = false)
    { From = f; To = t; Cost = c; IsStairs = stairs; IsElevator = elev; }
}