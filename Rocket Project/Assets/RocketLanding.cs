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
    public float maxThrust = 1500000;
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
    float rocketLandingVelocity = 0.0f;

    private float lastRotationX = 0;
    private float lastRotationY = 90;
    private float lastRotationZ = 0;
    private float lastPositionY = 0;

    void Fail() {
        Debug.Log("Crashed");
        SetReward(-1f);
        platformMesh.material = failMaterial;
        EndEpisode();
    }

    void Success() {
        Debug.Log("Landed");
        SetReward(1f);
        platformMesh.material = successMaterial;
        EndEpisode();
    }

    void FixedUpdate() {
        rb.mass = mass + fuel;
        rb.drag = drag;
        rb.angularDrag = angularDrag;

        Physics.gravity = new Vector3(0, -gravity, 0);
        // Debug.Log("Fuel: " + fuel);
        rocketLandingVelocity = rb.velocity.y;

        // Check if rocket went up
        if (lastPositionY < transform.localPosition.y) {
            Fail();
        }

        // Check if rocket is below platform
        if (platform.localPosition.y > transform.localPosition.y) {
            Fail();
        }

        // Check if rocket is rotating away from upright position
        if ((transform.localRotation.x > 0 && transform.localRotation.x > lastRotationX + 0.1f) || (transform.localRotation.x < 0 && transform.localRotation.x < lastRotationX - 0.1f)) {
            SetReward(-1f);
        } else {
            SetReward(1f);
        }

        if ((transform.localRotation.y > 0 && transform.localRotation.y > lastRotationY + 0.1f) || (transform.localRotation.y < 0 && transform.localRotation.y < lastRotationY - 0.1f)) {
            SetReward(-1f);
        } else {
            SetReward(1f);
        }

        if ((transform.localRotation.z > 0 && transform.localRotation.z > lastRotationZ + 0.1f) || (transform.localRotation.z < 0 && transform.localRotation.z < lastRotationZ - 0.1f)) {
            SetReward(-1f);
        } else {
            SetReward(1f);
        }

        lastRotationX = transform.localRotation.x;
        lastRotationY = transform.localRotation.y;
        lastRotationZ = transform.localRotation.z;
        lastPositionY = transform.localPosition.y;
    }

    public override void OnEpisodeBegin()
    {
        rb = GetComponent<Rigidbody>();

        fuel = startingFuel;
        rb.mass = mass + fuel;
        rb.drag = drag;
        rb.angularDrag = angularDrag;

        // transform.localPosition = new Vector3(Random.Range(-8,8),1.5f,Random.Range(-8,8));
        transform.localPosition = new Vector3(0, 1000f, 0);
        rb.velocity = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        lastRotationX = 0;
        lastRotationY = 90;
        lastRotationZ = 0;

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
        if (actions.ContinuousActions[0] > 0) {
            mainThrust = actions.ContinuousActions[0];
        }
        thrust = Mathf.Clamp(mainThrust, 0, maxThrust);

        // Add Forces
        if (fuel > 0 && thrust > 0) {
            Vector3 thrustVector = transform.up * thrust;
            rb.AddForce(thrustVector);
            fuel -= fuelBurnRate * Time.deltaTime;
            rb.mass = mass + fuel;
            if (fuel < 0) {
                fuel = 0;
            }
        } else if (fuel <= 0) {
            Debug.Log("Out of fuel");
        }
    }

    private void OnCollisionEnter(Collision other) {

        Debug.Log($"Landed with a speed of {rocketLandingVelocity} m/s");

        if (rocketLandingVelocity < -5.0f) {
            Fail();
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
            Debug.Log($"Adding thrust: {thrustIncreaseRate * Time.deltaTime}");
            thrust += thrustIncreaseRate * Time.deltaTime;

            if (thrust > maxThrust) {
                thrust = maxThrust;
            }

            continuousActionsOut[0] = thrust;
        } else if (Input.GetKey(KeyCode.F)) {
            Debug.Log($"Removing thrust: {thrustIncreaseRate * Time.deltaTime}");
            thrust -= thrustIncreaseRate * Time.deltaTime;

            if (thrust < 0) {
                thrust = 0;
            }

            continuousActionsOut[0] = thrust;
        }
    }
}