using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Policies;
using Unity.Barracuda;
using TMPro;

public class SimulationManager : MonoBehaviour
{
    public bool simulationRunning = false;
    public enum Environment { Earth, Moon, Mars, Custom }

    [Header("Environment Parameters")]
    public Environment environment = Environment.Earth;
    public TMP_Dropdown environmentDropdown;

    // skyboxes
    public Material earthSkybox;
    public Material moonSkybox;
    public Material marsSkybox;

    [Header("Spawn Parameters")]
    public TMP_InputField positionX;
    public TMP_InputField positionY;
    public TMP_InputField positionZ;
    public TMP_InputField rotationX;
    public TMP_InputField rotationY;
    public TMP_InputField rotationZ;
    public TMP_InputField velocityX;
    public TMP_InputField velocityY;
    public TMP_InputField velocityZ;

    [Header("Rocket Parameters")]
    public GameObject rocket;
    public bool useInitialSpawn = true;
    // public Transform initialPosition;
    // public Transform initialRotation;
    // public Transform initialVelocity;
    public TMP_InputField initialMass;
    public TMP_InputField currentFuel;
    public TMP_InputField maxFuel;
    public TMP_InputField gravity;
    public TMP_InputField airResistance;

    private Transform originalPosition;
    private Quaternion originalRotation;
    private Vector3 originalVelocity;

    [Header("Control Parameters")]
    public BehaviorParameters showcaseRocket;
    public List<NNModel> models;
    public TMP_Dropdown modelDropdown;
    public GameObject manualControlButton;
    public GameObject aiControlButton;


    // Start is called before the first frame update
    void Start()
    {
        originalPosition = rocket.transform;
        originalRotation = rocket.transform.localRotation;
        originalVelocity = rocket.GetComponent<Rigidbody>().velocity;

        // ResetRocket();
        SetEnvironment((int)environment);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartSimulation() {
        ResetRocket();
        simulationRunning = true;
    }

    public void StopSimulation() {
        simulationRunning = false;
    }

    public void SetModel() {
        int value = modelDropdown.value;
        showcaseRocket.Model = models[value];
        // Debug.Log($"Model set to {models[value].name}");

        if (value == models.Count - 1) {
            manualControlButton.SetActive(false);
            aiControlButton.SetActive(true);

            // set the rocket to heuristic control
            rocket.GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.HeuristicOnly;
        } else {
            manualControlButton.SetActive(true);
            aiControlButton.SetActive(false);
            rocket.GetComponent<BehaviorParameters>().BehaviorType = BehaviorType.InferenceOnly;
        }
    }

    public void ToggleManualControl(bool value) {
        if (value) {
            // set the dropdown value to the last option
            modelDropdown.value = modelDropdown.options.Count - 1;
            SetModel();
        } else {
            // set the dropdown value to the first option
            modelDropdown.value = 0;
            SetModel();
        }
    }

    public void SetEnvironment(int value) {
        environment = (Environment)value;
        Debug.Log($"Environment set to {environment}");
        switch (environment) {
            case Environment.Earth:
                gravity.text = "9.81";
                airResistance.text = "0.1";
                // apply earth skybox
                RenderSettings.skybox = earthSkybox;

                // set the gravity of the world
                Physics.gravity = new Vector3(0, -9.81f, 0);

                // set the drag of the rocket
                rocket.GetComponent<RocketLanding>().drag = 0.1f;

                break;
            case Environment.Moon:
                gravity.text = "1.62";
                airResistance.text = "0.1";
                // apply moon skybox
                RenderSettings.skybox = moonSkybox;

                // set the gravity of the world
                Physics.gravity = new Vector3(0, -1.62f, 0);

                // set the drag of the rocket
                rocket.GetComponent<RocketLanding>().drag = 0f;
                break;
            case Environment.Mars:
                gravity.text = "3.71";
                airResistance.text = "0.1";
                // apply mars skybox
                RenderSettings.skybox = marsSkybox;

                // set the gravity of the world
                Physics.gravity = new Vector3(0, -3.71f, 0);

                // set the drag of the rocket
                rocket.GetComponent<RocketLanding>().drag = 0.05f;
                break;
            case Environment.Custom:
                // gravity.text = "";
                // airResistance.text = "";
                break;
        }

        // update the dropdown value
        environmentDropdown.SetValueWithoutNotify(value);
    }

    public void ResetRocket() {
        rocket.GetComponent<RocketLanding>().EndEpisode();
        if (useInitialSpawn) {
            // rocket.transform.localPosition= new Vector3(float.Parse(positionX.text), float.Parse(positionY.text), float.Parse(positionZ.text));
            // rocket.transform.localRotation = new Quaternion(float.Parse(rotationX.text), float.Parse(rotationY.text), float.Parse(rotationZ.text), 1);
            // rocket.GetComponent<Rigidbody>().velocity = new Vector3(float.Parse(velocityX.text), float.Parse(velocityY.text), float.Parse(velocityZ.text));

            rocket.transform.localPosition= new Vector3(UnityEngine.Random.Range(-200, 200), 1000f, UnityEngine.Random.Range(-200, 200));
            rocket.transform.localRotation = new Quaternion(float.Parse(rotationX.text), float.Parse(rotationY.text), float.Parse(rotationZ.text), 1);
            rocket.GetComponent<Rigidbody>().velocity = new Vector3(float.Parse(velocityX.text), float.Parse(velocityY.text), float.Parse(velocityZ.text));
        }
        if (initialMass.text != "") {
            Debug.Log($"Setting mass to {initialMass.text}");
            rocket.GetComponent<RocketLanding>().mass = float.Parse(initialMass.text);
            Debug.Log($"Mass set to {rocket.GetComponent<RocketLanding>().mass}");
        }
        if (currentFuel.text != "") rocket.GetComponent<RocketLanding>().fuel = float.Parse(currentFuel.text);
        if (maxFuel.text != "") rocket.GetComponent<RocketLanding>().startingFuel = float.Parse(maxFuel.text);

        // if (gravity.text != "") rocket.GetComponent<RocketLandingManual>().gravity = float.Parse(gravity.text);
        // if (airResistance.text != "") rocket.GetComponent<RocketLandingManual>().drag = float.Parse(airResistance.text);
        // if (airResistance.text != "") rocket.GetComponent<RocketLandingManual>().angularDrag = float.Parse(airResistance.text);

        // Debug.Log($"Rocket Reset with mass: {rocket.GetComponent<RocketLandingManual>().mass}, fuel: {rocket.GetComponent<RocketLandingManual>().startingFuel}");
    }
}
