using UnityEngine;

public class BirdController : MonoBehaviour
{
   [SerializeField] private float myPitchSpeed = 2f;  
   [SerializeField] private float myRollSpeed = 2f; 
   [SerializeField] private float myYawSpeed = 2f;   
   [SerializeField] private float myForwardSpeed = 5f; 
   [SerializeField] private float myMinAngle = -80f;
   [SerializeField] private float myMaxAngle = 80f;
   [SerializeField] private float upperAngleThreshold = 20f;
   [SerializeField] private float lowerAngleThreshold = 20f;
   [SerializeField] private bool momentumEnabled;
   [SerializeField] private InputManager inputManager;
   
   private float currentSpeed;
   private Rigidbody myRigidbody;
   private float momentumFactor;
   private Vector3 forwardMovement;
   private float yaw;
   
   void Start()
   {
      myRigidbody = GetComponent<Rigidbody>();
   }

   void Update()
   {
      HandleFlightInput();
      ClampRotation();
   }

   private void HandleFlightInput()
   {
      Vector2 moveInput = inputManager.moveDirection;

      transform.Rotate(moveInput.y * myPitchSpeed,   moveInput.x * myRollSpeed, -yaw * myYawSpeed, Space.Self);
      
      if (momentumEnabled)
         CalculateMomentum();
      
      currentSpeed = myForwardSpeed + (myForwardSpeed - 10) * momentumFactor;
      forwardMovement = transform.forward * currentSpeed;
      
      myRigidbody.MovePosition(transform.position + forwardMovement * Time.deltaTime);
   }

   private void CalculateMomentum()
   {
      float angle = transform.eulerAngles.x;

      if (angle < 180)
      {
         if (angle > lowerAngleThreshold)
         {
            angle *= -1f;
            momentumFactor = angle / myMinAngle;
         }
         else
         {
            momentumFactor = 0;
         }
      }
      else
      {
         if (angle < 360 - upperAngleThreshold)
         {
            angle = 360 - angle;
            momentumFactor = angle / myMaxAngle;
            momentumFactor *= -1;
         }
         else
         {
            momentumFactor = 0;
         }
      }
   }

   private void ClampRotation()
   {
      float xRotation = ClampAngle(transform.eulerAngles.x, myMinAngle, myMaxAngle);
      float zRotation = ClampAngle(transform.eulerAngles.z, myMinAngle, myMaxAngle);
      Vector3 rotation = new Vector3(xRotation, transform.eulerAngles.y, zRotation);
      transform.eulerAngles = rotation;
   }

   private float ClampAngle(float anAngle, float aStartAngle, float aDestAngle)
   {
      if (anAngle < 0f) 
         anAngle = 360 + anAngle;
      
      return anAngle > 180f ? Mathf.Max(anAngle, 360+aStartAngle) : Mathf.Min(anAngle, aDestAngle);
   }

   public Vector3 GetForwardMovement()
   {
      return forwardMovement;
   }

   public float GetMomentumFactor()
   {
      return momentumFactor;
   }

   public Vector3  GetVelocity()
   {
      return myRigidbody.velocity;
   }

   public float ForwardSpeed()
   {
      return currentSpeed;
   }

   public bool IsFast()
   {
      return currentSpeed > myForwardSpeed;
   }

   public float SpeedDifference()
   {
      return currentSpeed - myForwardSpeed;
   }

   public void SetYaw(float aValue)
   {
      yaw = aValue;
   }
}
