using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class velocityText : MonoBehaviour
{

    public GameObject rocket;
    public TMP_Text textFieldVelocity;
    public TMP_Text textFieldPosition;

    public float velocity = 0f;

    public float position = 0f;

    // Update is called once per frame
    void FixedUpdate()
    {

        velocity = Math.Abs(rocket.GetComponent<RocketLanding>().rb.velocity.y);
        velocity = (float)Math.Round(velocity, 1);


        position = rocket.GetComponent<RocketLanding>().rb.position.y;
        position = (float) Math.Round(position, 1);

        textFieldPosition.text = position.ToString();

        textFieldVelocity.text = velocity.ToString();


    }
}
