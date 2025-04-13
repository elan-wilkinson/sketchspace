using NUnit.Framework;
using System.Xml;
using UnityEngine;
using System.Collections.Generic;

public class GridGenerator : MonoBehaviour
{
    public Camera cam;
    public float nodeSize;
    public float cameraDefaultDist;
    public float height;
    public List<Node> allGendNodes = new List<Node>();

    void Start()
    {
        
    }


    public void CreateGrid(
        Vector3 bottomtopLeft,
        Vector3 bottomtopRight,
        Vector3 bottombottomRight,
        Vector3 bottombottomLef,
        Vector3 upperTopLef,
        Vector3 upperTopRight,
        Vector3 upperbottomRight,
        Vector3 upperbottomLef,
        GameObject par
      )
    {
        Transform newPar = par.transform;
        /*Vector3 bottomtopLeft = cam.ScreenToWorldPoint(new Vector3(0,Screen.height,cameraDefaultDist));
        Vector3 bottomtopRight = cam.ScreenToWorldPoint(new Vector3(Screen.width,Screen.height,cameraDefaultDist));
        Vector3 bottombottomRight = cam.ScreenToWorldPoint(new Vector3(Screen.width,0,cameraDefaultDist));
        Vector3 bottombottomLef = cam.ScreenToWorldPoint(new Vector3(0,0.0f,cameraDefaultDist));
        Vector3 upperTopLef = cam.ScreenToWorldPoint(new Vector3(0, Screen.height, cameraDefaultDist - height));
        Vector3 upperTopRight = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, cameraDefaultDist - height));
        Vector3 upperbottomRight = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0, cameraDefaultDist-height));
        Vector3 upperbottomLef = cam.ScreenToWorldPoint(new Vector3(0, 0.0f, cameraDefaultDist - height));
        */




        int nodesPerRow = (int)(Vector3.Distance(bottomtopLeft, bottomtopRight) / nodeSize);
        int nodesPerCol = (int)(Vector3.Distance(bottombottomLef, bottombottomRight) / nodeSize);
        int nodesPerLayer = (int)((height) / nodeSize);

        Debug.Log($"nodes Pr Row {nodesPerRow} nodes per col {nodesPerCol} nodes per layer {nodesPerLayer}");

        Node[,,] nodeGraph = new Node[nodesPerLayer, nodesPerRow, nodesPerCol];

        for (int L = 0; L < nodesPerLayer; L++)
        {

            for (int i = 0; i < nodesPerRow; i++)
            {
                for (int k = 0; k < nodesPerCol; k++)
                {
                    GameObject newNode = new GameObject();//GameObject.CreatePrimitive(PrimitiveType.Sphere);

                    newNode.transform.parent = newPar;
                    newNode.transform.localScale *= 0.1f;
                    Node n = newNode.AddComponent<Node>();
                    allGendNodes.Add(n);
                    if (k != 0)
                    {
                        n.left = nodeGraph[L, i, k - 1];
                        n.left.right = n;
                    }
                    if (i != 0)
                    {
                        n.back = nodeGraph[L, i - 1, k];
                        n.back.forward = n;
                    }
                    if (L != 0)
                    {
                        n.up = nodeGraph[L - 1, i, k];
                        n.up.down = n;
                    }

                            nodeGraph[L, i, k] = n;
                            newNode.transform.position = Vector3.zero;
                            n.meshRenderer = newNode.GetComponent<MeshRenderer>();

                            //float rowLerp = (i == 0) ? 0 : (i == nodesPerRow - 1) ? 1 : i / nodesPerRow;
                            //float colLerp = (k == 0) ? 0 : (k == nodesPerCol - 1) ? 1 : k / nodesPerCol;

                            float rowLerp = ((float)i / nodesPerRow) + (nodeSize / 4.0f / nodesPerRow);
                            float colLerp = ((float)k / nodesPerCol) + (nodeSize / 4.0f / nodesPerCol);
                            float heightLerp = ((float)L / nodesPerLayer) + (nodeSize / 4.0f / nodesPerLayer);


                            Vector3 lowertopRowPoint = Vector3.Lerp(bottomtopLeft, bottomtopRight, rowLerp);
                            Vector3 lowerbottomRowPoint = Vector3.Lerp(bottombottomLef, bottombottomRight, rowLerp);
                            Vector3 lowercolPos = Vector3.Lerp(lowertopRowPoint, lowerbottomRowPoint, colLerp);

                            Vector3 uppertopRowPoint = Vector3.Lerp(upperTopLef, upperTopRight, rowLerp);
                            Vector3 upperbottomRowPoint = Vector3.Lerp(upperbottomLef, upperbottomRight, rowLerp);
                            Vector3 uppercolPos = Vector3.Lerp(uppertopRowPoint, upperbottomRowPoint, colLerp);

                            newNode.transform.position = Vector3.Lerp(lowercolPos, uppercolPos, heightLerp);
                            newNode.gameObject.name = $"{L} {i} {k}";


                        }
                    
                }
            }
        }
    }
    

