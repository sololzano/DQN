using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;
using UnityEngine.UI;

public class Avoid_Obstacles : Agent {

    // Public objects
    public Transform[] sensor = new Transform[5];
    public GameObject[] obstacle = new GameObject[4];
    public GameObject goal;
    public bool isTraining;
    public Text txtDistance, txtPosition, txtReward;

    // Useful variables
    RaycastHit[] hit = new RaycastHit[5];
    float rayLength = 5.0f;
    Vector3 Pos0; 
    Quaternion Rot0;
    float tReward;
    float lambda = -0.005f;
    bool goingRight = false;
    bool goingRight2 = true;
   
    // Start is called before the first frame update
    void Start() {
        
    }
    
    // Agent initialization 
    public override void InitializeAgent() {
        tReward = 0.0f;
        Pos0 = this.transform.localPosition;
        Rot0 = this.transform.rotation;
    }

    // Delta X, Delta Z and Facing angle
    public override void CollectObservations() {
        float DeltaX = this.transform.position.x - goal.transform.position.x;
        float DeltaZ = this.transform.position.z - goal.transform.position.z;
        if (DeltaZ > 0.0f) {
            DeltaZ = DeltaZ * -1.0f;
        }
        if (DeltaX > 0.0f) {
            DeltaZ = DeltaZ * -1.0f;
        }
        // Get facing angle
        Vector3 targetDir = goal.transform.position - this.transform.position;
        float facingAngle = Vector3.Angle(targetDir, transform.forward);
        AddVectorObs(DeltaX);
        AddVectorObs(DeltaZ);
        AddVectorObs(facingAngle);
        for (int i = 0; i < hit.Length; i++) {
            Physics.Raycast(sensor[i].position, sensor[i].forward, out hit[i], rayLength);
            AddVectorObs(hit[i].distance);
        }
    }

    // Agent action
    public override void AgentAction(float[] vectorAction) {
        int actionIdx = (int) vectorAction[0];

        AddReward(-0.05f);
        tReward -= 0.05f;
        switch(actionIdx) {
            case 0:
                this.transform.Translate(0, 0, 0.1f);
                break;
            case 1:
                this.transform.Rotate(0, 5f, 0);
                break;
            case 2:
                this.transform.Rotate(0, -5f, 0);
                break;
            case 3:
                // Do nothing
                break;
        }

        // Assign reward based on distance and angle
        float distToGoal = Vector3.Distance(this.transform.position, goal.transform.position);
        Vector3 targetDir = goal.transform.position - this.transform.position;
        float facingAngle = Vector3.SignedAngle(targetDir, transform.forward, Vector3.up);
        if (distToGoal < 1.0f) {
            AddReward(20.0f);
            tReward += 20.0f;
            Done();
        } else {
            float reward = 2/(1 + Mathf.Exp(-2 * - distToGoal)) - 1;
            if (facingAngle < 0.0f) {
                facingAngle += 180.0f;
            }
            float nAngle = lambda * facingAngle;
            float angleReward = 2/(1 + Mathf.Exp(-2 * nAngle)) - 1;
            reward += lambda * Mathf.Abs(facingAngle - 180.0f);
            tReward += reward;
            AddReward(reward);
        }

        // Check if agent is out of boundaries and let it roam freely
        if (this.transform.localPosition.x < -2.0 || this.transform.localPosition.x > 2.0 ||  
        this.transform.localPosition.z < -2.0 || this.transform.localPosition.z > 2.0) { 
            print("Out of boundaries");
            AddReward(-10.0f); 
            tReward -= 10.0f;
        }

        // Print observations in TextBox
        float DeltaX = this.transform.position.x - goal.transform.position.x;
        float DeltaZ = this.transform.position.z - goal.transform.position.z;
        txtPosition.text = DeltaX.ToString("F2") + ", " + DeltaZ.ToString("F2") + ", " + distToGoal.ToString("F4");
        txtReward.text = "R: " + tReward.ToString("F5");
        string s = " ";

        // Draw range sensors
        for(int i = 0; i < hit.Length; i++) {
            Debug.DrawLine(sensor[i].position, sensor[i].position + sensor[i].forward * rayLength, Color.red);
            Physics.Raycast(sensor[i].position, sensor[i].forward, out hit[i], rayLength);
            s = s + hit[i].distance.ToString("F2") + ", ";
        }
        // Get information of range sensors
        if (distToGoal < 1.0f) {
            txtDistance.text = "Goal.!!!";
        } else {
            txtDistance.text = s;
        }
        renderAnimation(isTraining);
    }

    // Reset agent to initial position and set goal randomly if it's not training 
    public override void AgentReset() {
        this.transform.localPosition = Pos0; 
        this.transform.rotation = Rot0; 
        obstacle[3].transform.position = new Vector3(0, 0, 2);
        goingRight = false;

        // Get randomized vector for new goal's position
        float z = Random.Range(1.0f, 3.5f);
        while ((z >= 1.5f) && (z <= 2.75f)) {
            z = Random.Range(1.0f, 3.5f);
        }
        float x = Random.Range(-3.0f, 3.0f);
        while ((x <= 1.8f) && (x >= 1.0f)) {
            x = Random.Range(-3.0f, 3.0f);
        }
        if (!isTraining) {
            goal.transform.position = new Vector3(x, 0.25f, z);
        }
        SetReward(-1.0f);
        tReward = -1.0f;
    }

    public override float[] Heuristic() {
        if (Input.GetKey(KeyCode.W)) {
            return new float[] {0};
        } else if (Input.GetKey(KeyCode.D)) {
            return new float[] {1};
        } else if (Input.GetKey(KeyCode.A)) {
            return new float[] {2};
        } else {
            return new float[] {3};
        }
    }

    /**
        Detects collision from the physics engine.
        Let the agent hit the wall and get punished.
    */
    private void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.tag == "block") {
            AddReward(-5.0f);
            tReward -= 5.0f;
        }
    }

    /**
        Simple animation of obstacles
    */
    private void renderAnimation(bool training) {
        if (!training) {
            float ox = obstacle[3].transform.position.x;
            if (goingRight) {
                if (ox > -2.0f) {
                    ox -= 0.05f;
                } else {
                    goingRight = false;
                }
            } else {
                if (ox <= 0.0f) {
                    ox += 0.05f;
                } else {
                    goingRight = true;
                }
            }
            float ox2 = obstacle[0].transform.position.x;
            if (goingRight2) {
                if (ox2 > -2.5f) {
                    ox2 -= 0.1f;
                } else {
                    goingRight2 = false;
                }
            } else {
                if (ox2 <= 2.5f) {
                    ox2 += 0.1f;
                } else {
                    goingRight2 = true;
                }
            }
            obstacle[3].transform.position = new Vector3(ox, 0, 2);
            obstacle[0].transform.position = new Vector3(ox2, 0, -1);
        }
    }

    // Update is called once per frame
    public void Update() {
        RequestDecision();
    }
}
