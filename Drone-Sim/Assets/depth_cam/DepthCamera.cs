using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DepthCamera : MonoBehaviour
{
    private Camera cam;
    private RenderTexture tex;
    // Use this for initialization
    public Shader shader;

    private Material _material;
    private Material material
    {
        get
        {
            if (_material == null)
            {
                _material = new Material(shader);
                _material.hideFlags = HideFlags.HideAndDontSave;
            }
            return _material;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        tex = new RenderTexture(512, 512, 24);
        cam = GetComponent<Camera>();
        cam.SetTargetBuffers(tex.colorBuffer, tex.depthBuffer);
        cam.depthTextureMode = DepthTextureMode.Depth;
    }

    // Update is called once per frame
    void Update()
    {
        RenderTexture.active = tex;
        cam.Render();
        Graphics.Blit(tex, tex, material);

        Texture2D t = new Texture2D(tex.width, tex.height, TextureFormat.ARGB32, false);
        t.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

        t.Apply();
        RenderTexture.active = null;

        byte[] bytes = t.EncodeToJPG();
        string filename = ScreenShotName(tex.width, tex.height, "0");
        System.IO.File.WriteAllBytes(filename, bytes);
    }

    public static string ScreenShotName(int width, int height, string s)
    {
        return string.Format("{0}/screenshot" + s + ".jpeg",
                             Application.dataPath,
                             width, height,
                             System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
    }
}
