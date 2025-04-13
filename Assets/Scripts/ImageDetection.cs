using UnityEngine;
using Unity.Sentis;
using Unity.XR.CoreUtils;

public class ImageDetection : MonoBehaviour
{
    [SerializeField]
    ModelAsset yoloModelAsset;
    Model runTimeYOLO;

    [SerializeField] Texture2D testTexture;
    Worker worker;
    public float conf;
    void Start()
    {
        runTimeYOLO = ModelLoader.Load(yoloModelAsset);
        worker = new Worker(runTimeYOLO, BackendType.GPUCompute);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            GetInference();
        }
    }

    void GetInference()
    {
        //Tensor<float> inputTensor = TextureConverter.ToTensor(testTexture);

        //TensorFloat inputTensor = TextureUtils.ToTensor(testTexture, channels: TextureChannel.RedGreenBlue);
        Tensor<float> inputTensor = TextureConverter.ToTensor(testTexture, new TextureTransform().SetDimensions(width: 640, height: 640));
        
        worker.Schedule(inputTensor);

        Tensor<float> outputTensor = worker.PeekOutput() as Tensor<float>;
        Debug.Log(outputTensor.ToString());
        //Debug.Log(outputTensor.ToString());
        var output = outputTensor.ReadbackAndClone();
        // See async examples for non-blocking readback.
        Debug.Log(output.ToString());

// optional: clone it if needed
        int numPreds = 8400;
        int numAttrs = 17; // 4 bbox + 1 obj + 12 classes

        for (int i = 0; i < numPreds; i++)
        {
            float x = output[0, 0 + i * numAttrs];
            float y = output[0, 1 + i * numAttrs];
            float w = output[0, 2 + i * numAttrs];
            float h = output[0, 3 + i * numAttrs];
            float objConf = output[0, 4 + i * numAttrs];

            float maxClassConf = -1f;
            int classId = -1;

            float classConf = 0.0f;
            for (int j = 0; j < 12; j++) // Assuming 12 classes
            {
                classConf = output[0, 5 + j + i * numAttrs];
                if (classConf > maxClassConf)
                {
                    maxClassConf = classConf;
                    classId = j;
                }
            }
            Debug.Log("classConf: " + classConf + "objConf: " + objConf);
            float confidence = objConf * maxClassConf;
            if (confidence > conf) // adjust threshold
            {
                // Convert to bounding box (optionally denormalize)
                Debug.Log($"Detected class {classId} with confidence {confidence} at (x: {x}, y: {y}, w: {w}, h: {h})");
            }
        }

        output.Dispose();
    }

    void OnDisable()
    {
        // Clean up Sentis resources.
        worker.Dispose();
        //m_Input.Dispose();
    }
}
