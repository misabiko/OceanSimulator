using UnityEngine;

public class BirdController : MonoBehaviour
{
   [SerializeField] private InputManager inputManager;

   [Header("Controller Settings")]
   [SerializeField] private float myPitchSpeed = 2f;  
   [SerializeField] private float myRollSpeed = 2f; 
   [SerializeField] private float myYawSpeed = 2f;   
   [SerializeField] private float myForwardSpeed = 5f; 
   [SerializeField] private float myMinAngle = -80f;
   [SerializeField] private float myMaxAngle = 80f;
   [SerializeField] private float upperAngleThreshold = 20f;
   [SerializeField] private float lowerAngleThreshold = 20f;
   [SerializeField] private bool momentumEnabled;
   [SerializeField] private float lowerHeightCap = 5f;
   [SerializeField] private float upperHeightCap = 200f;
   [SerializeField] private float upwardsLerpSpeed = 0.7f;
   [SerializeField] private float downwardsLerpSpeed = 0.7f;
   [SerializeField] private float decellerationLerpSpeed = 2f;

   private float currentSpeed;
   private Rigidbody myRigidbody;
   private float momentumFactor;
   private Vector3 forwardMovement;
   
   void Start()
   {
      myRigidbody = GetComponent<Rigidbody>();
   }

   void Update()
   {
      HandleFlightInput();
      ClampHeight();
      ClampRotation();
   }

   private void HandleFlightInput()
   {
      Vector2 moveInput = inputManager.moveDirection;

      transform.Rotate(moveInput.y * myPitchSpeed,   moveInput.x * myRollSpeed, -inputManager.Yaw * myYawSpeed, Space.Self);
      
      if (momentumEnabled)
         CalculateMomentum();
      
      currentSpeed = myForwardSpeed + (0.75f * myForwardSpeed) * momentumFactor;
      forwardMovement = transform.forward * currentSpeed;
      
      myRigidbody.MovePosition(transform.position + forwardMovement * Time.deltaTime);
   }

   private void CalculateMomentum()
   {
      if (transform.position.y <= lowerHeightCap+1 || transform.position.y >= upperHeightCap-1 ) 
         momentumFactor = Mathf.Lerp(momentumFactor, 0, decellerationLerpSpeed * Time.deltaTime);
      
      float angle = transform.eulerAngles.x;

      if (angle < 180)// if we go downwards
      {
         if (angle > lowerAngleThreshold)
            momentumFactor = Mathf.Lerp(momentumFactor, angle * -1 / myMinAngle, downwardsLerpSpeed * Time.deltaTime);
         else
            momentumFactor = Mathf.Lerp(momentumFactor, 0, decellerationLerpSpeed * Time.deltaTime);
      }
      else // if go upwards
      {
         if (angle < 360 - upperAngleThreshold)
            momentumFactor = Mathf.Lerp(momentumFactor, ((360 - angle) / myMaxAngle)*-1, upwardsLerpSpeed * Time.deltaTime);
         else
            momentumFactor = Mathf.Lerp(momentumFactor, 0, decellerationLerpSpeed * Time.deltaTime);
      }
   }

   private void ClampRotation()
   {
      float xRotation = ClampAngle(transform.eulerAngles.x, myMinAngle, myMaxAngle);
      float zRotation = ClampAngle(transform.eulerAngles.z, myMinAngle, myMaxAngle);
      Vector3 rotation = new Vector3(xRotation, transform.eulerAngles.y, zRotation);
      transform.eulerAngles = rotation;
   }

   private void ClampHeight()
   {
      float heightValue = Mathf.Clamp(transform.position.y, lowerHeightCap, upperHeightCap);
      transform.position = new Vector3(transform.position.x, heightValue, transform.position.z);
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
      return myRigidbody.linearVelocity;
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
}