// The effect of this script is same as Drone manula movement 
// in this case the drone movemnt is made slower for better control
// The ttilt motions have been removed , so it is very basic movement script and can be used to  move anygame object
using UnityEngine;

public class DroneSimpleMove : MonoBehaviour

{
    public float m_MoveSpeed = 0f;
    public float m_RotateSpeed = 0f;
    public KeyCode m_ForwardButton = KeyCode.W;
    public KeyCode m_BackwardButton = KeyCode.S;
    public KeyCode m_RightButton = KeyCode.D;
    public KeyCode m_LeftButton = KeyCode.A;
    public KeyCode m_UpButton = KeyCode.K;
    public KeyCode m_DownButton = KeyCode.I;

    void FixedUpdate()
    {

        Rotation2();
    }

        private void Update ()
    {
        // translation
        {
            Vector3 dir = Vector3.zero;
			Move (m_ForwardButton, ref dir, transform.forward);
			Move (m_BackwardButton, ref dir, -transform.forward);
			Move (m_RightButton, ref dir, transform.right);
			Move (m_LeftButton, ref dir, -transform.right);
			Move (m_UpButton, ref dir, transform.up);
			Move (m_DownButton, ref dir, -transform.up);
			transform.position += dir * m_MoveSpeed * Time.deltaTime;
        }
        // rotation
       

    }
    // aditional suffix of 2 is added to variables so that they do not conflict with same name variables of other script
    private float wantedYRotation2;
    [HideInInspector] public float currentYRotation2;
    private float rorateAmountByKeys2 = 2.5f;
    private float rotationYVelocity2;
    void Rotation2()
    {

        if (Input.GetKey(KeyCode.J))
        {
            wantedYRotation2 -= rorateAmountByKeys2;
        }
        if (Input.GetKey(KeyCode.L))
        {
            wantedYRotation2 += rorateAmountByKeys2;
        }
        currentYRotation2 = Mathf.SmoothDamp(currentYRotation2, wantedYRotation2, ref rotationYVelocity2, 0.25f);
    }
    private void Move (KeyCode key, ref Vector3 moveTo, Vector3 dir)
    {
        if (Input.GetKey (key))
			moveTo = dir;
    }
}
