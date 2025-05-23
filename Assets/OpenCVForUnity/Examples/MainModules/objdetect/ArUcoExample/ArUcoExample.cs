using OpenCVForUnity.Calib3dModule;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.ImgprocModule;
using OpenCVForUnity.ObjdetectModule;
using OpenCVForUnity.UnityUtils;
using OpenCVForUnity.UnityUtils.Helper;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OpenCVForUnityExample
{
    /// <summary>
    /// ArUco Example
    /// An example of marker-based AR view and camera pose estimation using the objdetect and aruco module.
    /// Referring to https://github.com/opencv/opencv_contrib/blob/4.x/modules/aruco/samples/detect_markers.cpp
    /// http://docs.opencv.org/3.1.0/d5/dae/tutorial_aruco_detection.html
    /// https://github.com/opencv/opencv/blob/4.x/modules/objdetect/test/test_arucodetection.cpp
    /// https://github.com/opencv/opencv/blob/4.x/modules/objdetect/test/test_boarddetection.cpp
    /// https://github.com/opencv/opencv/blob/4.x/modules/objdetect/test/test_charucodetection.cpp
    /// </summary>
    [RequireComponent(typeof(MultiSource2MatHelper))]
    public class ArUcoExample : MonoBehaviour
    {

        /// <summary>
        /// The marker type.
        /// </summary>
        public MarkerType markerType = MarkerType.CanonicalMarker;

        /// <summary>
        /// The marker type dropdown.
        /// </summary>
        public Dropdown markerTypeDropdown;

        /// <summary>
        /// The dictionary identifier.
        /// </summary>
        public ArUcoDictionary dictionaryId = ArUcoDictionary.DICT_6X6_250;

        /// <summary>
        /// The dictionary id dropdown.
        /// </summary>
        public Dropdown dictionaryIdDropdown;

        /// <summary>
        /// Determines if restores the camera parameters when the file exists.
        /// </summary>
        public bool useStoredCameraParameters = false;

        /// <summary>
        /// The toggle for switching to use the stored camera parameters.
        /// </summary>
        public Toggle useStoredCameraParametersToggle;

        /// <summary>
        /// Determines if shows rejected corners.
        /// </summary>
        public bool showRejectedCorners = false;

        /// <summary>
        /// The shows rejected corners toggle.
        /// </summary>
        public Toggle showRejectedCornersToggle;

        /// <summary>
        /// Determines if applied the pose estimation.
        /// </summary>
        public bool applyEstimationPose = true;

        /// <summary>
        /// Determines if refine marker detection. (only valid for ArUco boards)
        /// </summary>
        public bool refineMarkerDetection = true;

        /// <summary>
        /// The shows refine marker detection toggle.
        /// </summary>
        public Toggle refineMarkerDetectionToggle;

        [Space(10)]

        /// <summary>
        /// The length of the markers' side. Normally, unit is meters.
        /// </summary>
        public float markerLength = 0.1f;

        /// <summary>
        /// The enable low pass filter toggle.
        /// </summary>
        public Toggle enableLowPassFilterToggle;

        /// <summary>
        /// ARHelper
        /// </summary>
        public ARHelper arHelper;

        /// <summary>
        /// The texture.
        /// </summary>
        Texture2D texture;

        /// <summary>
        /// The multi source to mat helper.
        /// </summary>
        MultiSource2MatHelper multiSource2MatHelper;

        /// <summary>
        /// The rgb mat.
        /// </summary>
        Mat rgbMat;

        /// <summary>
        /// The undistorted rgb mat.
        /// </summary>
        Mat undistortedRgbMat;

        /// <summary>
        /// The cameraparam matrix.
        /// </summary>
        Mat camMatrix;

        /// <summary>
        /// The distortion coeffs.
        /// </summary>
        MatOfDouble distCoeffs;

        /// <summary>
        /// The transformation matrix for AR.
        /// </summary>
        Matrix4x4 ARM;

        // for CanonicalMarker.
        Mat ids;
        List<Mat> corners;
        List<Mat> rejectedCorners;
        Mat rotMat;
        Dictionary dictionary;
        Mat recoveredIdxs;
        ArucoDetector arucoDetector;

        // for GridBoard.
        // number of markers in X direction
        const int gridBoradMarkersX = 5;
        // number of markers in Y direction
        const int gridBoradMarkersY = 7;
        // marker side length (normally in meters)
        const float gridBoradMarkerLength = 0.04f;
        // separation between two markers (same unit as markerLength)
        const float gridBoradMarkerSeparation = 0.01f;
        GridBoard gridBoard;

        // for ChArUcoBoard.
        //  number of chessboard squares in X direction
        const int chArUcoBoradSquaresX = 5;
        //  number of chessboard squares in Y direction
        const int chArUcoBoradSquaresY = 7;
        // chessboard square side length (normally in meters)
        const float chArUcoBoradSquareLength = 0.04f;
        // marker side length (same unit than squareLength)
        const float chArUcoBoradMarkerLength = 0.02f;
        const int charucoMinMarkers = 2;
        Mat charucoCorners;
        Mat charucoIds;
        CharucoBoard charucoBoard;
        CharucoDetector charucoDetector;

        // for ChArUcoDiamondMarker.
        // size of the diamond squares in pixels
        const float diamondSquareLength = 0.1f;
        // size of the markers in pixels.
        const float diamondMarkerLength = 0.06f;
        List<Mat> diamondCorners;
        Mat diamondIds;
        CharucoBoard charucoDiamondBoard;
        CharucoDetector charucoDiamondDetector;


        // Use this for initialization
        void Start()
        {
            //if true, The error log of the Native side OpenCV will be displayed on the Unity Editor Console.
            Utils.setDebugMode(true);


            markerTypeDropdown.value = (int)markerType;
            dictionaryIdDropdown.value = (int)dictionaryId;
            useStoredCameraParametersToggle.isOn = useStoredCameraParameters;
            showRejectedCornersToggle.isOn = showRejectedCorners;
            refineMarkerDetectionToggle.isOn = refineMarkerDetection;
            refineMarkerDetectionToggle.interactable = (markerType == MarkerType.GridBoard || markerType == MarkerType.ChArUcoBoard);
            enableLowPassFilterToggle.isOn = arHelper.useLowPassFilter;

            multiSource2MatHelper = gameObject.GetComponent<MultiSource2MatHelper>();
            multiSource2MatHelper.outputColorFormat = Source2MatHelperColorFormat.RGBA;
            multiSource2MatHelper.Initialize();
        }

        /// <summary>
        /// Raises the source to mat helper initialized event.
        /// </summary>
        public void OnSourceToMatHelperInitialized()
        {
            Debug.Log("OnSourceToMatHelperInitialized");

            Mat rgbaMat = multiSource2MatHelper.GetMat();

            texture = new Texture2D(rgbaMat.cols(), rgbaMat.rows(), TextureFormat.RGBA32, false);
            Utils.matToTexture2D(rgbaMat, texture);

            // Set the Texture2D as the main texture of the Renderer component attached to the game object
            gameObject.GetComponent<Renderer>().material.mainTexture = texture;

            // Adjust the scale of the game object to match the dimensions of the texture
            gameObject.transform.localScale = new Vector3(rgbaMat.cols(), rgbaMat.rows(), 1);
            gameObject.transform.localScale = new Vector3(640, 640, 0);
            Debug.Log("Screen.width " + Screen.width + " Screen.height " + Screen.height + " Screen.orientation " + Screen.orientation);

            // Adjust the orthographic size of the main Camera to fit the aspect ratio of the image
            float width = rgbaMat.width();
            float height = rgbaMat.height();
            float imageSizeScale = 1.0f;
            float widthScale = (float)Screen.width / width;
            float heightScale = (float)Screen.height / height;
            if (widthScale < heightScale)
            {
                Camera.main.orthographicSize = (width * (float)Screen.height / (float)Screen.width) / 2;
                imageSizeScale = (float)Screen.height / (float)Screen.width;
            }
            else
            {
                Camera.main.orthographicSize = height / 2;
            }


            // set camera parameters.
            double fx;
            double fy;
            double cx;
            double cy;

            string loadDirectoryPath = Path.Combine(Application.persistentDataPath, "ArUcoCameraCalibrationExample");
            string calibratonDirectoryName = "camera_parameters" + width + "x" + height;
            string loadCalibratonFileDirectoryPath = Path.Combine(loadDirectoryPath, calibratonDirectoryName);
            string loadPath = Path.Combine(loadCalibratonFileDirectoryPath, calibratonDirectoryName + ".xml");
            if (useStoredCameraParameters && File.Exists(loadPath))
            {
                CameraParameters param;
                XmlSerializer serializer = new XmlSerializer(typeof(CameraParameters));
                using (var stream = new FileStream(loadPath, FileMode.Open))
                {
                    param = (CameraParameters)serializer.Deserialize(stream);
                }

                camMatrix = param.GetCameraMatrix();
                distCoeffs = new MatOfDouble(param.GetDistortionCoefficients());

                fx = param.camera_matrix[0];
                fy = param.camera_matrix[4];
                cx = param.camera_matrix[2];
                cy = param.camera_matrix[5];

                Debug.Log("Loaded CameraParameters from a stored XML file.");
                Debug.Log("loadPath: " + loadPath);

            }
            else
            {
                int max_d = (int)Mathf.Max(width, height);
                fx = max_d;
                fy = max_d;
                cx = width / 2.0f;
                cy = height / 2.0f;

                camMatrix = new Mat(3, 3, CvType.CV_64FC1);
                camMatrix.put(0, 0, fx);
                camMatrix.put(0, 1, 0);
                camMatrix.put(0, 2, cx);
                camMatrix.put(1, 0, 0);
                camMatrix.put(1, 1, fy);
                camMatrix.put(1, 2, cy);
                camMatrix.put(2, 0, 0);
                camMatrix.put(2, 1, 0);
                camMatrix.put(2, 2, 1.0f);

                distCoeffs = new MatOfDouble(0, 0, 0, 0);

                Debug.Log("Created a dummy CameraParameters.");
            }

            Debug.Log("camMatrix " + camMatrix.dump());
            Debug.Log("distCoeffs " + distCoeffs.dump());



            // calibration camera matrix values.
            Size imageSize = new Size(width * imageSizeScale, height * imageSizeScale);
            double apertureWidth = 0;
            double apertureHeight = 0;
            double[] fovx = new double[1];
            double[] fovy = new double[1];
            double[] focalLength = new double[1];
            Point principalPoint = new Point(0, 0);
            double[] aspectratio = new double[1];

            Calib3d.calibrationMatrixValues(camMatrix, imageSize, apertureWidth, apertureHeight, fovx, fovy, focalLength, principalPoint, aspectratio);

            Debug.Log("imageSize " + imageSize.ToString());
            Debug.Log("apertureWidth " + apertureWidth);
            Debug.Log("apertureHeight " + apertureHeight);
            Debug.Log("fovx " + fovx[0]);
            Debug.Log("fovy " + fovy[0]);
            Debug.Log("focalLength " + focalLength[0]);
            Debug.Log("principalPoint " + principalPoint.ToString());
            Debug.Log("aspectratio " + aspectratio[0]);


            // To convert the difference of the FOV value of the OpenCV and Unity. 
            double fovXScale = (2.0 * Mathf.Atan((float)(imageSize.width / (2.0 * fx)))) / (Mathf.Atan2((float)cx, (float)fx) + Mathf.Atan2((float)(imageSize.width - cx), (float)fx));
            double fovYScale = (2.0 * Mathf.Atan((float)(imageSize.height / (2.0 * fy)))) / (Mathf.Atan2((float)cy, (float)fy) + Mathf.Atan2((float)(imageSize.height - cy), (float)fy));

            Debug.Log("fovXScale " + fovXScale);
            Debug.Log("fovYScale " + fovYScale);


            rgbMat = new Mat(rgbaMat.rows(), rgbaMat.cols(), CvType.CV_8UC3);
            undistortedRgbMat = new Mat();
            ids = new Mat();
            corners = new List<Mat>();
            rejectedCorners = new List<Mat>();
            rotMat = new Mat(3, 3, CvType.CV_64FC1);
            dictionary = Objdetect.getPredefinedDictionary((int)dictionaryId);
            recoveredIdxs = new Mat();

            DetectorParameters detectorParams = new DetectorParameters();
            detectorParams.set_minDistanceToBorder(3);
            detectorParams.set_useAruco3Detection(true);
            detectorParams.set_cornerRefinementMethod(Objdetect.CORNER_REFINE_SUBPIX);
            detectorParams.set_minSideLengthCanonicalImg(16);
            detectorParams.set_errorCorrectionRate(0.8);
            RefineParameters refineParameters = new RefineParameters(10f, 3f, true);
            arucoDetector = new ArucoDetector(dictionary, detectorParams, refineParameters);


            gridBoard = new GridBoard(new Size(gridBoradMarkersX, gridBoradMarkersY), gridBoradMarkerLength, gridBoradMarkerSeparation, dictionary);


            charucoCorners = new Mat();
            charucoIds = new Mat();
            charucoBoard = new CharucoBoard(new Size(chArUcoBoradSquaresX, chArUcoBoradSquaresY), chArUcoBoradSquareLength, chArUcoBoradMarkerLength, dictionary);
            CharucoParameters charucoParameters = new CharucoParameters();
            charucoParameters.set_cameraMatrix(camMatrix);
            charucoParameters.set_distCoeffs(distCoeffs);
            charucoParameters.set_minMarkers(charucoMinMarkers);
            charucoDetector = new CharucoDetector(charucoBoard, charucoParameters, detectorParams, refineParameters);


            diamondCorners = new List<Mat>();
            diamondIds = new Mat(1, 1, CvType.CV_32SC4);
            charucoDiamondBoard = new CharucoBoard(new Size(3, 3), diamondSquareLength, diamondMarkerLength, dictionary);
            CharucoParameters charucoDiamondParameters = new CharucoParameters();
            charucoDiamondParameters.set_cameraMatrix(camMatrix);
            charucoDiamondParameters.set_distCoeffs(distCoeffs);
            charucoDiamondParameters.set_tryRefineMarkers(true);
            charucoDiamondDetector = new CharucoDetector(charucoDiamondBoard, charucoDiamondParameters, detectorParams, refineParameters);

#if !OPENCV_DONT_USE_WEBCAMTEXTURE_API
            // If the WebCam is front facing, flip the Mat horizontally. Required for successful detection.
            if (multiSource2MatHelper.source2MatHelper is WebCamTexture2MatHelper webCamHelper)
                webCamHelper.flipHorizontal = webCamHelper.IsFrontFacing();
#endif

            arHelper.SetCamMatrix(camMatrix);
            arHelper.SetDistCoeffs(distCoeffs);
            arHelper.Initialize(Screen.width, Screen.height, rgbMat.width(), rgbMat.height());
        }

        /// <summary>
        /// Raises the source to mat helper disposed event.
        /// </summary>
        public void OnSourceToMatHelperDisposed()
        {
            Debug.Log("OnSourceToMatHelperDisposed");

            if (rgbMat != null)
                rgbMat.Dispose();

            if (undistortedRgbMat != null)
                undistortedRgbMat.Dispose();

            if (texture != null)
            {
                Texture2D.Destroy(texture);
                texture = null;
            }

            if (arucoDetector != null)
                arucoDetector.Dispose();
            if (charucoDetector != null)
                charucoDetector.Dispose();
            if (charucoDiamondDetector != null)
                charucoDiamondDetector.Dispose();

            if (ids != null)
                ids.Dispose();
            foreach (var item in corners)
            {
                item.Dispose();
            }
            corners.Clear();
            foreach (var item in rejectedCorners)
            {
                item.Dispose();
            }
            rejectedCorners.Clear();

            if (rotMat != null)
                rotMat.Dispose();

            if (recoveredIdxs != null)
                recoveredIdxs.Dispose();

            if (gridBoard != null)
                gridBoard.Dispose();

            if (charucoCorners != null)
                charucoCorners.Dispose();
            if (charucoIds != null)
                charucoIds.Dispose();
            if (charucoBoard != null)
                charucoBoard.Dispose();

            foreach (var item in diamondCorners)
            {
                item.Dispose();
            }
            diamondCorners.Clear();
            if (diamondIds != null)
                diamondIds.Dispose();

            if (charucoDiamondBoard != null)
                charucoDiamondBoard.Dispose();

            if (arHelper != null)
                arHelper.Dispose();
        }

        /// <summary>
        /// Raises the source to mat helper error occurred event.
        /// </summary>
        /// <param name="errorCode">Error code.</param>
        /// <param name="message">Message.</param>
        public void OnSourceToMatHelperErrorOccurred(Source2MatHelperErrorCode errorCode, string message)
        {
            Debug.Log("OnSourceToMatHelperErrorOccurred " + errorCode + ":" + message);
        }

        // Update is called once per frame
        void Update()
        {
            if (multiSource2MatHelper.IsPlaying() && multiSource2MatHelper.DidUpdateThisFrame())
            {

                Mat rgbaMat = multiSource2MatHelper.GetMat();

                Imgproc.cvtColor(rgbaMat, rgbMat, Imgproc.COLOR_RGBA2RGB);

                switch (markerType)
                {
                    default:
                    case MarkerType.CanonicalMarker:

                        Calib3d.undistort(rgbMat, undistortedRgbMat, camMatrix, distCoeffs);
                        arucoDetector.detectMarkers(undistortedRgbMat, corners, ids, rejectedCorners);

                        if (corners.Count == ids.total() || ids.total() == 0)
                            Objdetect.drawDetectedMarkers(undistortedRgbMat, corners, ids, new Scalar(0, 255, 0));

                        if (applyEstimationPose)
                        {
                            // If at least one marker detected
                            if (ids.total() > 0)
                                EstimatePoseCanonicalMarker(undistortedRgbMat);
                        }

                        break;

                    case MarkerType.GridBoard:

                        Calib3d.undistort(rgbMat, undistortedRgbMat, camMatrix, distCoeffs);
                        arucoDetector.detectMarkers(undistortedRgbMat, corners, ids, rejectedCorners);

                        if (refineMarkerDetection)
                            arucoDetector.refineDetectedMarkers(undistortedRgbMat, gridBoard, corners, ids, rejectedCorners, camMatrix, distCoeffs, recoveredIdxs);

                        if (corners.Count == ids.total() || ids.total() == 0)
                            Objdetect.drawDetectedMarkers(undistortedRgbMat, corners, ids, new Scalar(0, 255, 0));

                        if (applyEstimationPose)
                        {
                            // If at least one marker detected
                            if (ids.total() > 0)
                                EstimatePoseGridBoard(undistortedRgbMat);
                        }

                        break;

                    case MarkerType.ChArUcoBoard:

                        /*
                        //
                        Calib3d.undistort(rgbMat, undistortedRgbMat, camMatrix, distCoeffs);

                        ids = new Mat();
                        corners = new List<Mat>();

                        // When fails to detect any markers, it throws the following error:
                        // objdetect::detectBoard_12() : OpenCV(4.8.0-dev) \opencv\modules\objdetect\src\aruco\aruco_board.cpp:39: error: (-215:Assertion failed) detectedIds.total() > 0ull in function 'cv::aruco::Board::Impl::matchImagePoints'
                        charucoDetector.detectBoard(undistortedRgbMat, charucoCorners, charucoIds, corners, ids); // error

                        if (corners.Count == ids.total() || ids.total() == 0)
                            Objdetect.drawDetectedMarkers(undistortedRgbMat, corners, ids, new Scalar(0, 255, 0));

                        
                        if (charucoCorners.total() == charucoIds.total() || charucoIds.total() == 0)
                            Objdetect.drawDetectedCornersCharuco(undistortedRgbMat, charucoCorners, charucoIds, new Scalar(0, 0, 255));

                        if (applyEstimationPose)
                        {
                            // if at least one charuco corner detected
                            if (charucoIds.total() > 0)
                                EstimatePoseChArUcoBoard(undistortedRgbMat);
                        }
                        //
                        */


                        //
                        Calib3d.undistort(rgbMat, undistortedRgbMat, camMatrix, distCoeffs);
                        arucoDetector.detectMarkers(undistortedRgbMat, corners, ids, rejectedCorners);

                        if (refineMarkerDetection)
                            // https://github.com/opencv/opencv/blob/377be68d923e40900ac5526242bcf221e3f355e5/modules/objdetect/src/aruco/charuco_detector.cpp#L310
                            arucoDetector.refineDetectedMarkers(undistortedRgbMat, charucoBoard, corners, ids, rejectedCorners);

                        // If at least one marker detected
                        if (ids.total() > 0)
                        {
                            charucoDetector.detectBoard(undistortedRgbMat, charucoCorners, charucoIds, corners, ids);

                            if (corners.Count == ids.total() || ids.total() == 0)
                                Objdetect.drawDetectedMarkers(undistortedRgbMat, corners, ids, new Scalar(0, 255, 0));

                            if (charucoCorners.total() == charucoIds.total() || charucoIds.total() == 0)
                                Objdetect.drawDetectedCornersCharuco(undistortedRgbMat, charucoCorners, charucoIds, new Scalar(0, 0, 255));

                            if (applyEstimationPose)
                            {
                                // if at least one charuco board detected
                                if (charucoIds.total() > 0)
                                    EstimatePoseChArUcoBoard(undistortedRgbMat);
                            }
                        }
                        //

                        break;

                    case MarkerType.ChArUcoDiamondMarker:

                        //
                        Calib3d.undistort(rgbMat, undistortedRgbMat, camMatrix, distCoeffs);

                        ids = new Mat();
                        corners = new List<Mat>();
                        charucoDiamondDetector.detectDiamonds(undistortedRgbMat, diamondCorners, diamondIds, corners, ids);

                        if (corners.Count == ids.total() || ids.total() == 0)
                            Objdetect.drawDetectedMarkers(undistortedRgbMat, corners, ids, new Scalar(0, 255, 0));

                        if (diamondCorners.Count == diamondIds.total() || diamondIds.total() == 0)
                            Objdetect.drawDetectedDiamonds(undistortedRgbMat, diamondCorners, diamondIds, new Scalar(0, 0, 255));

                        if (applyEstimationPose)
                        {
                            // If at least one diamonds detected
                            if (diamondIds.total() > 0)
                                EstimatePoseChArUcoDiamondMarker(undistortedRgbMat);
                        }
                        //


                        /*
                        //
                        Calib3d.undistort(rgbMat, undistortedRgbMat, camMatrix, distCoeffs);
                        arucoDetector.detectMarkers(undistortedRgbMat, corners, ids, rejectedCorners);

                        // If at least one marker detected
                        if (ids.total() > 0)
                        {
                            charucoDiamondDetector.detectDiamonds(undistortedRgbMat, diamondCorners, diamondIds, corners, ids);

                            if (corners.Count == ids.total() || ids.total() == 0)
                                Objdetect.drawDetectedMarkers(undistortedRgbMat, corners, ids, new Scalar(0, 255, 0));

                            if (diamondCorners.Count == diamondIds.total() || diamondIds.total() == 0)
                                Objdetect.drawDetectedDiamonds(undistortedRgbMat, diamondCorners, diamondIds, new Scalar(0, 0, 255));

                            if (applyEstimationPose)
                            {
                                // If at least one diamonds detected
                                if (diamondIds.total() > 0)
                                    EstimatePoseChArUcoDiamondMarker(undistortedRgbMat);
                            }
                        }
                        //
                        */

                        break;
                }


                if (showRejectedCorners && rejectedCorners.Count > 0)
                    Objdetect.drawDetectedMarkers(undistortedRgbMat, rejectedCorners, new Mat(), new Scalar(255, 0, 0));


                //Imgproc.putText (rgbaMat, "W:" + rgbaMat.width () + " H:" + rgbaMat.height () + " SO:" + Screen.orientation, new Point (5, rgbaMat.rows () - 10), Imgproc.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar (255, 255, 255, 255), 2, Imgproc.LINE_AA, false);

                Imgproc.cvtColor(undistortedRgbMat, rgbaMat, Imgproc.COLOR_RGB2RGBA);

                Utils.matToTexture2D(rgbaMat, texture);
            }
        }

        private void EstimatePoseCanonicalMarker(Mat rgbMat)
        {
            using (MatOfPoint3f objectPoints = new MatOfPoint3f(
                new Point3(-markerLength / 2f, markerLength / 2f, 0),
                new Point3(markerLength / 2f, markerLength / 2f, 0),
                new Point3(markerLength / 2f, -markerLength / 2f, 0),
                new Point3(-markerLength / 2f, -markerLength / 2f, 0)
                ))
            {
                for (int i = 0; i < corners.Count; i++)
                {
                    using (Mat rvec = new Mat(1, 1, CvType.CV_64FC3))
                    using (Mat tvec = new Mat(1, 1, CvType.CV_64FC3))
                    using (Mat corner_4x1 = corners[i].reshape(2, 4)) // 1*4*CV_32FC2 => 4*1*CV_32FC2
                    using (MatOfPoint2f imagePoints = new MatOfPoint2f(corner_4x1))
                    {
                        // Calculate pose for each marker
                        Calib3d.solvePnP(objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);

                        // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                        Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);

                        // This example can display the ARObject on only first detected marker.
                        if (i == 0)
                        {
                            //UpdateARObjectTransform(rvec, tvec);
                            arHelper.imagePoints = imagePoints.toVector2Array();
                            arHelper.objectPoints = objectPoints.toVector3Array();
                        }
                    }
                }
            }
        }

        private void EstimatePoseGridBoard(Mat rgbMat)
        {
            if (ids.total() == 0)
                return;

            // https://github.com/opencv/opencv_contrib/blob/f10c84d48b0714f2b408c9e5cccfac1277c8e6cc/modules/aruco/src/aruco.cpp#L43
            if (corners.Count != ids.total())
                return;

            using (Mat rvec = new Mat(1, 1, CvType.CV_64FC3))
            using (Mat tvec = new Mat(1, 1, CvType.CV_64FC3))
            using (Mat objectPoints = new Mat())
            using (Mat imagePoints = new Mat())
            {
                // Get object and image points for the solvePnP function
                gridBoard.matchImagePoints(corners, ids, objectPoints, imagePoints);

                if (imagePoints.total() != objectPoints.total())
                    return;

                if (objectPoints.total() == 0) // 0 of the detected markers in board
                    return;

                // Find pose
                MatOfPoint3f obectjPoints_p3f = new MatOfPoint3f(objectPoints);
                MatOfPoint2f imagePoints_p3f = new MatOfPoint2f(imagePoints);
                Calib3d.solvePnP(obectjPoints_p3f, imagePoints_p3f, camMatrix, distCoeffs, rvec, tvec);

                // If at least one board marker detected
                int markersOfBoardDetected = (int)objectPoints.total() / 4;
                if (markersOfBoardDetected > 0)
                {
                    // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                    Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);

                    //UpdateARObjectTransform(rvec, tvec);
                    arHelper.imagePoints = imagePoints_p3f.toVector2Array();
                    arHelper.objectPoints = obectjPoints_p3f.toVector3Array();
                }
            }
        }

        private void EstimatePoseChArUcoBoard(Mat rgbMat)
        {
            /*
            //
            using (Mat rvec = new Mat(1, 1, CvType.CV_64FC3))
            using (Mat tvec = new Mat(1, 1, CvType.CV_64FC3))
            {
                bool valid = Aruco.estimatePoseCharucoBoard(charucoCorners, charucoIds, charucoBoard, camMatrix, distCoeffs, rvec, tvec); // error

                // if at least one board marker detected
                if (valid)
                {
                    // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                    Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);

                    UpdateARObjectTransform(rvec, tvec);
                }
            }
            //
            */


            //
            // https://github.com/opencv/opencv_contrib/blob/f10c84d48b0714f2b408c9e5cccfac1277c8e6cc/modules/aruco/src/aruco.cpp#L63
            if (charucoCorners.total() != charucoIds.total())
                return;
            if (charucoIds.total() < 4)
                return;

            using (Mat rvec = new Mat(1, 1, CvType.CV_64FC3))
            using (Mat tvec = new Mat(1, 1, CvType.CV_64FC3))
            using (Mat objectPoints = new Mat())
            using (Mat imagePoints = new Mat())
            {
                // Get object and image points for the solvePnP function
                List<Mat> charucoCorners_list = new List<Mat>();
                for (int i = 0; i < charucoCorners.rows(); i++)
                {
                    charucoCorners_list.Add(charucoCorners.row(i));
                }
                charucoBoard.matchImagePoints(charucoCorners_list, charucoIds, objectPoints, imagePoints);

                // Find pose
                MatOfPoint3f objectPoints_p3f = new MatOfPoint3f(objectPoints);
                MatOfPoint2f imagePoints_p3f = new MatOfPoint2f(imagePoints);

                try
                {
                    Calib3d.solvePnP(objectPoints_p3f, imagePoints_p3f, camMatrix, distCoeffs, rvec, tvec);
                }
                catch (CvException e)
                {
                    Debug.LogWarning("estimatePoseCharucoBoard: " + e);
                    return;
                }

                // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);

                //UpdateARObjectTransform(rvec, tvec);
                arHelper.imagePoints = imagePoints_p3f.toVector2Array();
                arHelper.objectPoints = objectPoints_p3f.toVector3Array();
                //
            }
        }

        private void EstimatePoseChArUcoDiamondMarker(Mat rgbMat)
        {
            using (MatOfPoint3f objectPoints = new MatOfPoint3f(
                new Point3(-markerLength / 2f, markerLength / 2f, 0),
                new Point3(markerLength / 2f, markerLength / 2f, 0),
                new Point3(markerLength / 2f, -markerLength / 2f, 0),
                new Point3(-markerLength / 2f, -markerLength / 2f, 0)
                ))
            {
                for (int i = 0; i < diamondCorners.Count; i++)
                {
                    using (Mat rvec = new Mat(1, 1, CvType.CV_64FC3))
                    using (Mat tvec = new Mat(1, 1, CvType.CV_64FC3))
                    using (Mat corner_4x1 = diamondCorners[i].reshape(2, 4)) // 1*4*CV_32FC2 => 4*1*CV_32FC2
                    using (MatOfPoint2f imagePoints = new MatOfPoint2f(corner_4x1))
                    {
                        // Calculate pose for each marker
                        Calib3d.solvePnP(objectPoints, imagePoints, camMatrix, distCoeffs, rvec, tvec);

                        // In this example we are processing with RGB color image, so Axis-color correspondences are X: blue, Y: green, Z: red. (Usually X: red, Y: green, Z: blue)
                        Calib3d.drawFrameAxes(rgbMat, camMatrix, distCoeffs, rvec, tvec, markerLength * 0.5f);

                        // This example can display the ARObject on only first detected marker.
                        if (i == 0)
                        {
                            //UpdateARObjectTransform(rvec, tvec);
                            arHelper.imagePoints = imagePoints.toVector2Array();
                            arHelper.objectPoints = objectPoints.toVector3Array();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Raises the destroy event.
        /// </summary>
        void OnDestroy()
        {
            multiSource2MatHelper.Dispose();


            Utils.setDebugMode(false);
        }

        /// <summary>
        /// Raises the back button click event.
        /// </summary>
        public void OnBackButtonClick()
        {
            SceneManager.LoadScene("OpenCVForUnityExample");
        }

        /// <summary>
        /// Raises the play button click event.
        /// </summary>
        public void OnPlayButtonClick()
        {
            multiSource2MatHelper.Play();
        }

        /// <summary>
        /// Raises the pause button click event.
        /// </summary>
        public void OnPauseButtonClick()
        {
            multiSource2MatHelper.Pause();
        }

        /// <summary>
        /// Raises the stop button click event.
        /// </summary>
        public void OnStopButtonClick()
        {
            multiSource2MatHelper.Stop();
        }

        /// <summary>
        /// Raises the change camera button click event.
        /// </summary>
        public void OnChangeCameraButtonClick()
        {
            multiSource2MatHelper.requestedIsFrontFacing = !multiSource2MatHelper.requestedIsFrontFacing;
        }

        /// <summary>
        /// Raises the marker type dropdown value changed event.
        /// </summary>
        public void OnMarkerTypeDropdownValueChanged(int result)
        {
            if ((int)markerType != result)
            {
                markerType = (MarkerType)result;

                refineMarkerDetectionToggle.interactable = (markerType == MarkerType.GridBoard || markerType == MarkerType.ChArUcoBoard);

                //ResetObjectTransform();

                if (multiSource2MatHelper.IsInitialized())
                    multiSource2MatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the dictionary id dropdown value changed event.
        /// </summary>
        public void OnDictionaryIdDropdownValueChanged(int result)
        {
            if ((int)dictionaryId != result)
            {
                dictionaryId = (ArUcoDictionary)result;
                dictionary = Objdetect.getPredefinedDictionary((int)dictionaryId);

                //ResetObjectTransform();

                if (multiSource2MatHelper.IsInitialized())
                    multiSource2MatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the use stored camera parameters toggle value changed event.
        /// </summary>
        public void OnUseStoredCameraParametersToggleValueChanged()
        {
            if (useStoredCameraParameters != useStoredCameraParametersToggle.isOn)
            {
                useStoredCameraParameters = useStoredCameraParametersToggle.isOn;

                if (multiSource2MatHelper != null && multiSource2MatHelper.IsInitialized())
                    multiSource2MatHelper.Initialize();
            }
        }

        /// <summary>
        /// Raises the show rejected corners toggle value changed event.
        /// </summary>
        public void OnShowRejectedCornersToggleValueChanged()
        {
            showRejectedCorners = showRejectedCornersToggle.isOn;
        }

        /// <summary>
        /// Raises the refine marker detection toggle value changed event.
        /// </summary>
        public void OnRefineMarkerDetectionToggleValueChanged()
        {
            refineMarkerDetection = refineMarkerDetectionToggle.isOn;
        }


        /// <summary>
        /// Raises the enable low pass filter toggle value changed event.
        /// </summary>
        public void OnEnableLowPassFilterToggleValueChanged()
        {
            arHelper.useLowPassFilter = enableLowPassFilterToggle.isOn;
        }

        public enum MarkerType
        {
            CanonicalMarker,
            GridBoard,
            ChArUcoBoard,
            ChArUcoDiamondMarker
        }

        public enum ArUcoDictionary
        {
            DICT_4X4_50 = Objdetect.DICT_4X4_50,
            DICT_4X4_100 = Objdetect.DICT_4X4_100,
            DICT_4X4_250 = Objdetect.DICT_4X4_250,
            DICT_4X4_1000 = Objdetect.DICT_4X4_1000,
            DICT_5X5_50 = Objdetect.DICT_5X5_50,
            DICT_5X5_100 = Objdetect.DICT_5X5_100,
            DICT_5X5_250 = Objdetect.DICT_5X5_250,
            DICT_5X5_1000 = Objdetect.DICT_5X5_1000,
            DICT_6X6_50 = Objdetect.DICT_6X6_50,
            DICT_6X6_100 = Objdetect.DICT_6X6_100,
            DICT_6X6_250 = Objdetect.DICT_6X6_250,
            DICT_6X6_1000 = Objdetect.DICT_6X6_1000,
            DICT_7X7_50 = Objdetect.DICT_7X7_50,
            DICT_7X7_100 = Objdetect.DICT_7X7_100,
            DICT_7X7_250 = Objdetect.DICT_7X7_250,
            DICT_7X7_1000 = Objdetect.DICT_7X7_1000,
            DICT_ARUCO_ORIGINAL = Objdetect.DICT_ARUCO_ORIGINAL,
        }
    }
}