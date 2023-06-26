using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
using System.IO;
using System;

public class ReynoldsWith_ThreeCamera : MonoBehaviour
{
    public List<GameObject> cameras;
    public Shader uberReplacementShader;
    private readonly int resWidth = 256;
    private readonly int resHeight = 256;

    //To store the recent parameters of the drones for th e front camera
    private List<float> initial_xdist_frontCam = new List<float>();
    private List<float> initial_zdist_frontCam = new List<float>();
    private List<float> initial_ydist_frontCam = new List<float>();

    //To store the recent parameters of the drones for the left camera
    private List<float> initial_xdist_leftCam = new List<float>();
    private List<float> initial_zdist_leftCam = new List<float>();
    private List<float> initial_ydist_leftCam = new List<float>();

    //To store the recent parameters of the drones for the right camera
    private List<float> initial_xdist_rightCam = new List<float>();
    private List<float> initial_zdist_rightCam = new List<float>();
    private List<float> initial_ydist_rightCam = new List<float>();


    //To store the previous parameters of the drone
    private List<Tuple<Tuple<float, float>, float>> previous_dist_frontCam = new List<Tuple<Tuple<float, float>, float>>();
    private List<Tuple<Tuple<float, float>, float>> previous_dist_leftCam = new List<Tuple<Tuple<float, float>, float>>();
    private List<Tuple<Tuple<float, float>, float>> previous_dist_rightCam = new List<Tuple<Tuple<float, float>, float>>();


    //To store the current parameters of the drone
    private List<Tuple<Tuple<float, float>, float>> current_dist_frontCam = new List<Tuple<Tuple<float, float>, float>>();
    private List<Tuple<Tuple<float, float>, float>> current_dist_leftCam = new List<Tuple<Tuple<float, float>, float>>();
    private List<Tuple<Tuple<float, float>, float>> current_dist_rightCam = new List<Tuple<Tuple<float, float>, float>>();

    private float threshold = 40f;
    static int pixelcountThreshold = 10;


    //For Time ///
    StreamWriter writetext;
    double t1 = 0, t2 = 0;
    String line = "";


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

    private int i = 1;

