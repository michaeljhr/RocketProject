using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SimulationManager : MonoBehaviour
{
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

    // Start is called before the first frame update
    void Start()
    {
        originalPosition = rocket.transform;
        originalRotation = rocket.transform.localRotation;
        originalVelocity = rocket.GetComponent<Rigidbody>().velocity;

        ResetRocket();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetRocket() {
        if (useInitialSpawn) {
            rocket.transform.localPosition= new Vector3(float.Parse(positionX.text), float.Parse(positionY.text), float.Parse(positionZ.text));
            rocket.transform.localRotation = new Quaternion(float.Parse(rotationX.text), float.Parse(rotationY.text), float.Parse(rotationZ.text), 1);
            rocket.GetComponent<Rigidbody>().velocity = new Vector3(float.Parse(velocityX.text), float.Parse(velocityY.text), float.Parse(velocityZ.text));
        }
        if (initialMass.text != "") {
            Debug.Log($"Setting mass to {initialMass.text}");
            rocket.GetComponent<RocketLandingManual>().mass = float.Parse(initialMass.text);
            Debug.Log($"Mass set to {rocket.GetComponent<RocketLandingManual>().mass}");
        }
        if (currentFuel.text != "") rocket.GetComponent<RocketLandingManual>().startingFuel = float.Parse(currentFuel.text);
        // if (maxFuel.text != "") rocket.GetComponent<RocketLandingManual>().maxFuel = float.Parse(maxFuel.text);

        if (gravity.text != "") rocket.GetComponent<RocketLandingManual>().gravity = float.Parse(gravity.text);
        if (airResistance.text != "") rocket.GetComponent<RocketLandingManual>().drag = float.Parse(airResistance.text);
        if (airResistance.text != "") rocket.GetComponent<RocketLandingManual>().angularDrag = float.Parse(airResistance.text);

        Debug.Log($"Rocket Reset with mass: {rocket.GetComponent<RocketLandingManual>().mass}, fuel: {rocket.GetComponent<RocketLandingManual>().startingFuel}");
    }
}
