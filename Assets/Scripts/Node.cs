using NUnit.Framework;
using Unity.Hierarchy;
using UnityEngine;
using System.Collections.Generic;

public class Node : MonoBehaviour
{

    public Vector3 position;
    public float goalDist;
    public float cost;
    public bool visited;

    public Node right;
    public Node left;
    public Node forward;
    public Node back;
    public Node up;
    public Node down;
    public float gh;
    public Material mat;
    public MeshRenderer meshRenderer;
    public Node pathPar;
}
