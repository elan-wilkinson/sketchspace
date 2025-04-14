using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.IO;
using FF = Unity.Sentis.Functional;
using System.Collections;
using static UnityEngine.EventSystems.EventTrigger;


public class RunYOLO : MonoBehaviour
{
    [SerializeField] GridGenerator gridGen;
    public GameObject plane;
    public GameObject modelPlaceholder;
    public Transform boxPositionPar;
    public GameObject ArucoObj;
    public Camera arCam;
    public GameObject[] models;
    List<int> modIdxs = new List<int>();
    List<GameObject> instantiatedModels = new List<GameObject>();

    Transform startPoint;
    Transform endPoint;

    public Material openCVwebCamMat;
    [Tooltip("Drag a YOLO model .onnx file here")]
    public ModelAsset modelAsset;

    [Tooltip("Drag the classes.txt here")]
    public TextAsset classesAsset;

    [Tooltip("Create a Raw Image in the scene and link it here")]
    public RawImage displayImage;

    [Tooltip("Drag a border box texture here")]

    public Sprite borderTexture;

    [Tooltip("Select an appropriate font for the labels")]
    public Font font;

    [Tooltip("Change this to the name of the video you put in the Assets/StreamingAssets folder")]

    const BackendType backend = BackendType.GPUCompute;

    private Transform displayLocation;
    private Worker worker;
    private string[] labels;
    private RenderTexture targetRT;
    private Sprite borderSprite;

    private const int imageWidth = 640;
    private const int imageHeight = 640;

    [SerializeField]
    WebCamTexture webcamTexture;

    private VideoPlayer video;

    List<GameObject> boxPool = new();

    [Tooltip("Intersection over union threshold used for non-maximum suppression")]
    [SerializeField, Range(0, 1)] float iouThreshold = 0.5f;

    [Tooltip("Confidence score threshold used for non-maximum suppression")]
    [SerializeField, Range(0, 1)] float scoreThreshold = 0.5f;

    Tensor<float> centersToCorners;

    public struct BoundingBox
    {
        public float centerX;
        public float centerY;
        public float width;
        public float height;
        public string label;
    }

    IEnumerator SetMat()
    { 
        yield return new WaitForSeconds(1.0f);
        openCVwebCamMat = ArucoObj.GetComponent<MeshRenderer>().material;
    }
    void Start()
    {
        StartCoroutine(SetMat());
        
        LoadModel();
        labels = classesAsset.text.Split('\n');
       
    }
    void LoadModel()
    {

        var model1 = ModelLoader.Load(modelAsset);

        centersToCorners = new Tensor<float>(new TensorShape(4, 4),
        new float[]
        {
                    1,      0,      1,      0,
                    0,      1,      0,      1,
                    -0.5f,  0,      0.5f,   0,
                    0,      -0.5f,  0,      0.5f
        });

        var graph = new FunctionalGraph();
        var inputs = graph.AddInputs(model1);
        var modelOutput = FF.Forward(model1, inputs)[0];                        
        var boxCoords = modelOutput[0, 0..4, ..].Transpose(0, 1);               
        var allScores = modelOutput[0, 4.., ..];                                
        var scores = FF.ReduceMax(allScores, 0);                                
        var classIDs = FF.ArgMax(allScores, 0);                                 
        var boxCorners = FF.MatMul(boxCoords, FF.Constant(centersToCorners));   
        var indices = FF.NMS(boxCorners, scores, iouThreshold, scoreThreshold); 
        var coords = FF.IndexSelect(boxCoords, 0, indices);                     
        var labelIDs = FF.IndexSelect(classIDs, 0, indices);                    

        worker = new Worker(graph.Compile(coords, labelIDs), backend);
    }

    void SetupInput()
    {
        video = gameObject.AddComponent<VideoPlayer>();
        video.renderMode = VideoRenderMode.APIOnly;
        video.source = VideoSource.Url;
        video.isLooping = true;
        video.Play();
    }

