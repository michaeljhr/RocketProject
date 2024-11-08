using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

/*

Falcon 9 Rocket Physics Referece:

Total Rocket Mass: 549,800 kg
First Stage Mass (dry): 22,200 kg
First Stage Mass (fuel): 433,100 kg
Second Stage Mass (dry): 4,000 kg
Second Stage Mass (fuel): 111,500 kg
First Stage Thrust: 6,806 kN (1,530,000 lbf)
Second Stage Thrust: 934 kN (210,000 lbf)

ML Agent Considerations:
Observations:
-rocket postition, rotation, velocity (9 values)
-fuel remaining, fuel burn rate, thrust, max thrust, mass, drag, angular drag (7 values)
-platform position (3 values)

Rocket Controls:
Main Thruster:
- Discrete thrusters
    -WASD for generating thrust in 4 directions
    -Q and E for rotating the rocket
- Continuous thrusters
    -R to ramp up main thruster value
    -F to ramp down main thruster value
    -5 seconds total to ramp up or down

*/

public class RocketLanding : Agent
{
    public SimulationManager simulationManager;
    [Header("Object References")]
    public GameObject mainThrusterParticles;
    public Transform eastThruster;
    public GameObject eastThrusterParticles;
    public Transform westThruster;
    public GameObject westThrusterParticles;
    public Transform northThruster;
    public GameObject northThrusterParticles;
    public Transform southThruster;
    public GameObject southThrusterParticles;
    // [TextArea(5, 10)]
    // public string description;
    [Header("Rocket Parameters")]
    public float mass = 22200;
    public float thrust = 0;
    public float steerThrust = 1000;
    public float maxThrust = 6806000f;
    public float startingFuel = 200000;
    public float thrustIncreaseRate = 1000;
    public float fuelBurnRate = 1000;
    public float fuel; // not to be editted in inspector

    [Header("Environment Parameters")]
    public Transform platform;

    [Header("Physics Parameters")]
    public float gravity = 9.81f;
    public float drag = 0.1f;
    public float angularDrag = 0.1f;
    public float torqueAmount = 1000;

    [Header("Training Parameters")]
    public Material successMaterial;
    public Material failMaterial;
    public MeshRenderer platformMesh;
    Rigidbody rb;

    float mainThrust = 0;
    private float heuristicThrust = 0;
    float rocketLandingVelocity = 0.0f;

    float lastVerticalVelocity = 0;

    private float lastRotationX = 0;
    private float lastRotationY = 90;
    private float lastRotationZ = 0;
    private float lastPositionY = 2000;

    float thrustVector = 0f;
    bool simulationRunning = false;

    void Fail(float penalty = -1, string message = "") {
        // float penalty = -Mathf.Log10(Mathf.Abs(rocketLandingVelocity) + 1);
        // Debug.Log("Fail penalty: " + penalty);
        if (message != "") {
            Debug.Log(message);
        }

        SetReward(penalty);
        platformMesh.material = failMaterial;
        EndEpisode();
    }

    void Success() {
        Debug.Log("Landed");
        SetReward(25f);
        platformMesh.material = successMaterial;
        EndEpisode();
    }

