using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
using System.IO;
using System;

//THIS SCRIPT Includes two cameras on the drones as well as uses tagging

public class TwoCam2 : MonoBehaviour
{
    public List<GameObject> cameras;
    public Shader uberReplacementShader;
    private readonly int resWidth = 256;
    private readonly int resHeight = 256;

    //To store the recent parameters of the drones for th e front camera
    /*private List<float> initial_xdist_frontCam = new List<float>();
    private List<float> initial_zdist_frontCam = new List<float>();
    private List<float> initial_ydist_frontCam = new List<float>();*/

    //To store the recent parameters of the drones for the left camera
    private List<float> initial_xdist_leftCam = new List<float>();
    private List<float> initial_zdist_leftCam = new List<float>();
    private List<float> initial_ydist_leftCam = new List<float>();

    //To store the recent parameters of the drones for the right camera
    private List<float> initial_xdist_rightCam = new List<float>();
    private List<float> initial_zdist_rightCam = new List<float>();
    private List<float> initial_ydist_rightCam = new List<float>();


    //To store the previous parameters of the drone
    //private List<Tuple<Tuple<float, float>, float>> previous_dist_frontCam = new List<Tuple<Tuple<float, float>, float>>();
    private List<Tuple<Tuple<float, float>, float>> previous_dist_leftCam = new List<Tuple<Tuple<float, float>, float>>();
    private List<Tuple<Tuple<float, float>, float>> previous_dist_rightCam = new List<Tuple<Tuple<float, float>, float>>();


    //To store the current parameters of the drone
    //private List<Tuple<Tuple<float, float>, float>> current_dist_frontCam = new List<Tuple<Tuple<float, float>, float>>();
    private List<Tuple<Tuple<float, float>, float>> current_dist_leftCam = new List<Tuple<Tuple<float, float>, float>>();
    private List<Tuple<Tuple<float, float>, float>> current_dist_rightCam = new List<Tuple<Tuple<float, float>, float>>();

    //Storing the previous tags of the drones
    List<int> prevTagsLeft = new List<int>();
    List<int> prevTagsRight = new List<int>();
    //Storing the new tags of the drones
    List<int> newTagsLeft = new List<int>();
    List<int> newTagsRight = new List<int>();
    //To traverse the tags list
    List<bool> prevtagTakenLeft = new List<bool>();
    List<bool> prevtagTakenRight = new List<bool>();

    private float threshold = 40f;
    static int pixelcountThreshold = 10;
    public float threshold_distance = 2f;

    public float multiplier = 0.3f;
    public float r1 = 1f;
    public float r2 = 1.5f;
    public float r3 = 1f;
    public float r4 = 1f;
    public float r5 = 1f;
    public float r6 = 1f;
    public float r7 = 1f;
    public float r8 = 1f;

    //public float frontWeight = 1f;
    public float leftWeight = 0.5f;
    public float rightWeight = 0.5f;

    public enum ReplacementMode
    {
        ObjectId = 0,
        CatergoryId = 1,
        DepthCompressed = 2,
        DepthMultichannel = 3,
        Normals = 4
    };

    private bool startit = true;
    //private float avgDisplacement = 0f;
    private Vector3 avgDisplacement = Vector3.zero;
    private Vector3 targetposition;
    private int counter = 0;

    bool isSafe(float[][] depthMap, int row, int col, int n, int l, bool[,] visited)
    {
        return (row >= 0 && row < n) &&
            (col >= 0 && col < l) &&
            (depthMap[row][col] < threshold &&
            !visited[row, col]);
    }

    void DFS(float[][] depthMap, int row, int col, int n, int l, List<Tuple<int, int>> list, bool[,] visited)
    {
        // These arrays are used to get row and column
        // numbers of 4 neighbours of a given cell
        int[] rowNbr = { -1, 1, 0, 0 };
        int[] colNbr = { 0, 0, 1, -1 };

        // Mark this cell as visited
        visited[row, col] = true;
        var tuple = new Tuple<int, int>(row, col);
        list.Add(tuple);

        // Recur for all connected neighbours
        for (int k = 0; k < 4; ++k)
        {
            if (isSafe(depthMap, row + rowNbr[k],
                        col + colNbr[k], n, l, visited))
            {
                DFS(depthMap, row + rowNbr[k],
                    col + colNbr[k], n, l, list, visited);
            }
        }
    }

