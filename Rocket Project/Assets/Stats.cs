using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/*Success: 0
Failures: 0
Rise fail
Miss fail
drift fail
Fuel fail
Crash fail*/

public class Stats : MonoBehaviour
{
    public TextMeshProUGUI statText;
    public static int success = 0;
    public static int riseFails = 0;
    public static int missFails = 0;
    public static int driftFails = 0;
    public static int fuelFails = 0;
    public static int crashFails = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        int totalFails = riseFails + missFails + driftFails + fuelFails + crashFails;
        statText.text = "Success: " + success + "\n" +
                         "Failures: " + totalFails + "\n" +
                         "Rise fail: " + riseFails + "\n" +
                         "Miss fail: " + missFails + "\n" +
                         "Drift fail: " + driftFails + "\n" +
                         "Fuel fail: " + fuelFails + "\n" +
                         "Crash fail: " + crashFails + "\n" +
                         "Total: " + (success + totalFails) + "\n" +
                         "Accuracy: " + (success + totalFails == 0 ? 0 : (float)success / (success + totalFails) * 100).ToString("F2") + "%";
    }
}
