﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu class
/// Toggle between menu and detail view
/// Allow button interaction
/// Detect Shake: https://resocoder.com/2018/07/20/shake-detecion-for-mobile-devices-in-unity-android-ios/
/// To use the class Gyroscope, the device needs to have a gyroscope
/// </summary>
public class Menu : MonoBehaviour
{
    public Text Log;

    private float minInputInterval = 0.2f; // 0.2sec - to avoid detecting multiple shakes per shake
    private float sqrShakeDetectionThreshold;
    private int shakeCounter;

    private InputTracker shakeTracker;
    private InputTracker tiltTracker;

    private Gyroscope deviceGyroscope;

    void Start()
    {
        shakeTracker = new InputTracker();
        shakeTracker.Threshold = 3.6f;
        sqrShakeDetectionThreshold = Mathf.Pow(shakeTracker.Threshold, 2);

        tiltTracker = new InputTracker();
        tiltTracker.Threshold = 0.1f;
        tiltTracker.TimeSinceLast = Time.unscaledTime;
        deviceGyroscope = Input.gyro;
        deviceGyroscope.enabled = true;
    }

    void Update()
    {
        if (shakeCounter > 0 && Time.unscaledTime > shakeTracker.TimeSinceFirst + minInputInterval * 5)
        {
            HandleShakeInput(shakeCounter);
            shakeCounter = 0;
        }

        //Debug Input
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMenu();
        }
        else if (Input.GetKeyDown(KeyCode.L)) { //Shake
            shakeTracker.TimeSinceLast = Time.unscaledTime;

            if (shakeCounter == 0)
                shakeTracker.TimeSinceFirst = shakeTracker.TimeSinceLast;

            shakeCounter++;
        }
        else if (Input.GetKeyDown(KeyCode.K))
        {
            Log.text = $"nothing yet";
        }

        CheckShakeInput();
        CheckTiltInput();
    }

    private void SendToClient(NetworkMessage message)
    {
        var client = GameObject.Find(StringConstants.Client)?.GetComponent<Client>();
        client?.SendServer(message);
    }

    private void CheckShakeInput()
    {
        if (Input.acceleration.sqrMagnitude >= sqrShakeDetectionThreshold
            && Time.unscaledTime >= shakeTracker.TimeSinceLast + minInputInterval)
        {
            shakeTracker.TimeSinceLast = Time.unscaledTime;

            if (shakeCounter == 0)
            {
                shakeTracker.TimeSinceFirst = shakeTracker.TimeSinceLast;
            }

            shakeCounter++;
        }
    }

    private void HandleShakeInput(int shakeCounter)
    {
        shakeTracker.TimeSinceLast = Time.unscaledTime;
        var shakeMessage = new ShakeMessage();
        shakeMessage.Count = shakeCounter;
        SendToClient(shakeMessage);
    }

    /// <summary>
    /// https://docs.unity3d.com/2019.4/Documentation/ScriptReference/Input-gyro.html
    /// </summary>
    private void CheckTiltInput()
    {
        if (Time.unscaledTime >= tiltTracker.TimeSinceLast + minInputInterval * 5)
        {
            var horizontalTilt = deviceGyroscope.attitude.x;

            if (Math.Abs(horizontalTilt) < tiltTracker.Threshold)
            {
                return;
            }

            tiltTracker.TimeSinceLast = Time.unscaledTime;

            var tiltMessage = new TiltMessage();
            tiltMessage.isLeft = horizontalTilt > 0;
            SendToClient(tiltMessage);
        }                
    }

    public void DisplayData()
    {
        Log.text += $"Add log info for tablet";
    }

    public void RefreshData()
    {
        Log.text = "";
    }

    /// <summary>
    /// Display menu on plane
    /// </summary>
    public void ToggleMenu()
    {

    }
}
