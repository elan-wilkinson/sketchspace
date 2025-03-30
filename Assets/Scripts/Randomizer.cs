using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

public class Randomizer : MonoBehaviour
{
    public Sprite[] backgrounds;
    public GameObject backgroundObj;
    public SpriteRenderer backgroundSR;
    public Camera captureCamera;
    int imageSize = 640;
    public string saveFolder = "C:\\Users\\elanw\\Files\\USD\\Capstone\\FinalProject\\SynthData";
    public string csvFilePath = "C:\\Users\\elanw\\Files\\USD\\Capstone\\FinalProject\\SynthData\\labels.csv";

    public GameObject[] doods;
    public SpriteRenderer[] doodsSR;

    public int imgCount = 10000;
    int cur = 0;

    float minX = -4.3f;
    float maxX = 4.3f;
    float minY = -3.4f;
    float maxY = 5.1f;
    float minScale = 0.65f;
    float maxScale = 1.34f;
    int numDoods = 1;
    public int imageCount = 10;

    int numOfDoods = 1;

    Sprite[] campiresprites;
    Sprite[] cloudsprites;
    Sprite[] firetrucksprites;
    Sprite[] helicoptersprites;
    Sprite[] hospitalsprites;
    Sprite[] mountainsprites;
    Sprite[] skullsprites;
    Sprite[] skyscrapersprites;
    Sprite[] tractorsprites;
    Sprite[] trafficlightsprites;
    Sprite[] treesprites;
    Sprite[] vansprites;