    List<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>> computeDepth()
    {
        List<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>> answer = new List<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>>();
        float[][] arr = new float[256][];
        List<Tuple<Tuple<int, int>, float>> depth = new List<Tuple<Tuple<int, int>, float>>();
        //Stroring the number of pixels the drone has
        List<int> weights = new List<int>();
        for (int camera_num = 0; camera_num < cameras.Count; camera_num++)
        {
            depth.Clear();
            weights.Clear();
            RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
            cameras[camera_num].GetComponent<Camera>().targetTexture = rt;
            Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
            cameras[camera_num].GetComponent<Camera>().usePhysicalProperties = true;
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
            /*Debug.Log(this.transform.name);
            if(this.transform.name == "Drone_red (29)")
            {
                Debug.Log("Here1");
                string filename = ScreenShotName(resWidth, resHeight, this.gameObject.name, camera_num);
                System.IO.File.WriteAllBytes(filename, bytes);
            }
            */
            
            for (int i = 0; i < 256; i++)
            {
                arr[i] = new float[256];
                for (int j = 0; j < 256; j++)
                {
                    arr[i][j] = screenShot.GetPixel(j, i).grayscale * cameras[camera_num].GetComponent<Camera>().farClipPlane;
                }
            }
        }
        /////////////TIME//////////////
        t2 = Time.realtimeSinceStartupAsDouble * 1000000;
        line += ((t2 - t1).ToString() + "-");
        t1 = Time.realtimeSinceStartupAsDouble * 1000000;
        for (int camera_num = 0; camera_num < cameras.Count; camera_num++)
        {

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

        writetext = new StreamWriter("Rey3Cam " + this.transform.name + ".txt");
        writetext.WriteLine("GetDepthMap-DistanceCalc-ForceVel");

        List<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>> initialDepth_Weight = computeDepth();
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

    public float frontWeight = 1f;
    public float leftWeight = 0.5f;
    public float rightWeight = 0.5f;

    // Update is called once per frame
    void Update()
    {
        avgDisplacement = Vector3.zero;
        List<float> halfAngle = new List<float>();
        for(int i=0; i<cameras.Count; i++)
        {
            float f = cameras[i].GetComponent<Camera>().fieldOfView;
            halfAngle.Add(f/2);
        }

        if (startit)
        {
            targetposition = this.transform.position;
            List<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>> initialDepth_Weight = computeDepth();

            int camera_num = 0; //FRONT CAMERA
            for (int i = 0; i < initialDepth_Weight[camera_num].Item1.Count; i++)
            {
                int pix_x = initialDepth_Weight[camera_num].Item1[i].Item1.Item2;
                int pix_y = initialDepth_Weight[camera_num].Item1[i].Item1.Item1;
                float z_depth = initialDepth_Weight[camera_num].Item1[i].Item2;
                float disp_x = 0;
                float disp_y = 0;

                //LEFT
                if (pix_x < 128)
                {
                    float angle = (128 - pix_x) * halfAngle[camera_num] / 128f;
                    disp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                else if (pix_x > 130)
                {
                    float angle = (pix_x - 128) * halfAngle[camera_num] / 128f;
                    disp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }

                //UP
                if (pix_y > 128)
                {
                    float angle = (pix_y - 128) * halfAngle[camera_num] / 128f;
                    disp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                //DOWN
                else if (pix_y < 130)
                {
                    float angle = (128 - pix_y) * halfAngle[camera_num] / 128f;
                    disp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                initial_xdist_frontCam.Add(disp_x);
                initial_ydist_frontCam.Add(disp_y);
                initial_zdist_frontCam.Add(z_depth);

                var t1 = new Tuple<float, float>(disp_x, disp_y);
                var t2 = new Tuple<Tuple<float, float>, float>(t1, z_depth);
                previous_dist_frontCam.Add(t2);

                //Debug.Log("avg dusp of drone  " + i + " " + disp_x + " " + disp_y + " " + z_depth);
            }

            /*
            camera_num += 1;  //LEFT CAMERA
            for (int i = 0; i < initialDepth_Weight[camera_num].Item1.Count; i++)
            {
                int pix_x = initialDepth_Weight[camera_num].Item1[i].Item1.Item2;
                int pix_y = initialDepth_Weight[camera_num].Item1[i].Item1.Item1;
                float z_depth = initialDepth_Weight[camera_num].Item1[i].Item2;
                float disp_x = 0;
                float disp_y = 0;

                //LEFT
                if (pix_x < 128)
                {
                    float angle = (128 - pix_x) * halfAngle[camera_num] / 128f;
                    disp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                else if (pix_x > 130)
                {
                    float angle = (pix_x - 128) * halfAngle[camera_num] / 128f;
                    disp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }

                //UP
                if (pix_y > 128)
                {
                    float angle = (pix_y - 128) * halfAngle[camera_num] / 128f;
                    disp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                //DOWN
                else if (pix_y < 130)
                {
                    float angle = (128 - pix_y) * halfAngle[camera_num] / 128f;
                    disp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                initial_xdist_leftCam.Add(disp_x);
                initial_ydist_leftCam.Add(disp_y);
                initial_zdist_leftCam.Add(z_depth);

                var t1 = new Tuple<float, float>(disp_x, disp_y);
                var t2 = new Tuple<Tuple<float, float>, float>(t1, z_depth);
                previous_dist_leftCam.Add(t2);

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

                //LEFT
                if (pix_x < 128)
                {
                    float angle = (128 - pix_x) * halfAngle[camera_num] / 128f;
                    disp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                else if (pix_x > 130)
                {
                    float angle = (pix_x - 128) * halfAngle[camera_num] / 128f;
                    disp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }

                //UP
                if (pix_y > 128)
                {
                    float angle = (pix_y - 128) * halfAngle[camera_num] / 128f;
                    disp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                //DOWN
                else if (pix_y < 130)
                {
                    float angle = (128 - pix_y) * halfAngle[camera_num] / 128f;
                    disp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                initial_xdist_rightCam.Add(disp_x);
                initial_ydist_rightCam.Add(disp_y);
                initial_zdist_rightCam.Add(z_depth);

                var t1 = new Tuple<float, float>(disp_x, disp_y);
                var t2 = new Tuple<Tuple<float, float>, float>(t1, z_depth);
                previous_dist_rightCam.Add(t2);

                //Debug.Log("avg dusp of drone  " + i + " " + disp_x + " " + disp_y + " " + z_depth);
            }
            */

            startit = false;
        }
        else
        {
            /////////////TIME//////////////
            t1 = Time.realtimeSinceStartupAsDouble * 1000000;

            List<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>> depth_weight = computeDepth();

            current_dist_frontCam.Clear();
            current_dist_leftCam.Clear();
            current_dist_rightCam.Clear();

            int camera_num = 0;  //FRONT CAMERA

            for (int i = 0; i < depth_weight[camera_num].Item1.Count; i++)
            {
                int pix_x = depth_weight[camera_num].Item1[i].Item1.Item2;
                int pix_y = depth_weight[camera_num].Item1[i].Item1.Item1;
                float z_depth = depth_weight[camera_num].Item1[i].Item2;
                float disp_x = 0;
                float disp_y = 0;

                //LEFT
                if (pix_x < 128)
                {
                    float angle = (128 - pix_x) * halfAngle[camera_num] / 128f;
                    disp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                else if (pix_x > 130)
                {
                    float angle = (pix_x - 128) * halfAngle[camera_num] / 128f;
                    disp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }

                //UP
                if (pix_y > 128)
                {
                    float angle = (pix_y - 128) * halfAngle[camera_num] / 128f;
                    disp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                //DOWN
                else if (pix_y < 130)
                {
                    float angle = (128 - pix_y) * halfAngle[camera_num] / 128f;
                    disp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                //avgDisplacement += (new Vector3(disp_x - initial_xdist[i], disp_y - initial_ydist[i], z_depth - initial_zdist[i])) * depth_weight.Item2[i];

                var t1 = new Tuple<float, float>(disp_x, disp_y);
                var t2 = new Tuple<Tuple<float, float>, float>(t1, z_depth);
                current_dist_frontCam.Add(t2);
            }

            
            camera_num += 1; //LEFT CAMERA
            for (int i = 0; i < depth_weight[camera_num].Item1.Count; i++)
            {
                int pix_x = depth_weight[camera_num].Item1[i].Item1.Item2;
                int pix_y = depth_weight[camera_num].Item1[i].Item1.Item1;
                float z_depth = depth_weight[camera_num].Item1[i].Item2;
                /*if(this.transform.name == "Drone_red (29)")
                {
                    Debug.Log("Left depth : " + z_depth);
                }*/
                float disp_x = 0;
                float disp_y = 0;

                /*//LEFT
                if (pix_x < 128)
                {
                    float angle = (128 - pix_x) * halfAngle[camera_num] / 128f;
                    disp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                else if (pix_x > 130)
                {
                    float angle = (pix_x - 128) * halfAngle[camera_num] / 128f;
                    disp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }

                //UP
                if (pix_y > 128)
                {
                    float angle = (pix_y - 128) * halfAngle[camera_num] / 128f;
                    disp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                //DOWN
                else if (pix_y < 130)
                {
                    float angle = (128 - pix_y) * halfAngle[camera_num] / 128f;
                    disp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }*/
                //avgDisplacement += (new Vector3(disp_x - initial_xdist[i], disp_y - initial_ydist[i], z_depth - initial_zdist[i])) * depth_weight.Item2[i];

                var t1 = new Tuple<float, float>(disp_x, disp_y);
                var t2 = new Tuple<Tuple<float, float>, float>(t1, z_depth);
                current_dist_leftCam.Add(t2);
            }

            camera_num += 1; //RIGHT CAMERA
            for (int i = 0; i < depth_weight[camera_num].Item1.Count; i++)
            {
                int pix_x = depth_weight[camera_num].Item1[i].Item1.Item2;
                int pix_y = depth_weight[camera_num].Item1[i].Item1.Item1;
                float z_depth = depth_weight[camera_num].Item1[i].Item2;
                /*if (this.transform.name == "Drone_red (29)")
                {
                    Debug.Log("Right depth : " + z_depth);
                }*/
                float disp_x = 0;
                float disp_y = 0;

                /*//LEFT
                if (pix_x < 128)
                {
                    float angle = (128 - pix_x) * halfAngle[camera_num] / 128f;
                    disp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                else if (pix_x > 130)
                {
                    float angle = (pix_x - 128) * halfAngle[camera_num] / 128f;
                    disp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }

                //UP
                if (pix_y > 128)
                {
                    float angle = (pix_y - 128) * halfAngle[camera_num] / 128f;
                    disp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                //DOWN
                else if (pix_y < 130)
                {
                    float angle = (128 - pix_y) * halfAngle[camera_num] / 128f;
                    disp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }*/
                //avgDisplacement += (new Vector3(disp_x - initial_xdist[i], disp_y - initial_ydist[i], z_depth - initial_zdist[i])) * depth_weight.Item2[i];

                var t1 = new Tuple<float, float>(disp_x, disp_y);
                var t2 = new Tuple<Tuple<float, float>, float>(t1, z_depth);
                current_dist_rightCam.Add(t2);
            }

            /////////////TIME//////////////
            t2 = Time.realtimeSinceStartupAsDouble * 1000000;
            line += ((t2 - t1).ToString() + "-");
            t1 = Time.realtimeSinceStartupAsDouble * 1000000;



            Vector3 droneVel_front = Vector3.zero;
            Vector3 droneVel_left = Vector3.zero;
            Vector3 droneVel_right = Vector3.zero;

            //Find a force through Reynold's algorithm only when we have equal number of drones in the previous and the current frame
            //Front force
            if (current_dist_frontCam.Count == previous_dist_frontCam.Count && current_dist_frontCam.Count > 0)
            {
                //Rule 1 : Seperation
                Vector3 seperation = Vector3.zero;
                int neighbours = 0;
                for (int i = 0; i < current_dist_frontCam.Count; i++)
                {
                    //Vector3 direction = this.transform.position - g.transform.position;
                    Vector3 direction = -(new Vector3(current_dist_frontCam[i].Item1.Item1, current_dist_frontCam[i].Item1.Item2, current_dist_frontCam[i].Item2));
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
                for (int i = 0; i < current_dist_frontCam.Count; i++)
                {
                    Vector3 vel = new Vector3(current_dist_frontCam[i].Item1.Item1 - previous_dist_frontCam[i].Item1.Item1,
                                              current_dist_frontCam[i].Item1.Item2 - previous_dist_frontCam[i].Item1.Item2, 
                                              current_dist_frontCam[i].Item2 - previous_dist_frontCam[i].Item2);
                    alignment += vel;
                }
                alignment /= neighbours;

                //Rule 3 : Cohesion
                Vector3 cohesion = Vector3.zero;
                for (int i = 0; i < current_dist_frontCam.Count; i++)
                {
                    Vector3 pos = new Vector3(current_dist_frontCam[i].Item1.Item1, current_dist_frontCam[i].Item1.Item2, current_dist_frontCam[i].Item2);
                    cohesion += pos;

                }
                cohesion /= neighbours;

                droneVel_front = r1 * seperation + r2 * alignment + r3 * cohesion;
                //Debug.Log(droneVel_front.x + "  " + droneVel_front.y + "  " + droneVel_front.z);
            }

            
            //Find a force through Reynold's algorithm only when we have equal number of drones in the previous and the current frame
            //Force from left
            if (current_dist_leftCam.Count > 0)
            {
                //Rule 1 : Seperation
                Vector3 seperation = Vector3.zero;
                int neighbours = 0;
                for (int i = 0; i < current_dist_leftCam.Count; i++)
                {
                    Vector3 direction = new Vector3(current_dist_leftCam[i].Item2, 0, 0);
                    float distance = direction.sqrMagnitude;
                    if (this.transform.name == "Drone_red (33)")
                    {
                        Debug.Log("Left direction : " + direction);
                    }
                    direction.Normalize();
                    direction = direction / distance;
                    seperation += direction;
                    neighbours += 1;
                }
                seperation /= neighbours;

                //Rule 3 : Cohesion
                Vector3 cohesion = Vector3.zero;
                for (int i = 0; i < current_dist_frontCam.Count; i++)
                {
                    Vector3 pos = new Vector3(current_dist_frontCam[i].Item1.Item1, current_dist_frontCam[i].Item1.Item2, current_dist_frontCam[i].Item2);
                    cohesion += pos;

                }
                cohesion /= neighbours;

                droneVel_left = r5 * seperation + r7 * cohesion;

            }

            //Find a force through Reynold's algorithm only when we have equal number of drones in the previous and the current frame
            //Force from right
            if (current_dist_rightCam.Count > 0)
            {
                //Rule 1 : Seperation
                Vector3 seperation = Vector3.zero;
                int neighbours = 0;
                for (int i = 0; i < current_dist_rightCam.Count; i++)
                {
                    Vector3 direction = -(new Vector3(current_dist_rightCam[i].Item2, 0, 0));
                    float distance = direction.sqrMagnitude;
                    if (this.transform.name == "Drone_red (33)")
                    {
                        Debug.Log("Right direction : " + direction);
                    }
                    direction.Normalize();
                    direction = direction / distance;
                    seperation += direction;
                    neighbours += 1;
                }
                seperation /= neighbours;

                //Rule 3 : Cohesion
                Vector3 cohesion = Vector3.zero;
                for (int i = 0; i < current_dist_frontCam.Count; i++)
                {
                    Vector3 pos = new Vector3(current_dist_frontCam[i].Item1.Item1, current_dist_frontCam[i].Item1.Item2, current_dist_frontCam[i].Item2);
                    cohesion += pos;

                }
                cohesion /= neighbours;

                droneVel_right = r6 * seperation + r8 * cohesion;
            }
            

            Vector3 droneVel = Vector3.zero;
            if (this.transform.name == "Drone_red (33)")
            {
                Debug.Log("Front : " + droneVel_front);
                Debug.Log("Left : " + droneVel_left);
                Debug.Log("Right : " + droneVel_right);
            }
            droneVel = frontWeight * droneVel_front + leftWeight * droneVel_left + rightWeight * droneVel_right;
            this.transform.GetComponent<Rigidbody>().velocity = multiplier * droneVel;

            /////////////TIME//////////////
            t2 = Time.realtimeSinceStartupAsDouble * 1000000;
            line += ((t2 - t1).ToString());
            writetext.WriteLine(line);
            line = "";

            previous_dist_frontCam.Clear();
            /*previous_dist_leftCam.Clear();
            previous_dist_rightCam.Clear();*/

            for (int i = 0; i < current_dist_frontCam.Count; i++)
            {
                previous_dist_frontCam.Add(current_dist_frontCam[i]);
            }
            /*for (int i = 0; i < current_dist_leftCam.Count; i++)
            {
                previous_dist_frontCam.Add(current_dist_leftCam[i]);
            }
            for (int i = 0; i < current_dist_rightCam.Count; i++)
            {
                previous_dist_frontCam.Add(current_dist_rightCam[i]);
            }*/
        }

    }
}
