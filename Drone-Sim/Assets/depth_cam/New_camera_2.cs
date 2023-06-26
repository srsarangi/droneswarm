using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
using System.IO;
using System;

public class New_camera_2 : MonoBehaviour
{
    public Shader uberReplacementShader;
    private readonly int resWidth = 256;
    private readonly int resHeight = 256;
    private List<float> initial_xdist = new List<float>();
    private List<float> initial_zdist = new List<float>();
    private List<float> initial_ydist = new List<float>();
    private float threshold = 40f;
    static int pixelcountThreshold = 25;

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
        //return connectedComp;
    }
    private int i = 1;
    Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>> computeDepth()
    {
        //List<float> depth = new List<float>();
        List<Tuple<Tuple<int, int>, float>> depth = new List<Tuple<Tuple<int, int>, float>>();
        //Stroring the number of pixels the drone has
        List<int> weights = new List<int>();
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
        
        float[][] arr = new float[256][];
        //StreamWriter writetext = new StreamWriter("write"+this.gameObject.name+".txt");
        for (int i = 0; i < 256; i++)
        {
            //string line = "";
            arr[i] = new float[256];
            for (int j = 0; j < 256; j++)
            {
                arr[i][j] = screenShot.GetPixel(j, i).grayscale * 40;
                //line += (arr[i][j].ToString() + " ");
            }
            //writetext.WriteLine(line);           
        }

        /////////////TIME//////////////

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
        Debug.Log(" k is " + k);
        Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>> answer = new Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>>(depth, weights);

        return answer;
    }


    /*private string ScreenShotName(int width, int height, string s, int i)
    {
        Debug.Log("screenshot" + s + i + ".jpeg");
        return string.Format("{0}/screenshot" + s + i + ".jpeg",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }*/


    // Start is called before the first frame update
    void Start()
    {
        if (!uberReplacementShader)
            uberReplacementShader = Shader.Find("Hidden/UberReplacement");


        Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>> initialDepth_Weight = computeDepth();

    }

    /*void Update()
    {
        initialDepth = computeDepth();
    }*/


    // Update is called once per frame
    void Update()
    {
        float fov = this.GetComponent<Camera>().fieldOfView;
        avgDisplacement = Vector3.zero;
        if (startit)
        {
            targetposition = this.transform.position;
            Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>> initialDepth_Weight = computeDepth();
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
                    float angle = (128 - pix_x) * fov / 256f;
                    disp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                else if (pix_x > 130)
                {
                    float angle = (pix_x - 128) * fov / 256f;
                    disp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }

                //UP
                if (pix_y > 128)
                {
                    float angle = (pix_y - 128) * fov / 256f;
                    disp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                //DOWN
                else if (pix_y < 130)
                {
                    float angle = (128 - pix_y) * fov / 256f;
                    disp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                }
                initial_xdist.Add(disp_x);
                initial_ydist.Add(disp_y);
                initial_zdist.Add(z_depth);
                Debug.Log("avg dusp of drone  " + i + " " + disp_x + " " + disp_y + " " + z_depth);
            }

            startit = false;
        }
        else
        {
            /////////////TIME//////////////
            Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>> depth_weight = computeDepth();
            int sum_of_weights = 0;
            //avgDisplacement = Vector3.zero;
            
            if(depth_weight.Item1.Count == initial_xdist.Count)
            {
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
                        float angle = (128 - pix_x) * fov / 256f;
                        disp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                    }
                    else if (pix_x > 130)
                    {
                        float angle = (pix_x - 128) * fov / 256f;
                        disp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                    }

                    //UP
                    if (pix_y > 128)
                    {
                        float angle = (pix_y - 128) * fov / 256f;
                        disp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                    }
                    //DOWN
                    else if (pix_y < 130)
                    {
                        float angle = (128 - pix_y) * fov / 256f;
                        disp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                    }
                    avgDisplacement += (new Vector3(disp_x - initial_xdist[i], disp_y - initial_ydist[i], z_depth - initial_zdist[i])) * depth_weight.Item2[i];
                    sum_of_weights += depth_weight.Item2[i];
                }

                if (depth_weight.Item1.Count == 0)
                {
                    avgDisplacement = Vector3.zero;
                }
                else
                {
                    if (sum_of_weights != 0)
                        avgDisplacement /= sum_of_weights;
                }
                Debug.Log("avg dusp " + avgDisplacement);

                this.transform.parent.position = this.transform.parent.position + avgDisplacement;
                Vector3 velocity = Vector3.zero;
                //this.transform.parent.position = Vector3.SmoothDamp(this.transform.parent.position, this.transform.parent.position+avgDisplacement, ref velocity, 0.3f);

            }
            else
            {
                initial_xdist.Clear();
                initial_ydist.Clear();
                initial_zdist.Clear();
                Tuple<List<Tuple<Tuple<int, int>, float>>, List<int>> initialDepth_Weight = computeDepth();
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
                        float angle = (128 - pix_x) * fov / 256f;
                        disp_x = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                    }
                    else if (pix_x > 130)
                    {
                        float angle = (pix_x - 128) * fov / 256f;
                        disp_x = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                    }

                    //UP
                    if (pix_y > 128)
                    {
                        float angle = (pix_y - 128) * fov / 256f;
                        disp_y = (Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                    }
                    //DOWN
                    else if (pix_y < 130)
                    {
                        float angle = (128 - pix_y) * fov / 256f;
                        disp_y = -(Mathf.Tan(angle * Mathf.PI / 180) * z_depth);
                    }
                    initial_xdist.Add(disp_x);
                    initial_ydist.Add(disp_y);
                    initial_zdist.Add(z_depth);
                    Debug.Log("avg dusp of drone  " + i + " " + disp_x + " " + disp_y + " " + z_depth);
                }
            }
        }
        /////////////TIME//////////////
    }
}