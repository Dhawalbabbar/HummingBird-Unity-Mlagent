using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;

public class HummingbirdAgent : Agent
{
    public float moveForce=2f;
    public float pitchSpeed=100f;
    public float yawSpeed=100f;
    public Transform beakTip;
    public Camera agentCamera;
    public bool trainingMode;
    new private Rigidbody rigidbody;//rb of agent
    private FlowerArea flowerArea;
    private Flower nearestFlower;
    private float smoothPitchChange=0f;
    private float smoothYawChange=0f;
    private const float MaxPitchAngle=80f;
    private const float BeakTipRadius=0.008f;
    private bool frozen=false;
    public float NectorObtained{get;private set;}


    public override void Initialize()
    {
        rigidbody=GetComponent<Rigidbody>();
        flowerArea=GetComponentInParent<FlowerArea>();

        if (!trainingMode)MaxStep=0;
         
    }

    public override void OnEpisodeBegin()
    {
        if (trainingMode){
            flowerArea.ResetFlowers();
        }
        NectorObtained=0f;

        rigidbody.velocity=Vector3.zero;
        rigidbody.angularVelocity=Vector3.zero;

        bool inFrontOfFlower=true;
        if(trainingMode){
            inFrontOfFlower=UnityEngine.Random.value>.5f;
        }

        MoveToSafeRandomPosition(inFrontOfFlower);

        UpdateNearestFLoaer();
    }

    public override void OnActionReceived(float[] vectorAction)
    {
        if (frozen) return;
        //cal movement vector
        Vector3 move =new Vector3(vectorAction[0],vectorAction[1],vectorAction[2]);

        //Add force in this direction
        rigidbody.AddForce(move*moveForce);

        //get curr rotation
        Vector3 rotationVector=transform.rotation.eulerAngles;

        //cal pitch and yaw
        float pitchChange=vectorAction[3];
        float yawChange=vectorAction[4];

        //Cal smooth rotation changes
        smoothPitchChange=Mathf.MoveTowards(smoothPitchChange,pitchChange,2f*Time.fixedDeltaTime);
        smoothYawChange=Mathf.MoveTowards(smoothYawChange,yawChange,2f*Time.fixedDeltaTime);

        //Cal new Pitch and jaw, clamp it
        float pitch=rotationVector.x+smoothPitchChange*Time.fixedDeltaTime*pitchSpeed;
        if(pitch>180f)pitch-=360f;
        pitch=Mathf.Clamp(pitch,-10,MaxPitchAngle);

        float yaw=rotationVector.y+smoothYawChange*Time.fixedDeltaTime*yawSpeed;

        //new rotation
        transform.rotation=Quaternion.Euler(pitch,yaw,0f);

    }

    //Cpllect vector observationfrom env
    public override void CollectObservations(VectorSensor sensor){
        if(nearestFlower==null){
            sensor.AddObservation(new float[10]);
            return;
        }
        //local rotation
        sensor.AddObservation(transform.localRotation.normalized);

        //get a vector from beek tip to nearest flower
        Vector3 toFLower=nearestFlower.FlowerCenterPosition-beakTip.position;
        //vec pointing to nearest flower
        sensor.AddObservation(toFLower.normalized);

        //obs a . product beak tip in front of flower
        sensor.AddObservation(Vector3.Dot(toFLower.normalized,-nearestFlower.FlowerUpVector.normalized));

        //. product beak toward the flower
        sensor.AddObservation(Vector3.Dot(-nearestFlower.FlowerUpVector.normalized,beakTip.forward.normalized));

        // obs relative distance
        sensor.AddObservation(toFLower.magnitude/FlowerArea.areaDiameter);
    }

    //when behaviour type is set to huristic only  thi will be called, 
    public override void Heuristic(float[] actionsOut)
    {
        //create placeholders for all mov, turning
        Vector3 forward=Vector3.zero;
        Vector3 left=Vector3.zero;
        Vector3 up=Vector3.zero;
        float pitch=0f;
        float yaw=0f;

        //convert input to turning
        if (Input.GetKey(KeyCode.W)) forward=transform.forward;
        else if(Input.GetKey(KeyCode.S)) forward=-transform.forward;

        if (Input.GetKey(KeyCode.A)) left=-transform.right;
        else if(Input.GetKey(KeyCode.D)) left=transform.right;

        if (Input.GetKey(KeyCode.E)) up=transform.up;
        else if(Input.GetKey(KeyCode.C)) up=-transform.up;

        if (Input.GetKey(KeyCode.UpArrow)) pitch=1f;
        else if(Input.GetKey(KeyCode.DownArrow)) pitch=-1f;

        if (Input.GetKey(KeyCode.LeftArrow)) yaw=-1f;
        else if(Input.GetKey(KeyCode.RightArrow)) yaw=1f;

        Vector3 combined=(forward+left+up).normalized;

        actionsOut[0]=combined.x;
        actionsOut[1]=combined.y;
        actionsOut[2]=combined.z;
        actionsOut[3]=pitch;
        actionsOut[4]=yaw;
    }