    void FixedUpdate() {
        // check if simulation is supposed to be running and freeze the rocket if not
        if (!simulationManager.simulationRunning) {
            EndEpisode();
            return;
        } else if (simulationManager.simulationRunning && !simulationRunning) {
            simulationRunning = true;
            EndEpisode();
        }

        

        rb.mass = mass + fuel;
        rb.drag = drag;
        rb.angularDrag = angularDrag;

        gravity = float.Parse(simulationManager.gravity.text);
        Physics.gravity = new Vector3(0, -gravity, 0); // TODO: consider making the AI aware of the current gravity
        // Debug.Log("Fuel: " + fuel);
        rocketLandingVelocity = rb.velocity.y;

        // Reward for being closer to zero velocity the closer it is to the ground
        float distanceToPLatform = Vector3.Distance(transform.localPosition, platform.localPosition);
        // Encourage slow descent
        if (distanceToPLatform < 500f) {
            float reward = Mathf.Clamp(1f - (Mathf.Abs(rocketLandingVelocity) / 10f), -1f, 1f);
            AddReward(reward * Time.deltaTime);
        }

        // Check if rocket went up
        if (lastPositionY < transform.localPosition.y) {
            // SetReward(-10f);
            float penalty = -1f * Mathf.Clamp((transform.localPosition.y - lastPositionY), 0, 10);


            Fail(penalty, $"Rocket went up, penalty: {penalty} at distance: {distanceToPLatform}");
        }

        // Check if rocket is below platform
        if (platform.localPosition.y - 10.0f > transform.localPosition.y) {
            // Debug.Log("Below platform");
            Fail(-1f, "Below platform");
        }


        // if (rb.velocity.y < -10f){
        //     SetReward(rb.velocity.y * 0.05f);
        // }

        // Check if rocket is falling too fast
        // if (transform.localPosition.y < 500f) {
        //     Debug.Log("<500");
        //     if (rb.velocity.y < lastVerticalVelocity) {
        //         SetReward(-0.1f);

        //         if (rb.velocity.y < -30.0f) {
        //             Fail();
        //         }
        //     } else {
        //         Debug.Log($"Rocket is slowing down, velocity: {rb.velocity.y} > {lastVerticalVelocity}");
        //         SetReward(0.5f);
        //     }
        // }

        // Check if rocket is rotating away from upright position
        // if ((transform.localRotation.x > 0 && transform.localRotation.x > lastRotationX + 0.1f) || (transform.localRotation.x < 0 && transform.localRotation.x < lastRotationX - 0.1f)) {
        //     SetReward(-1f);
        // } else {
        //     SetReward(1f);
        // }

        // if ((transform.localRotation.y > 0 && transform.localRotation.y > lastRotationY + 0.1f) || (transform.localRotation.y < 0 && transform.localRotation.y < lastRotationY - 0.1f)) {
        //     SetReward(-1f);
        // } else {
        //     SetReward(1f);
        // }

        // if ((transform.localRotation.z > 0 && transform.localRotation.z > lastRotationZ + 0.1f) || (transform.localRotation.z < 0 && transform.localRotation.z < lastRotationZ - 0.1f)) {
        //     SetReward(-1f);
        // } else {
        //     SetReward(1f);
        // }

        lastRotationX = transform.localRotation.x;
        lastRotationY = transform.localRotation.y;
        lastRotationZ = transform.localRotation.z;
        lastPositionY = transform.localPosition.y;
        lastVerticalVelocity = rb.velocity.y;
    }

    void Start() {
        rb = GetComponent<Rigidbody>();
        // rb.mass = mass + fuel;
        // rb.drag = drag;
        // rb.angularDrag = angularDrag;
    }

    public override void OnEpisodeBegin()
    {
        rb = GetComponent<Rigidbody>();

        // Debug.Log($"RB: {rb}");

        fuel = startingFuel;
        rb.mass = mass + fuel;
        rb.drag = drag;
        rb.angularDrag = angularDrag;

        thrustIncreaseRate = maxThrust / 5f;
        fuelBurnRate = startingFuel / 160f;

        // transform.localPosition = new Vector3(Random.Range(-8,8),1.5f,Random.Range(-8,8));
        // transform.localPosition = new Vector3(0, 500f, 0);
        float initialPositionX = float.Parse(simulationManager.positionX.text);
        float initialPositionY = float.Parse(simulationManager.positionY.text);
        float initialPositionZ = float.Parse(simulationManager.positionZ.text);
        transform.localPosition = new Vector3(initialPositionX, initialPositionY, initialPositionZ);
        rb.velocity = new Vector3(0, Random.Range(-2f, -5f), 0);
        transform.localRotation = Quaternion.identity;
        lastRotationX = 0;
        lastRotationY = 90;
        lastRotationZ = 0;
        lastPositionY = 2000;

        thrust = 0f;

        // target.localPosition = new Vector3(Random.Range(-8,8),1.5f,Random.Range(-8,8));
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.localRotation);
        sensor.AddObservation(rb.velocity);

        sensor.AddObservation(fuel);
        sensor.AddObservation(fuelBurnRate);
        sensor.AddObservation(thrust);
        sensor.AddObservation(maxThrust);
        sensor.AddObservation(rb.mass);
        sensor.AddObservation(rb.drag);
        sensor.AddObservation(rb.angularDrag);

