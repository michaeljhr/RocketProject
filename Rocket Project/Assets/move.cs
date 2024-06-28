using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class move : Agent
{
    // [TextArea(5, 10)]
    // public string description;
    public float speed = 10;
    public Material successMaterial;
    public Material failMaterial;
    public MeshRenderer platform;
    Rigidbody rb;

    // [Tooltip("This is the speed of the rocket")]
    // [SerializeField]
    // List<int> list = new List<int>();

    [SerializeField] Transform target;

    public override void OnEpisodeBegin()
    {
        rb = GetComponent<Rigidbody>();
        transform.localPosition = new Vector3(Random.Range(-8,8),1.5f,Random.Range(-8,8));
        target.localPosition = new Vector3(Random.Range(-8,8),1.5f,Random.Range(-8,8));
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(target.localPosition);
    }

    public override void OnActionReceived(ActionBuffers actions) {
        // Debug.Log($"A: {actions.ContinuousActions[0]}");
        // Debug.Log($"B: {actions.ContinuousActions[1]}");
        // Debug.Log(actions.DiscreteActions[0]);
        // Debug.Log(actions.DiscreteActions[1]);
        // Debug.Log(actions.DiscreteActions[2]);
        // Debug.Log(actions.DiscreteActions[3]);

        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        // transform.localPosition += new Vector3(moveX, 0, moveZ) * speed * Time.deltaTime;

        // add force
        rb.AddForce(new Vector3(moveX, 0, moveZ) * speed * Time.deltaTime, ForceMode.VelocityChange);
    }

    // private void OnTriggerEnter(Collider other) {
    //     // Check if tag is target
    //     if (other.CompareTag("goal")) {
    //         SetReward(1f);
    //         EndEpisode();
    //     }
    // }

    // private void OnColliderEnter(Collider other) {
    //     if (other.CompareTag("goal")) {
    //         Debug.Log("Goal");
    //         SetReward(1f);
    //         platform.material = successMaterial;
    //         EndEpisode();
    //     } else if (other.CompareTag("wall")) {
    //         Debug.Log("Wall");
    //         SetReward(-1f);
    //         platform.material = failMaterial;
    //         EndEpisode();
    //     }
    // }

    private void OnCollisionEnter(Collision other) {
        if (other.collider.CompareTag("goal")) {
            Debug.Log("Goal");
            SetReward(1f);
            platform.material = successMaterial;
            EndEpisode();
        } else if (other.collider.CompareTag("wall")) {
            Debug.Log("Wall");
            SetReward(-1f);
            platform.material = failMaterial;
            EndEpisode();
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = Input.GetAxis("Horizontal");
        continuousActionsOut[1] = Input.GetAxis("Vertical");
    }
}