    private void Update()
    {
        ExecuteML();


    }

    Tensor<float> modeloutput;

    List<Vector2> centers = new List<Vector2>();
    public void ExecuteML()
    {
        centers.Clear();
        modIdxs.Clear();
        ClearAnnotations();

        using Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, 3, imageHeight, imageWidth));
        TextureConverter.ToTensor(openCVwebCamMat.mainTexture, inputTensor, default);
        worker.Schedule(inputTensor);

        using var output = (worker.PeekOutput("output_0") as Tensor<float>).ReadbackAndClone();
        using var labelIDs = (worker.PeekOutput("output_1") as Tensor<int>).ReadbackAndClone();

        float displayWidth = 640;
        float displayHeight = 640;

        float scaleX = 1;
        float scaleY = 1;

        int boxesFound = output.shape[0];

        for (int n = 0; n < Mathf.Min(boxesFound, 200); n++)
        {

            var box = new BoundingBox
            {
                centerX = output[n, 0],
                centerY = output[n, 1],
                width = output[n, 2],
                height = output[n, 3],
                label = labels[labelIDs[n]],

            };
            centers.Add(new Vector2(output[n, 0], output[n, 1]));
            modIdxs.Add(labelIDs[n]);

            DrawBox(box, n, displayHeight * 0.05f, labels[labelIDs[n]]);
        }
    }

    public void DrawBox(BoundingBox box, int id, float fontSize, string boxlabel = "box")
    {

        GameObject panel;
        if (id < boxPool.Count)
        {
            panel = boxPool[id];
            panel.SetActive(true);
        }
        else
        {
            panel = CreateNewBox(Color.yellow);
        }

        panel.transform.position =  boxPositionPar.TransformPoint(new Vector3(box.centerX/640, box.centerY/640));
        panel.name = boxlabel;

        panel.transform.localScale = new Vector3(box.width/320, box.height / 320, 0.1f);

        var label = panel.GetComponentInChildren<Text>();
        label.text = box.label;
        label.fontSize = (int)fontSize;
    }


    public GameObject CreateNewBox(Color color)
    {

        var panel = new GameObject("ObjectBox");
        panel.layer = LayerMask.NameToLayer("UI");

        SpriteRenderer spriteRend = panel.AddComponent<SpriteRenderer>();
        spriteRend.color = color;

        spriteRend.sprite = borderTexture;
        
        panel.transform.SetParent(boxPositionPar);

        var text = new GameObject("ObjectLabel");
        text.AddComponent<CanvasRenderer>();
        text.transform.SetParent(panel.transform, false);
        Text txt = text.AddComponent<Text>();
        txt.font = font;
        txt.color = color;
        txt.fontSize = 40;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;

        RectTransform rt2 = text.GetComponent<RectTransform>();
        rt2.offsetMin = new Vector2(20, rt2.offsetMin.y);
        rt2.offsetMax = new Vector2(0, rt2.offsetMax.y);
        rt2.offsetMin = new Vector2(rt2.offsetMin.x, 0);
        rt2.offsetMax = new Vector2(rt2.offsetMax.x, 30);
        rt2.anchorMin = new Vector2(0, 0);
        rt2.anchorMax = new Vector2(1, 1);

        boxPool.Add(panel);
        return panel;
    }

    public void ClearAnnotations()
    {
        foreach (var box in boxPool)
        {
            box.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        centersToCorners?.Dispose();
        worker?.Dispose();
    }

    public void PlaceModels()
    {

        for (int n = 0; n < centers.Count; n++)
        {

            float centerX = centers[n].x;
            float centerY = centers[n].y;
            GameObject mod  = Instantiate(models[modIdxs[n]], plane.transform);
            instantiatedModels.Add(mod);

            if (mod.gameObject.name.ToLower() == "helicopter(clone)")
            {
                startPoint = mod.transform;
            }
            else if (mod.gameObject.name.ToLower() == "hospital(clone)")
            {
                endPoint = mod.transform;
            }
            
            Ray ray = arCam.ScreenPointToRay(new Vector2(centerX, 640-centerY));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, Mathf.Infinity))
            {
                Debug.Log(hit.collider.gameObject.name + " was hit");
                mod.transform.position = hit.point;
            }
            mod.transform.rotation = plane.transform.rotation;
            float scale = 0.1f;
            mod.transform.localScale = new Vector3(scale, scale, scale);
            mod.layer = LayerMask.NameToLayer("UI");
        }
        GenerateGrid();
    }
    public void GenerateGrid()
    {
        float height = 0.25f;
        Vector3 bottomtopLeft = PaperPlaneSurfacePoint((new Vector2(0, 640)));
        Vector3 bottomtopRight = PaperPlaneSurfacePoint((new Vector2(640, 640)));
        Vector3 bottombottomRight = PaperPlaneSurfacePoint((new Vector2(640, 0)));
        Vector3 bottombottomLef = PaperPlaneSurfacePoint((new Vector2(0, 0)));
        Vector3 upperTopLef = PaperPlaneSurfacePoint((new Vector2(0, 640))) + plane.transform.up * height;
        Vector3 upperTopRight = PaperPlaneSurfacePoint((new Vector2(640, 640))) + plane.transform.up * height;
        Vector3 upperbottomRight = PaperPlaneSurfacePoint((new Vector2(640, 0))) + plane.transform.up * height;
        Vector3 upperbottomLef = PaperPlaneSurfacePoint((new Vector2(0, 0))) + plane.transform.up * height;
        gridGen.CreateGrid(
            bottomtopLeft,
            bottomtopRight,
            bottombottomRight,
            bottombottomLef,
            upperTopLef,
            upperTopRight,
            upperbottomRight,
            upperbottomLef,
            plane
            );
    }

    Vector3 PaperPlaneSurfacePoint(Vector2 screenPoint)
    {
        Ray ray = arCam.ScreenPointToRay(screenPoint);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            return hit.point;
        }
        return Vector3.zero;
    }

    public void Pathfind()
    {
        plane.GetComponent<BoxCollider>().enabled = false;
        foreach (GameObject obj in instantiatedModels)
        {
            obj.GetComponent<BoxCollider>().enabled = true;
        }
        Node[] nodes = this.GetComponent<GridGenerator>().allGendNodes.ToArray();
        float hospDist = 9999.0f;
        float heliDist = 9999.0f;
        Node start = null;
        Node goal = null;
        for (int i = 0; i < nodes.Length; i++)
        {
            Collider[] cols = Physics.OverlapSphere(nodes[i].gameObject.transform.position, 0.1f);
            if (cols.Length > 0)
            {
                foreach (Collider col in cols)
                {
                    string hitObj = col.gameObject.name.ToLower();
                    if ((hitObj == "helicopter(clone)") || (hitObj == "hospital(clone)"))
                    {
                        if (Vector3.Distance(nodes[i].transform.position, startPoint.position) <= heliDist)
                        {

                            start = nodes[i];
                            heliDist = Vector3.Distance(nodes[i].transform.position, startPoint.position);
                        }
                        else if (Vector3.Distance(nodes[i].transform.position, endPoint.position) <= hospDist)
                        {
                            goal = nodes[i];
                            hospDist = Vector3.Distance(nodes[i].transform.position, endPoint.position);
                        }
                        break;
                    }
                    else
                    {
                        DestroyImmediate(nodes[i]);
                        break;
                    }
                }
               
            }

        }
        A_star astar = this.GetComponent<A_star>();
        astar.start = start;
        astar.end = goal;

        astar.FindPath();
        astar.VisualizePath(plane.transform, plane.GetComponent<LineRenderer>());

    }

}