        sensor.AddObservation(platform.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        // Get Discrete Actions
        int rotateNorth = actions.DiscreteActions[0];
        int rotateEast = actions.DiscreteActions[1];
        int rotateSouth = actions.DiscreteActions[2];
        int rotateWest = actions.DiscreteActions[3];
        int rotateLeft = actions.DiscreteActions[4];
        int rotateRight = actions.DiscreteActions[5];

        // Rotate Rocket
        if (rotateNorth == 1) {
            rb.AddForceAtPosition(Vector3.right * steerThrust, northThruster.position);
        }
        if (rotateSouth == 1) {
            rb.AddForceAtPosition(Vector3.left * steerThrust, southThruster.position);
        }
        if (rotateWest == 1) {
            rb.AddForceAtPosition(Vector3.forward * steerThrust, westThruster.position);
        }
        if (rotateEast == 1) {
            rb.AddForceAtPosition(Vector3.back * steerThrust, eastThruster.position);
        }

        if (rotateLeft == 1) {
            rb.AddTorque(Vector3.up * torqueAmount);
        }
        if (rotateRight == 1) {
            rb.AddTorque(Vector3.down * torqueAmount);
        }

        
        // Get Continuous Actions
        // if (actions.ContinuousActions[0] > 0) {
            
            
        // }
        

        //Huristic is on or off by pressing R and F, ai should be doing the same way.
        mainThrust = actions.ContinuousActions[0];
        // Debug.Log($"mainThrust: {mainThrust}");
        
        // if (mainThrust > 0) {
        //     thrust += thrustIncreaseRate * Time.deltaTime;
        // }

        // if (mainThrust < 0){
        //     thrust -= thrustIncreaseRate * Time.deltaTime;
        // }

        thrust = maxThrust * mainThrust;



        thrust = Mathf.Clamp(thrust, 0, maxThrust);

        if (fuel > 0) {
            rb.AddForce(transform.up * thrust);
            fuel -= fuelBurnRate * Time.deltaTime;
            rb.mass = mass + fuel;
            if (fuel < 0) {
                fuel = 0;
            }
        } else  {
            // Debug.Log("Out of fuel");
            Fail(-1, "Out of fuel");
        }
        
        // mainThrust = actions.ContinuousActions[0];
        // Debug.Log($"mainThrust: {actions.ContinuousActions[0]}");
        // thrust = Mathf.Clamp((mainThrust + 1f) / 2f * maxThrust, 0, maxThrust);

        // // Add Forces
        // if (fuel > 0 && thrust > 0) {
        //     Vector3 thrustVector = transform.up * thrust;
        //     rb.AddForce(thrustVector);
        //     fuel -= fuelBurnRate * Time.deltaTime;
        //     rb.mass = mass + fuel;
        //     if (fuel < 0) {
        //         fuel = 0;
        //     }
        // } else if (fuel <= 0) {
        //     Debug.Log("Out of fuel");
        //     Fail();
        // }
    }

    private void OnCollisionEnter(Collision other) {

        Debug.Log($"Landed with a speed of {rocketLandingVelocity} m/s");

        if (rocketLandingVelocity < -10.0f) {
            float penalty = Mathf.Abs(rocketLandingVelocity) * -0.2f;
            Debug.Log("Fail penalty: " + penalty);
            Fail(penalty);
        } else {
            Success();
        }

        // if (other.collider.CompareTag("goal")) {
        //     Debug.Log("Goal");
        //     SetReward(1f);
        //     platformMesh.material = successMaterial;
        //     EndEpisode();
        // } else if (other.collider.CompareTag("wall")) {          
        //     Debug.Log("Wall");
        //     SetReward(-1f);
        //     platformMesh.material = failMaterial;
        //     EndEpisode();
        // }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        discreteActionsOut[0] = Input.GetKey(KeyCode.W) ? 1 : 0;
        discreteActionsOut[1] = Input.GetKey(KeyCode.D) ? 1 : 0;
        discreteActionsOut[2] = Input.GetKey(KeyCode.S) ? 1 : 0;
        discreteActionsOut[3] = Input.GetKey(KeyCode.A) ? 1 : 0;
        discreteActionsOut[4] = Input.GetKey(KeyCode.Q) ? 1 : 0;
        discreteActionsOut[5] = Input.GetKey(KeyCode.E) ? 1 : 0;


        var continuousActionsOut = actionsOut.ContinuousActions;

        if (Input.GetKey(KeyCode.R)) {
            // Debug.Log("Increasing thrust");
            heuristicThrust += (thrustIncreaseRate * Time.deltaTime * 10) / maxThrust;
        } else if (Input.GetKey(KeyCode.F)) {
            // Debug.Log("Decreasing thrust");
            heuristicThrust -= (thrustIncreaseRate * Time.deltaTime * 10) / maxThrust;
        }

        heuristicThrust = Mathf.Clamp(heuristicThrust, -1f, 1f);

        continuousActionsOut[0] = heuristicThrust;
    }
}
