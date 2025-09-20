using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class velocityText : MonoBehaviour
{

    public GameObject ro;
    public TMP_Text textFieldVelocity;

    public float velocity = 0f;

    float lastVelocity = 0f;

    // Update is called once per frame
    void Update()
    {

        velocity = Math.Abs(ro.GetComponent<RocketLanding>().rb.velocity.y);
        velocity = (float)Math.Round(velocity, 1);

        textFieldVelocity.text = velocity.ToString();

        lastVelocity = velocity;
    }
}