    List<List<Tuple<int, int>>> connectedComponents(float[][] depthMap, int n, int l)
    {
        //n = rows and l = columns
        int connectedComp = 0;
        //int l = M[0].Length;
        bool[,] visited = new bool[256, 256];
        List<List<Tuple<int, int>>> components = new List<List<Tuple<int, int>>>();

        for (int j = 0; j < l; j++)
        {
            for (int i = 0; i < n; i++)
            {
                if (!visited[i, j] && depthMap[i][j] < threshold)
                {
                    var list = new List<Tuple<int, int>>();
                    DFS(depthMap, i, j, n, l, list, visited);
                    if (list.Count > pixelcountThreshold)
                    {
                        components.Add(list);
                        connectedComp++;
                    }
                }
            }
        }
        return components;
    }

    List<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>> computeDepth()
    {
        List<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>> answer = new List<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>>();

        for (int camera_num = 0; camera_num < cameras.Count; camera_num++)
        {
            List<Tuple<Tuple<int, int>, float>> depth = new List<Tuple<Tuple<int, int>, float>>();
            //Stroring the number of pixels the drone
            List<int> weights = new List<int>();
            RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
            cameras[camera_num].GetComponent<Camera>().targetTexture = rt;
            Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
            //cameras[camera_num].GetComponent<Camera>().usePhysicalProperties = true;
            cameras[camera_num].GetComponent<Camera>().RemoveAllCommandBuffers();
            var cb = new CommandBuffer();
            cb.SetGlobalFloat("_OutputMode", (int)ReplacementMode.DepthCompressed); // @TODO: CommandBuffer is missing SetGlobalInt() method
            cameras[camera_num].GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cb);
            cameras[camera_num].GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeFinalPass, cb);
            cameras[camera_num].GetComponent<Camera>().SetReplacementShader(uberReplacementShader, "");
            cameras[camera_num].GetComponent<Camera>().backgroundColor = Color.white;
            cameras[camera_num].GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            cameras[camera_num].GetComponent<Camera>().allowHDR = false;
            cameras[camera_num].GetComponent<Camera>().allowMSAA = false;
            cameras[camera_num].GetComponent<Camera>().Render();
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
            cameras[camera_num].GetComponent<Camera>().targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);
            //byte[] bytes = screenShot.EncodeToJPG();
            //Debug.Log(this.transform.name);
            /*if (true)
            {
                Debug.Log("Here1");
                string filename = ScreenShotName(resWidth, resHeight, this.gameObject.name, camera_num);
                System.IO.File.WriteAllBytes(filename, bytes);
            }*/

            float[][] arr = new float[256][];
            for (int i = 0; i < 256; i++)
            {
                arr[i] = new float[256];
                for (int j = 0; j < 256; j++)
                {
                    arr[i][j] = screenShot.GetPixel(j, i).grayscale * cameras[camera_num].GetComponent<Camera>().farClipPlane;
                }
            }

            List<List<Tuple<int, int>>> components = connectedComponents(arr, 256, 256);
            int k = components.Count;
            for (int i = 0; i < components.Count; i++)
            {
                float min_depth = 999f;
                int drone_row = 0;
                int drone_col = 0;
                for (int j = 0; j < components[i].Count; j++)
                {
                    int myRow = components[i][j].Item1;
                    int myCol = components[i][j].Item2;
                    drone_row += myRow;
                    drone_col += myCol;
                    if (arr[myRow][myCol] < min_depth)
                    {
                        min_depth = arr[myRow][myCol];
                    }
                }
                drone_row /= components[i].Count;
                drone_col /= components[i].Count;

                //depth.Add(min_depth);
                var tuple1 = new Tuple<int, int>(drone_row, drone_col);
                var tuple2 = new Tuple<Tuple<int, int>, float>(tuple1, min_depth);
                depth.Add(tuple2);
                weights.Add(components[i].Count);
                //Debug.Log(drone_row + " " + drone_col + " " + min_depth);
            }

            Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>> cam_ans = new Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>(depth, weights);
            answer.Add(cam_ans);
        }

        return answer;
    }

    private string ScreenShotName(int width, int height, string s, int i)
    {
        Debug.Log("screenshot" + s + i + ".jpeg");
        return string.Format("{0}/screenshot" + s + i + ".jpeg",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!uberReplacementShader)
            uberReplacementShader = Shader.Find("Hidden/UberReplacement");

        List<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>> initialDepth_Weight = computeDepth();
    }

    // Update is called once per frame
    void Update()
    {
        avgDisplacement = Vector3.zero;
        List<float> halfAngle = new List<float>();
        for (int i = 0; i < cameras.Count; i++)
        {
            float f = cameras[i].GetComponent<Camera>().fieldOfView;
            halfAngle.Add(f / 2);
        }

        targetposition = this.transform.position;
        List<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>> initialDepth_Weight = computeDepth();

        int camera_num = 0; //LEFT CAMERA
        for (int i = 0; i < initialDepth_Weight[camera_num].Item1.Count; i++)
        {
            int pix_x = initialDepth_Weight[camera_num].Item1[i].Item1.Item2;
            int pix_y = initialDepth_Weight[camera_num].Item1[i].Item1.Item1;
            float z_depth = initialDepth_Weight[camera_num].Item1[i].Item2;
            float disp_x = 0;
            float disp_y = 0;
            //float angle1 = 0;
            
            
            // Y-Axis
            if (pix_y > 128)
            {
                float angle = (pix_y - 128) * 50 / 128f;
                disp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                
            }
            else if (pix_y < 130)
            {
                float angle = (128 - pix_y) * 50 / 128f;
                disp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
            }

            // X-Axis
            if (pix_x < 128)
            {
                float angle = (128 - pix_x) * 50 / 128f;
                disp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                //angle1 = angle;
            }
            else if (pix_x > 130)
            {
                float angle = (pix_x - 128) * 50 / 128f;
                disp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                //angle1 = angle;
            }
            
            //Debug.Log("depth value " + z_depth + " pix_x" + pix_x +" angle" + angle1);
            //Debug.Log("befote x is " + disp_x + " y is " + 1.8f * disp_y + " z is " + z_depth);
            float actualx = disp_x * Mathf.Cos(halfAngle[0] * Mathf.PI / 180) - z_depth * Mathf.Sin(halfAngle[0] * Mathf.PI / 180);
            float actualz = disp_x * Mathf.Sin(halfAngle[0] * Mathf.PI / 180) + z_depth * Mathf.Cos(halfAngle[0] * Mathf.PI / 180);


            Debug.Log("x is " + actualx + " y is " + 1.8f * disp_y + " z is " + actualz);
            var t1 = new Tuple<float, float>(disp_x, disp_y);
            var t2 = new Tuple<Tuple<float, float>, float>(t1, z_depth);
            //previous_dist_leftCam.Add(t2);

            //Debug.Log("avg dusp of drone  " + i + " " + disp_x + " " + disp_y + " " + z_depth);
        }

        camera_num += 1;  //RIGHT CAMERA
        for (int i = 0; i < initialDepth_Weight[camera_num].Item1.Count; i++)
        {
            int pix_x = initialDepth_Weight[camera_num].Item1[i].Item1.Item2;
            int pix_y = initialDepth_Weight[camera_num].Item1[i].Item1.Item1;
            float z_depth = initialDepth_Weight[camera_num].Item1[i].Item2;
            float disp_x = 0;
            float disp_y = 0;
            //float angle1 = 0;

            // Y-Axis
            if (pix_y > 128)
            {
                float angle = (pix_y - 128) * 50 / 128f;
                disp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);

            }
            else if (pix_y < 130)
            {
                float angle = (128 - pix_y) * 50 / 128f;
                disp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
            }

            // X-Axis
            if (pix_x < 128)
            {
                float angle = (128 - pix_x) * 50 / 128f;
                disp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                //angle1 = angle;
            }
            else if (pix_x > 130)
            {
                float angle = (pix_x - 128) * 50 / 128f;
                disp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                //angle1 = angle;
            }

            //Debug.Log("depth value " + z_depth + " pix_x" + pix_x + " angle" + angle1);
            //Debug.Log("befote x is " + disp_x + " y is " + 1.8f * disp_y + " z is " + z_depth);
            float actualx = disp_x * Mathf.Cos(halfAngle[0] * Mathf.PI / 180) + z_depth * Mathf.Sin(halfAngle[0] * Mathf.PI / 180);
            float actualz = -disp_x * Mathf.Sin(halfAngle[0] * Mathf.PI / 180) + z_depth * Mathf.Cos(halfAngle[0] * Mathf.PI / 180);
            Debug.Log("x is " + actualx + " y is " + 1.8f * disp_y + " z is " + actualz);

            var t1 = new Tuple<float, float>(disp_x, disp_y);
            var t2 = new Tuple<Tuple<float, float>, float>(t1, z_depth);
            //previous_dist_rightCam.Add(t2);

            //Debug.Log("avg dusp of drone  " + i + " " + disp_x + " " + disp_y + " " + z_depth);
        }

        //Previous tags assigned to drones
        for (int i = 1; i <= previous_dist_leftCam.Count; i++)
        {
            prevTagsLeft.Add(i);
        }
        for (int i = 1; i <= previous_dist_rightCam.Count; i++)
        {
            prevTagsRight.Add(i);
        }

        startit = true;


    }
}
