using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using System.Collections.Generic;

public class LightFlickerEffect : MonoBehaviour 
{
    public float Scaling { get; set; } = 1.0f;

    [SerializeField] private Light2D light;
    [SerializeField] private Light2D rimLight;
    [SerializeField] private float minIntensity = 0f;
    [SerializeField] private float maxIntensity = 1f;
    [Range(1, 50)] [SerializeField] private int smoothing = 5;

    private bool _active = true;

    // Continuous average calculation via FIFO queue
    // Saves us iterating every time we update, we just change by the delta
    Queue<float> smoothQueue;
    float lastSum = 0;


    /// <summary>
    /// Reset the randomness and start again. You usually don't need to call
    /// this, deactivating/reactivating is usually fine but if you want a strict
    /// restart you can do.
    /// </summary>
    public void ResetLight() {
        smoothQueue.Clear();
        lastSum = 0;
    }

    public void SetActive(bool active)
    {
        _active = active;
        light.enabled = active;
    }

    void Start() {
         smoothQueue = new Queue<float>(smoothing);
         // External or internal light?
         if (light == null) {
            light = GetComponent<Light2D>();
         }
    }

    void Update() {

        rimLight.enabled = Scaling > 0.5f;

        if (light == null || !_active)
            return;


        // pop off an item if too big
        while (smoothQueue.Count >= smoothing) {
            lastSum -= smoothQueue.Dequeue();
        }

        // Generate random new item, calculate new average
        float newVal = Random.Range(minIntensity, maxIntensity);
        smoothQueue.Enqueue(newVal);
        lastSum += newVal;

        // Calculate new smoothed average
        light.intensity = (lastSum / (float)smoothQueue.Count) * Scaling;
    }

}
