using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketLandingManual : MonoBehaviour
{
    [Header("Object References")]
    public GameObject mainThrusterParticles;
    public GameObject eastThrusterParticles;
    public GameObject westThrusterParticles;
    public GameObject northThrusterParticles;
    public GameObject southThrusterParticles;

    [Header("Rocket Parameters")]
    public float mass = 22200;
    public float thrust = 0;
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

    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        fuel = startingFuel;
        UpdatePhysics();
    }

    // Update is called once per frame
    void Update()
    {
        ChangeThrust();
        UpdatePhysics();
    }

    void FixedUpdate() {
        ApplyThrust();
        RotationInput();
    }

    void UpdatePhysics() {
        rb.mass = mass + fuel;
        rb.drag = drag;
        rb.angularDrag = angularDrag;

        Physics.gravity = new Vector3(0, -gravity, 0);
    }

    private void ApplyThrust() {
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


    private void ChangeThrust() {
        if (Input.GetKey(KeyCode.R)) {
            thrust += thrustIncreaseRate * Time.deltaTime;
            if (thrust > maxThrust) {
                thrust = maxThrust;
            }
            Debug.Log("Increasing Thrust: " + thrust);
        } else if (Input.GetKey(KeyCode.F)) {
            thrust -= thrustIncreaseRate * Time.deltaTime;
            if (thrust < 0) {
                thrust = 0;
            }
            Debug.Log("Decreasing Thrust: " + thrust);
        }
    }

    private void RotationInput() {
        if (Input.GetKey(KeyCode.W)) {
            rb.AddTorque(Vector3.right * torqueAmount);
        }
        if (Input.GetKey(KeyCode.S)) {
            rb.AddTorque(Vector3.left * torqueAmount);
        }
        if (Input.GetKey(KeyCode.A)) {
            rb.AddTorque(Vector3.forward * torqueAmount);
        }
        if (Input.GetKey(KeyCode.D)) {
            rb.AddTorque(Vector3.back * torqueAmount);
        }

        if (Input.GetKey(KeyCode.Q)) {
            rb.AddTorque(Vector3.up * torqueAmount);
        }
        if (Input.GetKey(KeyCode.E)) {
            rb.AddTorque(Vector3.down * torqueAmount);
        }
    }


    // private void OnCollisionEnter(Collision other) {
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
    // }
}
