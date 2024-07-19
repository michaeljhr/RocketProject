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
    // [TextArea(5, 10)]
    // public string description;
    [Header("Rocket Parameters")]
    public float mass = 22200;
    public float thrust = 0;
    public float maxThrust = 1500000;
    public float startingFuel = 200000;
    public float fuelBurnRate = 1000;
    public float fuel; // not to be editted in inspector

    [Header("Environment Parameters")]
    public Transform platform;

    [Header("Physics Parameters")]
    public float gravity = 9.81f;
    public float drag = 0.1f;
    public float angularDrag = 0.1f;

    [Header("Training Parameters")]
    public Material successMaterial;
    public Material failMaterial;
    public MeshRenderer platformMesh;
    Rigidbody rb;

    public override void OnEpisodeBegin()
    {
        rb = GetComponent<Rigidbody>();

        fuel = startingFuel;
        rb.mass = mass + fuel;
        rb.drag = drag;
        rb.angularDrag = angularDrag;

        // transform.localPosition = new Vector3(Random.Range(-8,8),1.5f,Random.Range(-8,8));
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

        // Get Continuous Actions
        float mainThrust = actions.ContinuousActions[0];
        thrust = Mathf.Clamp(mainThrust, 0, maxThrust);

        // Add Forces
        Vector3 thrustVector = transform.up * thrust;
        Debug.Log($"Adding thrust: {thrustVector}");
        rb.AddForce(thrustVector);
    }

    private void OnCollisionEnter(Collision other) {
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
            Debug.Log($"Adding thrust: {1000 * Time.deltaTime}");
            thrust += 1000 * Time.deltaTime;

            if (thrust > maxThrust) {
                thrust = maxThrust;
            }

            continuousActionsOut[0] = thrust;
        } else if (Input.GetKey(KeyCode.F)) {
            Debug.Log($"Removing thrust: {1000 * Time.deltaTime}");
            thrust -= 1000 * Time.deltaTime;

            if (thrust < 0) {
                thrust = 0;
            }

            continuousActionsOut[0] = thrust;
        }
    }
}
