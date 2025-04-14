using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Collections;
using System.Runtime.CompilerServices;

public class A_star : MonoBehaviour
{
    public Node start;
    public Node end;

    void CalHAll()
    {
        Node[] nodes = GameObject.FindObjectsByType<Node>(FindObjectsSortMode.None);
        foreach (Node node in nodes)
        {         
            node.goalDist = Mathf.Pow(Vector3.Distance(node.transform.position, end.transform.position),2.0f);
        }

    }

    public IEnumerator FindTheWay()
    {

        Dictionary<Node, float> visited = new();
        Dictionary<Node, float> notVisited = new();
        CalHAll();
        Node goal = null;

        start.visited = true;
        visited.Add(start, start.goalDist);
        List<Node> snewNeighbors = ReturnNeighbors(start);
        start.visited = true;
        start.cost = 0;

        foreach (Node node in snewNeighbors)
        {
            node.cost = start.cost + 1.0f;
            node.gh = node.cost + node.goalDist;
            node.pathPar = start;
            notVisited.Add(node, node.gh);
        }
        yield return new WaitForSeconds(0.1f);

        while (!goal)
        {
            Node bestOption = Lowest(notVisited);
            bestOption.visited = true;
            visited.Add(bestOption, bestOption.gh);
            notVisited.Remove(bestOption);

            List<Node> newNeighbors = ReturnNeighbors(bestOption);
            yield return new WaitForSeconds(0.1f);

            foreach (Node node in newNeighbors)
            {
                if (node == end)
                {
                    goal = node;
                    node.pathPar = bestOption;
                    StopAllCoroutines();
                }
                node.cost = bestOption.cost + 1;
                node.gh = node.cost + node.goalDist;
                if (!notVisited.ContainsKey(node) && !node.visited)
                {
                    notVisited.Add(node, node.gh);
                    node.pathPar = bestOption;
                }
                if (notVisited.ContainsKey(node))
                {
                    if (node.gh < notVisited[node])
                    {
                        notVisited[node] = node.gh;
                        node.pathPar = bestOption;
                    }
                    else
                        node.gh = notVisited[node];
                }
                if (node.visited && node.gh < visited[node])
                {
                    visited[node] = node.gh;
                    node.pathPar = bestOption;
                }         
            }
            yield return null;
        }
    }

    public void FindPath()
    {
        Dictionary<Node, float> visited = new();
        Dictionary<Node, float> notVisited = new();
        CalHAll();
        Node goal = null;

        start.visited = true;
        visited.Add(start, start.goalDist);
        List<Node> snewNeighbors = ReturnNeighbors(start);
        start.visited = true;
        start.cost = 0;

        foreach (Node node in snewNeighbors)
        {
            node.cost = start.cost + 1.0f;
            node.gh = node.cost + node.goalDist;
            node.pathPar = start;
            notVisited.Add(node, node.gh);
        }

        while (!goal)
        {

            Node bestOption = Lowest(notVisited);
            bestOption.visited = true;
            visited.Add(bestOption, bestOption.gh);
            notVisited.Remove(bestOption);

            List<Node> newNeighbors = ReturnNeighbors(bestOption);

            foreach (Node node in newNeighbors)
            {
                if (node == end)
                {
                    goal = node;
                    node.pathPar = bestOption;
                    StopAllCoroutines();
                }
                node.cost = bestOption.cost + 1;
                node.gh = node.cost + node.goalDist;
                if (!notVisited.ContainsKey(node) && !node.visited)
                {
                    notVisited.Add(node, node.gh);
                    node.pathPar = bestOption;
                }
                if (notVisited.ContainsKey(node))
                {
                    if (node.gh < notVisited[node])
                    {
                        notVisited[node] = node.gh;
                        node.pathPar = bestOption;
                    }
                    else
                        node.gh = notVisited[node];
                }
                if (node.visited && node.gh < visited[node])
                {
                    visited[node] = node.gh;
                    node.pathPar = bestOption;
                }
            }
        }
    }

    public void VisualizePath(Transform plane, LineRenderer lr)
    {
        Stack<Node> bestPathNodes = new();
        Node par = end.pathPar;
        bestPathNodes.Push(par);
        while (par != null)
        {

            par = par.pathPar;
            if (par != null)
                bestPathNodes.Push(par);
        }
        lr.positionCount = bestPathNodes.Count;
        int i = 0;

        while (bestPathNodes.Count > 0)
        {

            Node node = bestPathNodes.Pop();
            lr.SetPosition(i, plane.InverseTransformPoint(node.transform.position));
            i++;
        }
    }

        List<Node> ReturnNeighbors(Node start)
        {
            List<Node> neighbors = new();
            if (start.forward != null)
                neighbors.Add(start.forward);
            if (start.right != null)
                neighbors.Add(start.right);
            if (start.left != null)
                neighbors.Add(start.left);
            if (start.back != null)
                neighbors.Add(start.back);
        if (start.up != null)
            neighbors.Add(start.up);
        if (start.down != null)
            neighbors.Add(start.down);
        return neighbors;
        }

        static Node Lowest(Dictionary<Node, float> dict)
        {
            float lowest = 99999.0f;
            Node lowestNode = null;
            foreach (var node in dict.Keys)
            {
                if (dict[node] < lowest)
                {
                    lowestNode = node;
                    lowest = dict[node];
                }
            }
            return lowestNode;
        }
    }