    public void FreezeAgent(){
        Debug.Assert(trainingMode==false,"Freeze/Unfreeze not supp in training");
        frozen=true;
        rigidbody.Sleep();
    }
    public void UnfreezeAgent(){
        Debug.Assert(trainingMode==false,"Freeze/Unfreeze not supp in training");
        frozen=false;
        rigidbody.WakeUp();
    }

    //called when action is received by NN or player
    private void MoveToSafeRandomPosition(bool inFrontOfFlower){
        bool safePositionFound=false;
        int attemptRemaining=100;
        Vector3 potentialPosition=Vector3.zero;
        Quaternion potentialRotation=new Quaternion();

        while(!safePositionFound && attemptRemaining>0){
            attemptRemaining--;
            if(inFrontOfFlower){
                Flower randonFLower=flowerArea.Flowers[UnityEngine.Random.Range(0,flowerArea.Flowers.Count)];
                float distanceFromFlower=UnityEngine.Random.Range(.1f,.2f);

                potentialPosition=randonFLower.transform.position+randonFLower.FlowerUpVector*distanceFromFlower;

                Vector3 toFLower=randonFLower.FlowerCenterPosition-potentialPosition;
                potentialRotation=Quaternion.LookRotation(toFLower,Vector3.up);
            }
            else{
                float height=UnityEngine.Random.Range(1.2f,2.5f);
                float radius=UnityEngine.Random.Range(2f,7f);
                Quaternion direction=Quaternion.Euler(0f,UnityEngine.Random.Range(-180f,180f),0f);
                potentialPosition=flowerArea.transform.position+Vector3.up*height+direction*Vector3.forward*radius;

                float pitch=UnityEngine.Random.Range(-60f,60f);
                float yaw=UnityEngine.Random.Range(-180f,180f);
                potentialRotation=Quaternion.Euler(pitch,yaw,0f);
            }
            Collider[] colliders=Physics.OverlapSphere(potentialPosition,0.05f);
            safePositionFound=colliders.Length==0;

        }
        Debug.Assert(safePositionFound,"Could not find safe pos to spawn");

        transform.position=potentialPosition;
        transform.rotation=potentialRotation;

    }

    private void UpdateNearestFLoaer(){
        foreach(Flower flower in flowerArea.Flowers){
            if(nearestFlower==null && flower.HasNector){
                nearestFlower=flower;
            }
            else if(flower.HasNector){
                float distanceToFlower=Vector3.Distance(flower.transform.position,beakTip.position);
                float distanceToCurrentNearestFlower=Vector3.Distance(nearestFlower.transform.position,beakTip.position);

                if(!nearestFlower.HasNector || distanceToFlower<distanceToCurrentNearestFlower) {
                    nearestFlower=flower;
                }
            }
        }
    }

    //called when agent collider enters trigger
    private void  OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    //called when agent collider steys trigger
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    //handles above
    private void TriggerEnterOrStay(Collider collider){
        //check if agent colliding with nector
        if(collider.CompareTag("nector")){
            Vector3 closestPointToBeekTip=collider.ClosestPoint(beakTip.position);

            //check if closest pos point is close to beek tip
            if (Vector3.Distance(beakTip.position,closestPointToBeekTip)<BeakTipRadius){
                Flower flower=flowerArea.GetFlowerFromNector(collider);

                float nectorRecived=flower.Feed(0.01f);

                NectorObtained+=nectorRecived;

                if(trainingMode){
                    float bonus=.02f*Mathf.Clamp01(Vector3.Dot(transform.forward.normalized,-nearestFlower.FlowerUpVector.normalized));
                    AddReward(0.01f +bonus);
                }

                //if fl is empty update nearest fl
                if(!flower.HasNector){
                    UpdateNearestFLoaer();
                }
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(trainingMode && collision.collider.CompareTag("boundry")){
            AddReward(-0.5f);
        }
    }

    private void Update()
    {
        //draw a line from beak tip to fl
        if(nearestFlower!=null){
            Debug.DrawLine(beakTip.position,nearestFlower.FlowerCenterPosition,Color.green);
        }
    }

    private void FixedUpdate() {
        if (nearestFlower!=null&&!nearestFlower.HasNector){
            UpdateNearestFLoaer();
        }
    }
}
