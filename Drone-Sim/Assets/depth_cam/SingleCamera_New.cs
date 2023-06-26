using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
using System.IO;
using System;

public class SingleCamera_New : MonoBehaviour
{
    public Shader uberReplacementShader;
    private readonly int resWidth = 256;
    private readonly int resHeight = 256;

    //To store the recent parameters of the drones
    private List<float> initial_xdist = new List<float>();
    private List<float> initial_zdist = new List<float>();
    private List<float> initial_ydist = new List<float>();

    //To store the previous parameters of the drone
    private List<Tuple<Tuple<float, float>, float>> previous_dist = new List<Tuple<Tuple<float, float>, float>>();

    //To store the current parameters of the drone
    private List<Tuple<Tuple<float, float>, float>> current_dist = new List<Tuple<Tuple<float, float>, float>>();

    private float threshold = 40f;
    static int pixelcountThreshold = 8;

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
    private int delta = 15;
    bool outOfCriticalRegion(Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float> obs)
    {
        int minpix_x = obs.Item1.Item1.Item1;
        int minpix_y = obs.Item1.Item1.Item2;
        int maxpix_x = obs.Item1.Item2.Item1;
        int maxpix_y = obs.Item1.Item2.Item2;
        float z_depth = obs.Item2;
        
        if(maxpix_x < 128-delta || minpix_x > 128+delta || maxpix_y < 128-delta || minpix_y > 128+delta)
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

    void DFS(float[][] depthMap, int row, int col, int n, int l, ref List<Tuple<int, int>> list, bool[,] visited)
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


        while (queue.Count != 0)
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
                    DFS(depthMap, i, j, n, l, ref list, visited);
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
            if(components[i].Count > 5000)
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
    Tuple<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>, List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>> computeDepth()
    {
        //List<float> depth = new List<float>();
        List<Tuple<Tuple<int, int>, float>> depth = new List<Tuple<Tuple<int, int>, float>>();
        //Stroring the number of pixels the drone has
        List<int> weights = new List<int>();
        //Storing the bounding boxes of the drones
        List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>> bounding_boxes = new List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>();
        //Debug.Log("camerapositio is " + cameraHeadTransform.transform.position);
        RenderTexture rt = new RenderTexture(resWidth, resHeight, 24);
        this.GetComponent<Camera>().targetTexture = rt;
        //GetComponent<Camera>().targetTexture = rt;
        Texture2D screenShot = new Texture2D(resWidth, resHeight, TextureFormat.RGB24, false);
        this.GetComponent<Camera>().usePhysicalProperties = true;
        this.GetComponent<Camera>().RemoveAllCommandBuffers();
        var cb = new CommandBuffer();
        cb.SetGlobalFloat("_OutputMode", (int)ReplacementMode.DepthCompressed); // @TODO: CommandBuffer is missing SetGlobalInt() method
        this.GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cb);
        this.GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeFinalPass, cb);
        this.GetComponent<Camera>().SetReplacementShader(uberReplacementShader, "");
        this.GetComponent<Camera>().backgroundColor = Color.white;
        this.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        this.GetComponent<Camera>().allowHDR = false;
        this.GetComponent<Camera>().allowMSAA = false;
        this.GetComponent<Camera>().Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, resWidth, resHeight), 0, 0);
        this.GetComponent<Camera>().targetTexture = null;
        RenderTexture.active = null; // JC: added to avoid errors
        Destroy(rt);
        byte[] bytes = screenShot.EncodeToJPG();
        if (gameObject.name == "Cam7")
        {
            string filename = ScreenShotName(resWidth, resHeight, this.gameObject.name, i);
            System.IO.File.WriteAllBytes(filename, bytes);
            i++;
        }

        //Debug.Log("positio is " + this.transform.position);
        float[][] arr = new float[256][];
        StreamWriter writetext = new StreamWriter("write"+this.gameObject.name+".txt");
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

        Tuple<List<List<Tuple<int, int>>>, List<List<Tuple<int, int>>>> components = connectedComponents(arr, 256, 256);
        List<List<Tuple<int, int>>> drones = components.Item1;
        List<List<Tuple<int, int>>> obstacles = components.Item2;
        
        for (int i = 0; i < drones.Count; i++)
        {
            float min_depth = 999f;
            int drone_row = 0;
            int drone_col = 0;
            for (int j = 0; j < drones[i].Count; j++)
            {
                int myRow = drones[i][j].Item1;
                int myCol = drones[i][j].Item2;
                drone_row += myRow;
                drone_col += myCol;
                if (arr[myRow][myCol] < min_depth)
                {
                    min_depth = arr[myRow][myCol];
                }
            }
            drone_row /= drones[i].Count;
            drone_col /= drones[i].Count;

            //depth.Add(min_depth);
            var tuple1 = new Tuple<int, int>(drone_row, drone_col);
            var tuple2 = new Tuple<Tuple<int, int>, float>(tuple1, min_depth);
            depth.Add(tuple2);
            weights.Add(drones[i].Count);
            //Debug.Log(drone_row + " " + drone_col + " " + min_depth);
        }
        //Debug.Log(" k is " + k);

        Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>> t1 = new Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>(depth, weights);

        for (int i = 0; i < obstacles.Count; i++)
        {
            int min_row = 999;
            int min_col = 999;
            int max_row = -999;
            int max_col = -999;
            float min_depth = 999f;
            for (int j = 0; j < obstacles[i].Count; j++)
            {
                int myRow = obstacles[i][j].Item1;
                int myCol = obstacles[i][j].Item2;
                if (myRow > max_row) { max_row = myRow; }
                if (myRow < min_row) { min_row = myRow; }
                if (myCol > max_col) { max_col = myCol; }
                if (myCol < min_col) { min_col = myCol; }
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

        Tuple<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>, List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>> answer = new Tuple<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>, List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>>(t1,bounding_boxes);
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

        Tuple<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>, List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>> initialDepth_Weight = computeDepth();

    }

    public float multiplier = 0.3f;
    public float r1 = 1f;
    public float r2 = 1.5f;
    public float r3 = 1f;
    public float r4 = 1f;

    // Update is called once per frame
    /*void Update()
    {
        avgDisplacement = Vector3.zero;
        float fov = this.GetComponent<Camera>().fieldOfView;
        float halfAngle = fov / 2;

        if (startit)
        {
            targetposition = this.transform.position;
            Tuple<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>, List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>> initialDepth_Weight_Obstacle = computeDepth();
            Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>> initialDepth_Weight = initialDepth_Weight_Obstacle.Item1;
            List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>> initialObstacle = initialDepth_Weight_Obstacle.Item2;

            for (int i = 0; i < initialDepth_Weight.Item1.Count; i++)
            {
                int pix_x = initialDepth_Weight.Item1[i].Item1.Item2;
                int pix_y = initialDepth_Weight.Item1[i].Item1.Item1;
                float z_depth = initialDepth_Weight.Item1[i].Item2;
                float disp_x = 0;
                float disp_y = 0;

                //LEFT
                if (pix_x < 128)
                {
                    float angle = (128 - pix_x) * halfAngle / 128f;
                    disp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                else if (pix_x > 130)
                {
                    float angle = (pix_x - 128) * halfAngle / 128f;
                    disp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }

                //UP
                if (pix_y > 128)
                {
                    float angle = (pix_y - 128) * halfAngle / 128f;
                    disp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                //DOWN
                else if (pix_y < 130)
                {
                    float angle = (128 - pix_y) * halfAngle / 128f;
                    disp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                initial_xdist.Add(disp_x);
                initial_ydist.Add(disp_y);
                initial_zdist.Add(z_depth);

                var t1 = new Tuple<float, float>(disp_x, disp_y);
                var t2 = new Tuple<Tuple<float, float>, float>(t1, z_depth);
                previous_dist.Add(t2);

                //Debug.Log("avg dusp of drone  " + i + " " + disp_x + " " + disp_y + " " + z_depth);
            }

            startit = false;
        }
        else
        {
            Tuple<Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>, List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>>> depth_Weight_Obstacle = computeDepth();
            Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>> depth_weight = depth_Weight_Obstacle.Item1;
            List<Tuple<Tuple<Tuple<int, int>, Tuple<int, int>>, float>> obstacle = depth_Weight_Obstacle.Item2;

            int sum_of_weights = 0;
            current_dist.Clear();

            //avgDisplacement = Vector3.zero;
            for (int i = 0; i < depth_weight.Item1.Count; i++)
            {
                int pix_x = depth_weight.Item1[i].Item1.Item2;
                int pix_y = depth_weight.Item1[i].Item1.Item1;
                float z_depth = depth_weight.Item1[i].Item2;
                float disp_x = 0;
                float disp_y = 0;

                //LEFT
                if (pix_x < 128)
                {
                    float angle = (128 - pix_x) * halfAngle / 128f;
                    disp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                else if (pix_x > 130)
                {
                    float angle = (pix_x - 128) * halfAngle / 128f;
                    disp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }

                //UP
                if (pix_y > 128)
                {
                    float angle = (pix_y - 128) * halfAngle / 128f;
                    disp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                //DOWN
                else if (pix_y < 130)
                {
                    float angle = (128 - pix_y) * halfAngle / 128f;
                    disp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                //avgDisplacement += (new Vector3(disp_x - initial_xdist[i], disp_y - initial_ydist[i], z_depth - initial_zdist[i])) * depth_weight.Item2[i];

                var t1 = new Tuple<float, float>(disp_x, disp_y);
                var t2 = new Tuple<Tuple<float, float>, float>(t1, z_depth);
                current_dist.Add(t2);

                sum_of_weights += depth_weight.Item2[i];
            }

            float horizontalForce = 0;
            float verticalForce = 0;
            for (int i = 0; i < obstacle.Count; i++)
            {
                if (!outOfCriticalRegion(obstacle[i]))
                {
                    int minpix_x = obstacle[i].Item1.Item1.Item1;
                    int minpix_y = obstacle[i].Item1.Item1.Item2;
                    int maxpix_x = obstacle[i].Item1.Item2.Item1;
                    int maxpix_y = obstacle[i].Item1.Item2.Item2;
                    float z_depth = obstacle[i].Item2;
                    if (maxpix_x < 128)
                    {
                        //Right me force lagao in the inverse ratio of (128 - maxpix_x)
                        horizontalForce = 1;
                    }
                    else if (minpix_x > 128)
                    {
                        //Left me force lagao in the inverse ratio of (minpix_x - 128)
                        horizontalForce = -1;
                    }
                    else
                    {
                        if (Mathf.Abs(128 - minpix_x) > Mathf.Abs(128 - maxpix_x))
                        {
                            //Right me force lagado 
                            horizontalForce = 2;
                        }
                        else if (Mathf.Abs(128 - minpix_x) < Mathf.Abs(128 - maxpix_x))
                        {
                            //Left me force lagado
                            horizontalForce = -2;
                        }
                        else
                        {
                            //Right me force lagado
                            horizontalForce = 2;
                        }
                    }

                    if (minpix_y > 128)
                    {
                        //Upward force lagado in the inverse ratio of (minpix_y - 128)
                        verticalForce = 1;
                    }
                    else if (maxpix_y < 128)
                    {
                        //Downward force lagado in the inverse ratio of (128 - maxpix_y)
                        verticalForce = -1;
                    }
                    else
                    {
                        if (Mathf.Abs(128 - minpix_y) > Mathf.Abs(128 - maxpix_y))
                        {
                            //Downward me force lagado
                            verticalForce = -1;
                        }
                        else if (Mathf.Abs(128 - minpix_y) < Mathf.Abs(128 - maxpix_y))
                        {
                            //Upward me force lagado
                            verticalForce = 1;
                        }
                        else
                        {
                            //Upward me force lagado
                            verticalForce = 1;
                        }
                    }
                }
            }
            Vector3 obstacleAvoidance = new Vector3(horizontalForce, verticalForce, 0);

            Vector3 droneVel = Vector3.zero;
            //Find a force through Reynold's algorithm only when we have equal number of drones in the previous and the current frame
            if (current_dist.Count == previous_dist.Count)
            {
                //Rule 1 : Seperation
                Vector3 seperation = Vector3.zero;
                int neighbours = 0;
                for (int i = 0; i < current_dist.Count; i++)
                {
                    //Vector3 direction = this.transform.position - g.transform.position;
                    Vector3 direction = -(new Vector3(current_dist[i].Item1.Item1, current_dist[i].Item1.Item2, current_dist[i].Item2));
                    direction.Normalize();
                    //float distance = Vector3.Distance(g.transform.position, this.transform.position);
                    float distance = direction.sqrMagnitude;
                    direction = direction / distance;
                    seperation += direction;
                    neighbours += 1;
                }
                seperation /= neighbours;

                //Rule 2 : Alignment
                Vector3 alignment = Vector3.zero;

                for (int i = 0; i < current_dist.Count; i++)
                {
                    Vector3 vel = new Vector3(current_dist[i].Item1.Item1 - previous_dist[i].Item1.Item1, current_dist[i].Item1.Item2 - previous_dist[i].Item1.Item2, current_dist[i].Item2 - previous_dist[i].Item2);
                    alignment += vel;
                }
                alignment /= neighbours;

                //Rule 3 : Cohesion
                Vector3 cohesion = Vector3.zero;
                for (int i = 0; i < current_dist.Count; i++)
                {
                    Vector3 pos = new Vector3(current_dist[i].Item1.Item1, current_dist[i].Item1.Item2, current_dist[i].Item2);
                    cohesion += pos;

                }
                cohesion /= neighbours;

                //Vector3 droneVel = Vector3.zero;
                droneVel = r1 * seperation + r2 * alignment + r3 * cohesion;
                //this.transform.parent.GetComponent<Rigidbody>().velocity = multiplier * droneVel;

                if (this.name == "Cam32")
                {

                    Debug.Log("count is " + depth_weight.Item1.Count + " " + this.name);
                    Debug.Log("Seperation is " + seperation);
                    Debug.Log("alignment is " + alignment);
                    Debug.Log("Cohesion is " + cohesion);
                    Debug.Log("obsavoid is " + obstacleAvoidance);


                }
            }
            droneVel += r4 * obstacleAvoidance;
            if (this.name == "Cam32")
                Debug.Log("Velocity is " + droneVel);
            this.transform.parent.GetComponent<Rigidbody>().velocity = multiplier * droneVel;

            previous_dist.Clear();
            for (int i = 0; i < current_dist.Count; i++)
            {
                previous_dist.Add(current_dist[i]);
            }

            //SmoothDamp
            //Vector3 velocity = Vector3.zero;
            //this.transform.parent.position = Vector3.SmoothDamp(this.transform.parent.position, this.transform.parent.position+avgDisplacement, ref velocity, 0.3f);

        }
    }*/
}
