using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generator : MonoBehaviour
{
    //public int follower_count =0 ;
    public GameObject boxToGenerate;
    private int i = 0;
    private int j = 0;
    private int k = 0;
    //public GameObject follower_drone_id;
    // Start is called before the first frame update
    void Start()
    {
      for(int l =1; l <= 5; l+=1)
      {
          Instantiate(boxToGenerate, new Vector3(i,j,k), new Quaternion(0, 0, 0, 0));
          i += 3;
          j += 3;
      }
    }

    // Update is called once per frame
    void Update()
    {
      // if(Input.GetKeyDown(KeyCode.Space))
      //   {
      //       // instantiate the box object
      //       Instantiate(boxToGenerate, new Vector3(i,j,k), new Quaternion(0, 0, 0, 0));
      //       i += 1;
      //       j += 1;
      //
      //   }
    }
}