    private string[] classFolders = new string[] {
        "campfire","cloud","firetruck","helicopter","hospital","mountain","skull","skyscraper","tractor","traffic light","tree","van"
    };
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        campiresprites = Resources.LoadAll<Sprite>("doodle/campfire");
        cloudsprites = Resources.LoadAll<Sprite>("doodle/cloud");
        firetrucksprites = Resources.LoadAll<Sprite>("doodle/firetruck");
        helicoptersprites = Resources.LoadAll<Sprite>("doodle/helicopter");
        hospitalsprites = Resources.LoadAll<Sprite>("doodle/hospital");
        mountainsprites = Resources.LoadAll<Sprite>("doodle/mountain");
        skullsprites = Resources.LoadAll<Sprite>("doodle/skull");
        skyscrapersprites = Resources.LoadAll<Sprite>("doodle/skyscraper");
        tractorsprites = Resources.LoadAll<Sprite>("doodle/tractor");
        trafficlightsprites = Resources.LoadAll<Sprite>("doodle/traffic light");
        treesprites = Resources.LoadAll<Sprite>("doodle/tree");
        vansprites = Resources.LoadAll<Sprite>("doodle/van");
    }

    void PickRandImg(SpriteRenderer sr, int objClass = -1)
    {
        if (objClass == -1)
            objClass = Random.Range(2, 11);
        int idx = Random.Range(0, 2195);
        sr.gameObject.name = objClass.ToString();
        switch (objClass)
        {
            case 0:
                sr.sprite = helicoptersprites[idx];
                break;
            case 1:
                sr.sprite = hospitalsprites[idx];
                break;
            case 2:
                sr.sprite = mountainsprites[idx];
                break;
            case 3:
                sr.sprite = skullsprites[idx];
                break;
            case 4:
                print(skyscrapersprites.Length);
                sr.sprite = skyscrapersprites[idx];
                break;
            case 5:
                sr.sprite = tractorsprites[idx];
                break;
            case 6:
                sr.sprite = trafficlightsprites[idx];
                break;
            case 7:
                sr.sprite = vansprites[idx];
                break;
            case 8:
                sr.sprite = treesprites[idx];
                break;
            case 9:
                sr.sprite = cloudsprites[idx];
                break;
            case 10:
                sr.sprite = firetrucksprites[idx];
                break;
            case 11:
                sr.sprite = campiresprites[idx];
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        cur++;
        if (cur == imgCount )
        {
            Debug.Break();
        }
        Doodle();
    }

    void Doodle()
    {
        backgroundObj.transform.localEulerAngles = new Vector3(0, 0, Random.Range(0, 360.0f));
        int backGround = Random.Range(0, backgrounds.Length);
        backgroundSR.sprite = backgrounds[backGround];
        numDoods = Random.Range(2, doods.Length);

        for (int i = 0; i < doods.Length; i++)
        {
            if (i >= numDoods)
            {
                doods[i].SetActive(false);
                continue;
            }
            
            doods[i].gameObject.SetActive(true);
            if (i == 0)
                PickRandImg(doodsSR[i],0);
            else if (i == 1)
                PickRandImg(doodsSR[i],1);
            else
                PickRandImg(doodsSR[i]);
            doods[i].transform.localPosition = new Vector3(Random.Range(minX, maxX), Random.Range(minY, maxY), 0);
            float scale = Random.Range(minScale, maxScale);
            doods[i].transform.localScale = new Vector3(scale, scale, scale);



        }
        RemoveOverlappingDoodles();
        CaptureImageAndLabels();
    }

    void RemoveOverlappingDoodles()
    {
        // Make sure all transforms and colliders are up-to-date
        Physics2D.SyncTransforms();

        for (int i = 0; i < numDoods; i++)
        {
            if (doods[i] == null) continue;

            var boundsA = doods[i].GetComponent<Collider2D>().bounds;
            boundsA.center = doods[i].transform.position;


            for (int j = i + 1; j < numDoods; j++)
            {
                if (doods[j] == null) continue;

                var boundsB = doods[j].GetComponent<Collider2D>().bounds;
                boundsB.center = doods[j].transform.position;

                if (boundsA.Intersects(boundsB))
                {
                    doods[j].SetActive(false);
                }
            }
        }
    }
    void CaptureImageAndLabels()
    {
        // Sync transforms for accurate bounds
        Physics2D.SyncTransforms();

        string imageName = $"image_{imageCount:D4}.png";
        string imagePath = Path.Combine(saveFolder, imageName);

        StringBuilder labelBuilder = new StringBuilder();

        foreach (GameObject dood in doods)
        {
            if (dood == null || !dood.activeInHierarchy)
                continue;

            Collider2D col = dood.GetComponent<Collider2D>();
            if (col == null)
                continue;

            Bounds bounds = col.bounds;

            // Convert to viewport points (0-1)
            Vector3 min = captureCamera.WorldToViewportPoint(bounds.min);
            Vector3 max = captureCamera.WorldToViewportPoint(bounds.max);

            float x_center = (min.x + max.x) / 2f;
            float y_center = (min.y + max.y) / 2f;
            float width = Mathf.Abs(max.x - min.x);
            float height = Mathf.Abs(max.y - min.y);

            // Make sure the box is valid
            if (width <= 0 || height <= 0)
                continue;

            int classId = int.Parse(dood.name); // Replace with your label logic

            // YOLO format: class_id x_center y_center width height (all normalized)
            labelBuilder.AppendFormat("{0} {1:F6} {2:F6} {3:F6} {4:F6},",
                classId, x_center, y_center, width, height);
        }

        // Take screenshot and save to file
        StartCoroutine(SaveScreenshotAndCSV(imagePath, imageName, labelBuilder.ToString().TrimEnd(',')));

        imageCount++;
    }

    System.Collections.IEnumerator SaveScreenshotAndCSV(string imagePath, string imageName, string labels)
    {
        // Wait for end of frame so the frame renders
        yield return new WaitForEndOfFrame();

        RenderTexture rt = new RenderTexture(imageSize, imageSize, 24);
        captureCamera.targetTexture = rt;
        Texture2D screen = new Texture2D(imageSize, imageSize, TextureFormat.RGB24, false);
        captureCamera.Render();
        RenderTexture.active = rt;
        screen.ReadPixels(new Rect(0, 0, imageSize, imageSize), 0, 0);
        screen.Apply();

        byte[] bytes = screen.EncodeToPNG();
        File.WriteAllBytes(imagePath, bytes);

        // Clean up
        captureCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);
        Destroy(screen);

        // Save to CSV
        File.AppendAllText(csvFilePath, $"{imageName},{labels}\n");
    }
}
