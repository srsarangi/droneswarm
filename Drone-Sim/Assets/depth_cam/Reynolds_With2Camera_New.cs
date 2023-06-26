using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
using System.IO;
using System;

public class Reynolds_With2Camera_New : MonoBehaviour
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

    //Storing the obstacles of the drones
    List<Tuple<Tuple<Tuple<float, float>, Tuple<float, float>>, float>> obstacleAtLeft = new List<Tuple<Tuple<Tuple<float, float>, Tuple<float, float>>, float>>();
    List<Tuple<Tuple<Tuple<float, float>, Tuple<float, float>>, float>> obstacleAtRight = new List<Tuple<Tuple<Tuple<float, float>, Tuple<float, float>>, float>>();


    private float threshold = 40f;
    static int pixelcountThreshold = 10;
    static int obstaclePixelCountThreshold = 50;

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

    //To see whether a coponent is an obstacle or not!?
    bool isHuge(List<Tuple<int, int>> component)
    {
        if (component.Count > obstaclePixelCountThreshold)
        {
            return true;
        }
        return false;
    }

    bool extendsFromBottom(List<Tuple<int, int>> component)
    {
        bool result = false;
        int countBottom = 0;
        for (int i = 0; i < component.Count; i++)
        {
            if (component[i].Item1 > 250)
            {
                countBottom += 1;
                if (countBottom > 10)
                {
                    result = true;
                    break;
                }
            }
        }
        return result;
    }

    bool isObstacle(List<Tuple<int, int>> component)
    {
        if (isHuge(component))
        {
            return true;
        }
        return false;
    }

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
        Queue<Tuple<int, int>> queue = new Queue<Tuple<int, int>>();

        // Mark this cell as visited
        visited[row, col] = true;
        var tuple = new Tuple<int, int>(row, col);
        queue.Enqueue(tuple);
        

        while(queue.Count != 0)
        {
            tuple = queue.Dequeue();
            list.Add(tuple);
            for (int k = 0; k < 4; ++k)
            {
                if (isSafe(depthMap, tuple.Item1 + rowNbr[k],
                            tuple.Item2 + colNbr[k], n, l, visited))
                {
                    queue.Enqueue(new Tuple<int, int>(tuple.Item1 + rowNbr[k], tuple.Item2 + colNbr[k]));
                    visited[tuple.Item1 + rowNbr[k], tuple.Item2 + colNbr[k]] = true;
                }
            }
        }
        // Recur for all connected neighbours
    }

    Tuple<List<List<Tuple<int, int>>>, List<List<Tuple<int, int>>>> connectedComponents(float[][] depthMap, int n, int l)
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
        List<List<Tuple<int, int>>> drones = new List<List<Tuple<int, int>>>();
        List<List<Tuple<int, int>>> obstacles = new List<List<Tuple<int, int>>>();
        for (int i = 0; i < components.Count; i++)
        {
            if (components[i].Count > 5000)
            {
                obstacles.Add(components[i]);
            }
            else
            {
                drones.Add(components[i]);
            }
        }
        var t = new Tuple<List<List<Tuple<int, int>>>, List<List<Tuple<int, int>>>>(drones, obstacles);
        return t;
    }

    private int i = 1;

    List<Tuple<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>, List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>>> computeDepth()
    {
        List<Tuple<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>, List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>>> answer = new List<Tuple<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>, List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>>>();

        for (int camera_num = 0; camera_num < cameras.Count; camera_num++)
        {
            List<Tuple<Tuple<int, int>, float>> depth = new List<Tuple<Tuple<int, int>, float>>();
            //Stroring the number of pixels the drone has
            List<int> weights = new List<int>();
            //Storing th ebounding boxes of the drones
            List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>> bounding_boxes = new List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>();

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
            byte[] bytes = screenShot.EncodeToJPG();

            //Debug.Log(this.transform.name);
            /*if (true)
            {
                Debug.Log("Here1");
                string filename = ScreenShotName(resWidth, resHeight, this.gameObject.name, camera_num);
                System.IO.File.WriteAllBytes(filename, bytes);
            }*/

            /*float[][] arr = new float[256][];
            for (int i = 0; i < 256; i++)
            {
                arr[i] = new float[256];
                for (int j = 0; j < 256; j++)
                {
                    arr[i][j] = screenShot.GetPixel(j, i).grayscale * cameras[camera_num].GetComponent<Camera>().farClipPlane;
                }
            }*/

            float[][] arr = new float[256][];
            StreamWriter writetext = new StreamWriter("write"+ camera_num +this.gameObject.name+".txt");
            for (int i = 0; i < 256; i++)
            {
                string line = "";
                arr[i] = new float[256];
                for (int j = 0; j < 256; j++)
                {
                    arr[i][j] = screenShot.GetPixel(j, i).grayscale * 40;
                    line += (arr[i][j].ToString() + " ");
                }
                writetext.WriteLine(line);           
            }

            Tuple<List<List<Tuple<int, int>>>, List<List<Tuple<int, int>>>> comps = connectedComponents(arr, 256, 256);
            List<List<Tuple<int, int>>> componentsDrone = comps.Item1;
            List<List<Tuple<int, int>>> componentsObstacle = comps.Item2;

            int k = componentsDrone.Count;
            for (int i = 0; i < componentsDrone.Count; i++)
            {
                float min_depth = 999f;
                int drone_row = 0;
                int drone_col = 0;
                for (int j = 0; j < componentsDrone[i].Count; j++)
                {
                    int myRow = componentsDrone[i][j].Item1;
                    int myCol = componentsDrone[i][j].Item2;
                    drone_row += myRow;
                    drone_col += myCol;
                    if (arr[myRow][myCol] < min_depth)
                    {
                        min_depth = arr[myRow][myCol];
                    }
                }
                drone_row /= componentsDrone[i].Count;
                drone_col /= componentsDrone[i].Count;

                //depth.Add(min_depth);
                var tuple1 = new Tuple<int, int>(drone_row, drone_col);
                var tuple2 = new Tuple<Tuple<int, int>, float>(tuple1, min_depth);
                depth.Add(tuple2);
                weights.Add(componentsDrone[i].Count);
                //Debug.Log(drone_row + " " + drone_col + " " + min_depth);
            }
            
            int p = componentsObstacle.Count;
            for(int i=0; i<componentsObstacle.Count; i++)
            {
                int min_row = 999;
                int min_col = 999;
                int max_row = -999;
                int max_col = -999;
                float min_depth = 999f;
                for (int j = 0; j < componentsObstacle[i].Count; j++)
                {
                    int myRow = componentsObstacle[i][j].Item1;
                    int myCol = componentsObstacle[i][j].Item2;
                    if(myRow > max_row) { max_row = myRow; }
                    if(myRow < min_row) { min_row = myRow; }
                    if(myCol > max_col) { max_col = myCol; }
                    if(myCol < min_col) { min_col = myCol; }
                    if (arr[myRow][myCol] < min_depth)
                    {
                        min_depth = arr[myRow][myCol];
                    }

                }
                var tuple1 = new Tuple<int, int>(min_row, min_col);
                var tuple2 = new Tuple<int, int>(max_row, max_col);
                var tuple3 = new Tuple<Tuple<int, int>, Tuple<int, int>>(tuple1, tuple2);
                var boundary = new Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>(tuple3, min_depth);
                bounding_boxes.Add(boundary);
            }

            Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>> t = new Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>(depth, weights);
            Tuple<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>, List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>> cam_ans = new Tuple<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>, List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>>(t, bounding_boxes);
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

        List<Tuple<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>, List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>>> initialDepth_Weight_Obstacle = computeDepth();
    }

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
    private int dronec = 0;


    bool outOfCriticalRegionleft(Tuple<Tuple<Tuple<float, float>, Tuple<float, float>>, float> bound)
    {
        float mindisp_x = bound.Item1.Item1.Item1;
        float mindisp_y = bound.Item1.Item1.Item2;
        float maxdisp_x = bound.Item1.Item2.Item1;
        float maxdisp_y = bound.Item1.Item2.Item2;
        float z_depth = bound.Item2;
        if(Mathf.Abs(maxdisp_x) > 8)
        {
            return true;
        }
        if(z_depth > 16)
        {
            return true;
        }
        //if(Mathf.Abs(mindisp_y)


        return false;
        /*if (bound.Item2 > 16)
        {
            return true;
        }
        if(Mathf.Abs(bound.Item1.Item1.Item1) > 8)
        {
            return true;
        }
        if (Mathf.Abs(bound.Item1.Item1.Item2) > 8)
        {
            return true;
        }

        return false;*/
    }

    bool outOfCriticalRegionright(Tuple<Tuple<Tuple<float, float>, Tuple<float, float>>, float> bound)
    {
        if (bound.Item2 > 16)
        {
            return true;
        }
        if (Mathf.Abs(bound.Item1.Item1.Item1) > 8)
        {
            return true;
        }
        if (Mathf.Abs(bound.Item1.Item1.Item2) > 8)
        {
            return true;
        }

        return false;
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

        if (startit)
        {
            targetposition = this.transform.position;
            List<Tuple<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>, List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>>> initialDepth_Weight_obstacle = computeDepth();
            //List<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>> initialDepth_Weight = computeDepth();

            
            int camera_num = 0; //LEFT CAMERA
            Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>> initialDepth_WeightLeft = initialDepth_Weight_obstacle[camera_num].Item1;
            List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>> initialObstacleLeft = initialDepth_Weight_obstacle[camera_num].Item2;
            for (int i = 0; i < initialDepth_WeightLeft.Item1.Count; i++)
            {
                int pix_x = initialDepth_WeightLeft.Item1[i].Item1.Item2;
                int pix_y = initialDepth_WeightLeft.Item1[i].Item1.Item1;
                float z_depth = initialDepth_WeightLeft.Item1[i].Item2;
                float disp_x = 0;
                float disp_y = 0;
                //float yangle = 0;

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

                initial_xdist_leftCam.Add(actualx);
                initial_ydist_leftCam.Add(disp_y);
                initial_zdist_leftCam.Add(actualz);

                var t1 = new Tuple<float, float>(disp_x, disp_y);
                var t2 = new Tuple<Tuple<float, float>, float>(t1, z_depth);
                previous_dist_leftCam.Add(t2);
            }

            camera_num += 1;  //RIGHT CAMERA
            Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>> initialDepth_WeightRight = initialDepth_Weight_obstacle[camera_num].Item1;
            List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>> initialObstacleRight= initialDepth_Weight_obstacle[camera_num].Item2;
            for (int i = 0; i < initialDepth_WeightRight.Item1.Count; i++)
            {

                int pix_x = initialDepth_WeightRight.Item1[i].Item1.Item2;
                int pix_y = initialDepth_WeightRight.Item1[i].Item1.Item1;
                float z_depth = initialDepth_WeightRight.Item1[i].Item2;
                float disp_x = 0;
                float disp_y = 0;

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

                initial_xdist_rightCam.Add(actualx);
                initial_ydist_rightCam.Add(disp_y);
                initial_zdist_rightCam.Add(actualz);

                var t1 = new Tuple<float, float>(disp_x, disp_y);
                var t2 = new Tuple<Tuple<float, float>, float>(t1, z_depth);
                previous_dist_rightCam.Add(t2);

                //Debug.Log("avg dusp of drone  " + i + " " + disp_x + " " + disp_y + " " + z_depth);

            }

            startit = false;
        }
        else
        {
            List<Tuple<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>, List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>>> depth_Weight_obstacle = computeDepth();
        
            current_dist_leftCam.Clear();
            current_dist_rightCam.Clear();

            int camera_num = 0;  //LEFT CAMERA
            Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>> depth_WeightLeft = depth_Weight_obstacle[camera_num].Item1;
            List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>> obstacleLeft = depth_Weight_obstacle[camera_num].Item2;

            for (int i = 0; i < depth_WeightLeft.Item1.Count; i++)
            {

                int pix_x = depth_WeightLeft.Item1[i].Item1.Item2;
                int pix_y = depth_WeightLeft.Item1[i].Item1.Item1;
                float z_depth = depth_WeightLeft.Item1[i].Item2;

                float disp_x = 0;
                float disp_y = 0;

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

                var t1 = new Tuple<float, float>(actualx, disp_y);
                var t2 = new Tuple<Tuple<float, float>, float>(t1, actualz);
                current_dist_leftCam.Add(t2);
            }

            for (int p = 0; p < obstacleLeft.Count; p++)
            {
                int minpix_x = obstacleLeft[p].Item1.Item1.Item1;
                int minpix_y = obstacleLeft[p].Item1.Item1.Item2;
                int maxpix_x = obstacleLeft[p].Item1.Item2.Item1;
                int maxpix_y = obstacleLeft[p].Item1.Item2.Item2;
                float z_depth = obstacleLeft[p].Item2;

                float mindisp_x = 0;
                float mindisp_y = 0;
                float maxdisp_x = 0;
                float maxdisp_y = 0;

                // Y-Axis
                //For min pixels
                if (minpix_y > 128)
                {
                    float angle = (minpix_y - 128) * 50 / 128f;
                    mindisp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);

                }
                else if (minpix_y < 130)
                {
                    float angle = (128 - minpix_y) * 50 / 128f;
                    mindisp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                //For max pixels
                if (maxpix_y > 128)
                {
                    float angle = (maxpix_y - 128) * 50 / 128f;
                    maxdisp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);

                }
                else if (maxpix_y < 130)
                {
                    float angle = (128 - maxpix_y) * 50 / 128f;
                    maxdisp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }

                // X-Axis
                //For min pixels
                if (minpix_x < 128)
                {
                    float angle = (128 - minpix_x) * 50 / 128f;
                    mindisp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                else if (minpix_x > 130)
                {
                    float angle = (minpix_x - 128) * 50 / 128f;
                    mindisp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                float actual_mindisp_x = mindisp_x * Mathf.Cos(halfAngle[0] * Mathf.PI / 180) - z_depth * Mathf.Sin(halfAngle[0] * Mathf.PI / 180);
                // For max pixels
                if (maxpix_x < 128)
                {
                    float angle = (128 - maxpix_x) * 50 / 128f;
                    maxdisp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                    //angle1 = angle;
                }
                else if (maxpix_x > 130)
                {
                    float angle = (maxpix_x - 128) * 50 / 128f;
                    maxdisp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                    //angle1 = angle;
                }
                float actual_maxdisp_x = maxdisp_x * Mathf.Cos(halfAngle[0] * Mathf.PI / 180) - z_depth * Mathf.Sin(halfAngle[0] * Mathf.PI / 180);

                float actualz = mindisp_x * Mathf.Sin(halfAngle[0] * Mathf.PI / 180) + z_depth * Mathf.Cos(halfAngle[0] * Mathf.PI / 180);

                var t1 = new Tuple<float, float>(actual_mindisp_x, mindisp_y);
                var t2 = new Tuple<float, float>(actual_maxdisp_x, maxdisp_y);
                var t3 = new Tuple<Tuple<float, float>, Tuple<float, float>>(t1, t2);
                var boundb = new Tuple<Tuple<Tuple<float, float>, Tuple<float, float>>, float>(t3, actualz);
                obstacleAtLeft.Add(boundb);
            }

            //Obstacle left
            float horizontalForce = 0;
            float verticalForce = 0;
            for (int i = 0; i < obstacleAtLeft.Count; i++)
            {
                if (!outOfCriticalRegionleft(obstacleAtLeft[i]))
                {
                    if(Mathf.Abs(obstacleAtLeft[i].Item1.Item1.Item1) < 8)
                    {
                        horizontalForce += 1;
                    }
                    if (obstacleAtLeft[i].Item1.Item1.Item2 < 0 && obstacleAtLeft[i].Item1.Item1.Item2 > -8)
                    {
                        verticalForce += 1;
                    }
                    if (obstacleAtLeft[i].Item1.Item1.Item2 > 0 && obstacleAtLeft[i].Item1.Item1.Item2 < 8)
                    {
                        verticalForce += -1;
                    }
                }
            }
            Vector3 obstacleAvoidance_left = new Vector3(horizontalForce, verticalForce, 0);


            camera_num += 1; //RIGHT CAMERA
            Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>> depth_WeightRight = depth_Weight_obstacle[camera_num].Item1;
            List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>> obstacleRight = depth_Weight_obstacle[camera_num].Item2;

            for (int i = 0; i < depth_WeightRight.Item1.Count; i++)
            {

                int pix_x = depth_WeightRight.Item1[i].Item1.Item2;
                int pix_y = depth_WeightRight.Item1[i].Item1.Item1;
                float z_depth = depth_WeightRight.Item1[i].Item2;
                float disp_x = 0;
                float disp_y = 0;

                //RIGHT
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

                var t1 = new Tuple<float, float>(actualx, disp_y);
                var t2 = new Tuple<Tuple<float, float>, float>(t1, actualz);
                current_dist_rightCam.Add(t2);
            }

            for (int p = 0; p < obstacleRight.Count; p++)
            {
                int minpix_x = obstacleRight[p].Item1.Item1.Item1;
                int minpix_y = obstacleRight[p].Item1.Item1.Item2;
                int maxpix_x = obstacleRight[p].Item1.Item2.Item1;
                int maxpix_y = obstacleRight[p].Item1.Item2.Item2;
                float z_depth = obstacleRight[p].Item2;

                float mindisp_x = 0;
                float mindisp_y = 0;
                float maxdisp_x = 0;
                float maxdisp_y = 0;

                // Y-Axis
                //For min pixels
                if (minpix_y > 128)
                {
                    float angle = (minpix_y - 128) * 50 / 128f;
                    mindisp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);

                }
                else if (minpix_y < 130)
                {
                    float angle = (128 - minpix_y) * 50 / 128f;
                    mindisp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                //For max pixels
                if (maxpix_y > 128)
                {
                    float angle = (maxpix_y - 128) * 50 / 128f;
                    maxdisp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);

                }
                else if (maxpix_y < 130)
                {
                    float angle = (128 - maxpix_y) * 50 / 128f;
                    maxdisp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }

                // X-Axis
                //For min pixels
                if (minpix_x < 128)
                {
                    float angle = (128 - minpix_x) * 50 / 128f;
                    mindisp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                else if (minpix_x > 130)
                {
                    float angle = (minpix_x - 128) * 50 / 128f;
                    mindisp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                float actual_mindisp_x = mindisp_x * Mathf.Cos(halfAngle[0] * Mathf.PI / 180) - z_depth * Mathf.Sin(halfAngle[0] * Mathf.PI / 180);
                // For max pixels
                if (maxpix_x < 128)
                {
                    float angle = (128 - maxpix_x) * 50 / 128f;
                    maxdisp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                    //angle1 = angle;
                }
                else if (maxpix_x > 130)
                {
                    float angle = (maxpix_x - 128) * 50 / 128f;
                    maxdisp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                    //angle1 = angle;
                }
                float actual_maxdisp_x = maxdisp_x * Mathf.Cos(halfAngle[0] * Mathf.PI / 180) - z_depth * Mathf.Sin(halfAngle[0] * Mathf.PI / 180);

                float actualz = mindisp_x * Mathf.Sin(halfAngle[0] * Mathf.PI / 180) + z_depth * Mathf.Cos(halfAngle[0] * Mathf.PI / 180);

                var t1 = new Tuple<float, float>(actual_mindisp_x, mindisp_y);
                var t2 = new Tuple<float, float>(actual_maxdisp_x, maxdisp_y);
                var t3 = new Tuple<Tuple<float, float>, Tuple<float, float>>(t1, t2);
                var boundb = new Tuple<Tuple<Tuple<float, float>, Tuple<float, float>>, float>(t3, actualz);
                obstacleAtRight.Add(boundb);
            }

            //Obstacle right
            horizontalForce = 0;
            verticalForce = 0;
            for (int i = 0; i < obstacleAtRight.Count; i++)
            {
                if (!outOfCriticalRegionleft(obstacleAtRight[i]))
                {
                    if (Mathf.Abs(obstacleAtRight[i].Item1.Item1.Item1) < 8)
                    {
                        horizontalForce -= 1;
                    }
                    if (obstacleAtRight[i].Item1.Item1.Item2 < 0 && obstacleAtRight[i].Item1.Item1.Item2 > -8)
                    {
                        verticalForce += 1;
                    }
                    if (obstacleAtRight[i].Item1.Item1.Item2 > 0 && obstacleAtRight[i].Item1.Item1.Item2 < 8)
                    {
                        verticalForce += -1;
                    }
                }
            }
            Vector3 obstacleAvoidance_right = new Vector3(horizontalForce, verticalForce, 0);


            Vector3 droneVel_left = Vector3.zero;
            Vector3 droneVel_right = Vector3.zero;

            //Find a force through Reynold's algorithm only when we have equal number of drones in the previous and the current frame
            //Left force

            if (current_dist_leftCam.Count == previous_dist_leftCam.Count && current_dist_leftCam.Count > 0)
            {

                //Rule 1 : Seperation
                Vector3 seperation = Vector3.zero;
                int neighbours = 0;
                for (int i = 0; i < current_dist_leftCam.Count; i++)
                {
                    //Vector3 direction = this.transform.position - g.transform.position;
                    Vector3 direction = -(new Vector3(current_dist_leftCam[i].Item1.Item1, current_dist_leftCam[i].Item1.Item2, current_dist_leftCam[i].Item2));
                    float distance = direction.sqrMagnitude;
                    direction.Normalize();
                    //float distance = Vector3.Distance(g.transform.position, this.transform.position);
                    direction = direction / distance;
                    seperation += direction;
                    neighbours += 1;
                }
                seperation /= neighbours;

                //Rule 2 : Alignment
                Vector3 alignment = Vector3.zero;
                for (int i = 0; i < current_dist_leftCam.Count; i++)
                {
                    Vector3 vel = new Vector3(current_dist_leftCam[i].Item1.Item1 - previous_dist_leftCam[i].Item1.Item1,
                                              current_dist_leftCam[i].Item1.Item2 - previous_dist_leftCam[i].Item1.Item2,
                                              current_dist_leftCam[i].Item2 - previous_dist_leftCam[i].Item2);
                    alignment += vel;
                }
                alignment /= neighbours;

                //Rule 3 : Cohesion
                Vector3 cohesion = Vector3.zero;
                for (int i = 0; i < current_dist_leftCam.Count; i++)
                {
                    Vector3 pos = new Vector3(current_dist_leftCam[i].Item1.Item1, current_dist_leftCam[i].Item1.Item2, current_dist_leftCam[i].Item2);
                    cohesion += pos;

                }
                cohesion /= neighbours;

                droneVel_left = r1 * seperation + r2 * alignment + r3 * cohesion + r4* obstacleAvoidance_right;
                //Debug.Log(droneVel_front.x + "  " + droneVel_front.y + "  " + droneVel_front.z);
            }

            float delta = 0;
            //Force from the left obstacle
            for(int i=0; i<obstacleAtLeft.Count; i++)
            {
                float mindisp_x = obstacleAtLeft[i].Item1.Item1.Item1;
                float mindisp_y = obstacleAtLeft[i].Item1.Item1.Item2;
                float maxdisp_x = obstacleAtLeft[i].Item1.Item2.Item1;
                float maxdisp_y = obstacleAtLeft[i].Item1.Item2.Item2;
                float z_depth   = obstacleAtLeft[i].Item2;

                if(Mathf.Abs(mindisp_x) > delta && Mathf.Abs(mindisp_y) > delta)
                {
                    //Obstacle is out of range leave it
                }
                //else if()
            }
            
            //Right force
            if (current_dist_rightCam.Count == previous_dist_rightCam.Count && current_dist_rightCam.Count > 0)
            {

                //Rule 1 : Seperation
                Vector3 seperation = Vector3.zero;
                int neighbours = 0;
                for (int i = 0; i < current_dist_rightCam.Count; i++)
                {
                    //Vector3 direction = this.transform.position - g.transform.position;
                    Vector3 direction = -(new Vector3(current_dist_rightCam[i].Item1.Item1, current_dist_rightCam[i].Item1.Item2, current_dist_rightCam[i].Item2));
                    float distance = direction.sqrMagnitude;
                    direction.Normalize();
                    //float distance = Vector3.Distance(g.transform.position, this.transform.position);
                    direction = direction / distance;
                    seperation += direction;
                    neighbours += 1;
                }
                seperation /= neighbours;

                //Rule 2 : Alignment
                Vector3 alignment = Vector3.zero;
                for (int i = 0; i < current_dist_rightCam.Count; i++)
                {
                    Vector3 vel = new Vector3(current_dist_rightCam[i].Item1.Item1 - previous_dist_rightCam[i].Item1.Item1,
                                              current_dist_rightCam[i].Item1.Item2 - previous_dist_rightCam[i].Item1.Item2,
                                              current_dist_rightCam[i].Item2 - previous_dist_rightCam[i].Item2);
                    alignment += vel;
                }
                alignment /= neighbours;

                //Rule 3 : Cohesion
                Vector3 cohesion = Vector3.zero;
                for (int i = 0; i < current_dist_rightCam.Count; i++)
                {
                    Vector3 pos = new Vector3(current_dist_rightCam[i].Item1.Item1, current_dist_rightCam[i].Item1.Item2, current_dist_rightCam[i].Item2);
                    cohesion += pos;

                }
                cohesion /= neighbours;

                droneVel_right = r1 * seperation + r2 * alignment + r3 * cohesion + r4 * obstacleAvoidance_right;
                //Debug.Log(droneVel_front.x + "  " + droneVel_front.y + "  " + droneVel_front.z);
            }


            Vector3 droneVel = Vector3.zero;


            droneVel = leftWeight * droneVel_left + rightWeight * droneVel_right;
            this.transform.GetComponent<Rigidbody>().velocity = multiplier * droneVel;

            previous_dist_leftCam.Clear();
            previous_dist_rightCam.Clear();
            obstacleAtLeft.Clear();
            obstacleAtRight.Clear();

            for (int i = 0; i < current_dist_leftCam.Count; i++)
            {

                previous_dist_leftCam.Add(current_dist_leftCam[i]);
            }

            for (int i = 0; i < current_dist_rightCam.Count; i++)
            {
                previous_dist_rightCam.Add(current_dist_rightCam[i]);
            }


        }

    }
}
