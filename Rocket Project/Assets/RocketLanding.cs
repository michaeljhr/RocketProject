using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Policies;
using Unity.Barracuda;
using System;

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

max: 1000 particles/sec
min: 0 particles/sec

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
    public AudioSource thrustSound;
    // [TextArea(5, 10)]
    // public string description;
    [Header("Rocket Parameters")]
    public float mass = 22200;
    public float thrust = 0;
    public float steerThrust = 1000;
    public float translationThrust = 1000;
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

    private float lastPositionX = 0;
    private float lastPositionY = 2000;
    private float lastPositionZ = 0;

    float thrustVector = 0f;
    bool simulationRunning = false;

    // timestamp for start of current run
    private float startTime;

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
        // also reward for faster landing
        float timeToLand = Time.time - startTime;

        // add on a reward for landing faster
        // float reward = (10f / timeToLand) * 100;
        // Debug.Log($"Time to land: {timeToLand}, reward: {reward + 10f}");
        // SetReward(10f * reward);
        // This is if the numerator was (10f / timeToLand) * 100; OLD METHOD, USE IF THE NEW BREAKS EVERYTHING
        // 80 seconds = 12.5
        // 70 seconds = 14.2
        // 60 seconds = 16.6
        // 50 seconds = 20
        // 40 seconds = 25
        // 30 seconds = 33.3
        // 20 seconds = 50
        // 10 seconds = 100

        float reward = (60f / timeToLand) * 25;
        Debug.Log($"Time to land: {timeToLand}, reward: {reward}");
        SetReward(reward);
        // This is if the numerator was (60f / timeToLand) * 25;
        // 80 seconds = 18.75
        // 70 seconds = 21.43
        // 60 seconds = 25
        // 50 seconds = 30

        

        platformMesh.material = successMaterial;
        EndEpisode();
    }

    void FixedUpdate() {
        // check if simulation is supposed to be running and freeze the rocket if not
        if (simulationManager != null) {
            if (!simulationManager.simulationRunning) {
                EndEpisode();
                return;
            } else if (simulationManager.simulationRunning && !simulationRunning) {
                simulationRunning = true;
                EndEpisode();
            }
            gravity = float.Parse(simulationManager.gravity.text);
            Physics.gravity = new Vector3(0, -gravity, 0); // TODO: consider making the AI aware of the current gravity
        }
        
        rb.mass = mass + fuel;
        rb.drag = drag;
        rb.angularDrag = angularDrag;

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
        if (lastPositionY < transform.localPosition.y && GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.HeuristicOnly) {
            // SetReward(-10f);
            float penalty = -1f * Mathf.Clamp((transform.localPosition.y - lastPositionY), 0, 10);


            Fail(penalty, $"Rocket went up, penalty: {penalty} at distance: {distanceToPLatform}");
        }

        // Check if rocket is below platform
        if (platform.localPosition.y - 10.0f > transform.localPosition.y) {
            // Debug.Log("Below platform");
            Fail(-1f, "Below platform");
        }

        // Check if the rocket drifts away from the platform
        float tolerance = 45f;
        float distanceX = Mathf.Abs(transform.localPosition.x - platform.transform.localPosition.x);
        float lastDistanceX = Mathf.Abs(lastPositionX - platform.transform.localPosition.x);
        float distanceZ = Mathf.Abs(transform.localPosition.z - platform.transform.localPosition.z);
        float lastDistanceZ = Mathf.Abs(lastPositionZ - platform.transform.localPosition.z);
        if ((distanceX < lastDistanceX || distanceX < tolerance) && (distanceZ < lastDistanceZ || distanceZ < tolerance)) {
            AddReward(0.0001f);
        } else if (distanceX == lastDistanceX || distanceZ == lastDistanceZ) {
            AddReward(0);
        } else {
            Fail(-1f, "Drifted away from platform");
        }


        // Check if the rocket is rotating away from upright position
        // TODO: re-enable this once the AI properly learned translation and is ready for rotation
        // float toleranceRotation = 5.0f;
        // float xAngle = Mathf.Abs(transform.localRotation.x);
        // float xLastAngle = Mathf.Abs(lastRotationX);
        // float yAngle = Mathf.Abs(transform.localRotation.y);
        // float yLastAngle = Mathf.Abs(lastRotationY);
        // float zAngle = Mathf.Abs(transform.localRotation.z);
        // float zLastAngle = Mathf.Abs(lastRotationZ);
        // if ((xAngle < xLastAngle || xAngle < toleranceRotation) && (yAngle < yLastAngle || yAngle < toleranceRotation) && (zAngle < zLastAngle || zAngle < toleranceRotation)) {
        //     AddReward(0.0001f);
        // } else if (Mathf.Abs(transform.localRotation.x) == lastRotationX || Mathf.Abs(transform.localRotation.y) == lastRotationY || Mathf.Abs(transform.localRotation.z) == lastRotationZ) {
        //     AddReward(0);
        // } else {
        //     Fail(-1f, "Rocket is rotating away from upright position");
        // }



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
        startTime = Time.time;

        rb = GetComponent<Rigidbody>();

        // Debug.Log($"RB: {rb}");

        fuel = startingFuel;
        rb.mass = mass + fuel;
        rb.drag = drag;
        rb.angularDrag = angularDrag;

        thrustIncreaseRate = maxThrust / 5f;
        fuelBurnRate = startingFuel / 160f;

        if (simulationManager != null) {
            float initialPositionX = float.Parse(simulationManager.positionX.text);
            float initialPositionY = float.Parse(simulationManager.positionY.text);
            float initialPositionZ = float.Parse(simulationManager.positionZ.text);
            transform.localPosition = new Vector3(initialPositionX, initialPositionY, initialPositionZ);
            lastPositionX = initialPositionX;
            lastPositionY = initialPositionY;
            lastPositionZ = initialPositionZ;
        } else {
            // transform.localPosition = new Vector3(Random.Range(-8,8),1.5f,Random.Range(-8,8));
            transform.localPosition = new Vector3(UnityEngine.Random.Range(-100, 100), 500f, UnityEngine.Random.Range(-100, 100));
            lastPositionX = 0;
            lastPositionY = 500f;
            lastPositionZ = 0;
        }

        // randomize the x and z of the platform (-900, 900)
        platform.localPosition = new Vector3(UnityEngine.Random.Range(-100, 100), 0, UnityEngine.Random.Range(-100, 100));
        
        rb.velocity = new Vector3(0, UnityEngine.Random.Range(-2f, -5f), 0);
        transform.localRotation = Quaternion.identity;
        lastRotationX = 0;
        lastRotationY = 90;
        lastRotationZ = 0;
        lastPositionY = 2000;

        thrust = 0f;
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

        int translateNorth = actions.DiscreteActions[6];
        int translateEast = actions.DiscreteActions[7];
        int translateSouth = actions.DiscreteActions[8];
        int translateWest = actions.DiscreteActions[9];

        northThrusterParticles.GetComponent<ParticleSystem>().emissionRate = 0;
        southThrusterParticles.GetComponent<ParticleSystem>().emissionRate = 0;
        eastThrusterParticles.GetComponent<ParticleSystem>().emissionRate = 0;
        westThrusterParticles.GetComponent<ParticleSystem>().emissionRate = 0;

        // Rotate Rocket
        if (rotateNorth == 1) {
            rb.AddForceAtPosition(Vector3.right * steerThrust, northThruster.position);
            northThrusterParticles.GetComponent<ParticleSystem>().emissionRate = 100;
        }
        if (rotateSouth == 1) {
            rb.AddForceAtPosition(Vector3.left * steerThrust, southThruster.position);
            southThrusterParticles.GetComponent<ParticleSystem>().emissionRate = 100;
        }
        if (rotateWest == 1) {
            rb.AddForceAtPosition(Vector3.forward * steerThrust, westThruster.position);
            westThrusterParticles.GetComponent<ParticleSystem>().emissionRate = 100;
        }
        if (rotateEast == 1) {
            rb.AddForceAtPosition(Vector3.back * steerThrust, eastThruster.position);
            eastThrusterParticles.GetComponent<ParticleSystem>().emissionRate = 100;
        }

        if (rotateLeft == 1) {
            rb.AddTorque(Vector3.up * torqueAmount);
        }
        if (rotateRight == 1) {
            rb.AddTorque(Vector3.down * torqueAmount);
        }

        // Translate Rocket
        if (translateNorth == 1) {
            // apply force to the whole rocket
            rb.AddForce(Vector3.forward * translationThrust);
            northThrusterParticles.GetComponent<ParticleSystem>().emissionRate = 100;
        }
        if (translateSouth == 1) {
            rb.AddForce(Vector3.back * translationThrust);
            southThrusterParticles.GetComponent<ParticleSystem>().emissionRate = 100;
        }
        if (translateWest == 1) {
            rb.AddForce(Vector3.left * translationThrust);
            westThrusterParticles.GetComponent<ParticleSystem>().emissionRate = 100;
        }
        if (translateEast == 1) {
            rb.AddForce(Vector3.right * translationThrust);
            eastThrusterParticles.GetComponent<ParticleSystem>().emissionRate = 100;
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

        float thrustVolume = Mathf.Clamp(thrust / maxThrust, 0, 1);
        thrustSound.volume = thrustVolume;
        mainThrusterParticles.GetComponent<ParticleSystem>().emissionRate = thrustVolume * 1000;

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

        if (rocketLandingVelocity < -7.0f) {
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

        discreteActionsOut[6] = Input.GetKey(KeyCode.UpArrow) ? 1 : 0;
        discreteActionsOut[7] = Input.GetKey(KeyCode.RightArrow) ? 1 : 0;
        discreteActionsOut[8] = Input.GetKey(KeyCode.DownArrow) ? 1 : 0;
        discreteActionsOut[9] = Input.GetKey(KeyCode.LeftArrow) ? 1 : 0;

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
