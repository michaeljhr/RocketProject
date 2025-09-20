using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class velocityText : MonoBehaviour
{

    public GameObject ro;
    public TMP_Text textFieldVelocity;
    public TMP_Text textFieldAcc;

    public float velocity = 0f;

    public float acceleration = 0f;

    float lastVelocity = 0f;
    int c = 0;

    // Update is called once per frame
    void Update()
    {

        velocity = Math.Abs(ro.GetComponent<RocketLanding>().rb.velocity.y);
        velocity = (float)Math.Round(velocity, 1);
        if(c<50){
            c+=1;
        }
        else{

            acceleration = (velocity-lastVelocity)/Time.deltaTime;
            acceleration = (float)Math.Round(acceleration, 1);
            Debug.Log(velocity-lastVelocity);
            lastVelocity = velocity;
            c=0;
        }





        

        textFieldVelocity.text = velocity.ToString();


    }
}
