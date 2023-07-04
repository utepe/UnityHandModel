using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class TcpClientFunctional : MonoBehaviour
{
    private System.Net.Sockets.TcpClient socketConnection;
    private Thread clientReceiveThread;
    public int connectionPort = 25001;

    public string[] angles = new string[10];
    public float sensitivity = 0.01f;
    public Transform b_l_index1, b_l_index2, b_l_index3;

    // public Button calibrationButton;
    [SerializeField] 
    public Text textDisplay;

    // Might have to make this static
    private static int calibrationStep = 0;

    private int currentLineIndex = 1;

    public string[] calibrationStep1Lines;
    public string calibrationStep1FilePath = "Assets/Scripts/calibrationStep1.txt";

    public string[] calibrationStep2Lines;
    public string calibrationStep2FilePath = "Assets/Scripts/calibrationStep2.txt";

    public string[] calibrationStep3Lines;
    public string calibrationStep3FilePath = "Assets/Scripts/calibrationStep3.txt";

    void Start()
    {
        calibrationStep1Lines = File.ReadAllLines(calibrationStep1FilePath);
        calibrationStep2Lines = File.ReadAllLines(calibrationStep2FilePath);
        calibrationStep3Lines = File.ReadAllLines(calibrationStep3FilePath);
        
        ConnectToTcpServer();
    }

    private void ConnectToTcpServer()
    {
        try
        {
            clientReceiveThread = new Thread(new ThreadStart(ListenForData));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log("On client connect exception " + e);
        }
    }

    private void ListenForData()
    {
        try
        {
            socketConnection = new System.Net.Sockets.TcpClient("localhost", connectionPort);
            Byte[] bytes = new Byte[1024];
            while (true)
            {
                using (NetworkStream stream = socketConnection.GetStream())
                {
                    int length;
                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        var incommingData = new byte[length];
                        Array.Copy(bytes, 0, incommingData, 0, length);
                        string serverMessage = Encoding.ASCII.GetString(incommingData);
                        string[] parsedData = ParseData(serverMessage);
                        if (parsedData.Length > 1)
                        {
                            // Data represents angles
                            angles = parsedData;
                            Debug.Log("Received angles: " + serverMessage);
                        }
                        Debug.Log("server message received as: " + serverMessage);
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    private void SendMessage(string message)
    {
        if (socketConnection == null)
        {
            return;
        }
        try
        {
            NetworkStream stream = socketConnection.GetStream();

            if (stream.CanWrite)
            {
                byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(message);
                stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
                Debug.Log("Client sent this message: " + message);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket exception: " + socketException);
        }
    }

    // Parse the data received from the server
    public string[] ParseData(string dataString)
    {
        string[] data = dataString.Split(',');

        // Check if the data represents angles
        // TODO: Change this to parse based on what the Pico will send for angles
        if (data.Length > 1 && float.TryParse(data[0], out _) && float.TryParse(data[1], out _))
        {
            // Data represents angles, return it as is
            calibrationStep = 4;
            return data;
        }

        // Check if the data represents a calibration step
        // TODO: Change this to parse based on what the Pico will send to initiate calibration
        if (data.Length == 1)
        {
            string step = data[0];
            // Data represents a calibration step, update the calibrationStep variable
            if(step == "complete"){
                // TODO: When calibration step in incremented sendMessage to Pico to move to next step
                calibrationStep++;
                if(calibrationStep < 4){		        
                    textDisplay.text = "Calibration Step: " + calibrationStep + "Complete \nPress button to continue";
                }
                if(calibrationStep == 4){
		            textDisplay.text = "Calibration Complete!";
                    calibrationStep = 0;
                }
            }
            return new string[0];
        }

        // Return an empty array if the data doesn't match any recognized format
        return new string[0];
    }

    void Update()
    {
        // Move finger based on calibration step
        switch (calibrationStep)
        {
            case 0:
                // IDLE state
                // Do nothing during the initial default state
                break;
            case 1:
                // Perform calibration step 1
                CalibrationStep1();
                break;
            case 2:
                // Perform calibration step 2
                CalibrationStep2();
                break;
            case 3:
                // Perform calibration step 3
                CalibrationStep3();
                break;
            default:
                // Default state after calibration is done
                ProcessAngles(angles);
                break;
        }
    }

    // TODO: send mode back to 0 after each calbration step is completed, wait for user to press button and send message back to Pico to tell it to move forward

    // MCP changing, PIP fixed @ 0
    private void CalibrationStep1()
    {
        textDisplay.text = "Calibration Step 1 \nMCP changing, PIP fixed @ 0";
        HandleCalibrationFile(calibrationStep1Lines);
    }

    // MCP fixed @ 0, PIP changing
    private void CalibrationStep2()
    {
        textDisplay.text = "Calibration Step 2 \nMCP fixed @ 0, PIP changing";
        HandleCalibrationFile(calibrationStep2Lines);
    }

    // MCP fixed @ 90, PIP changing 
    private void CalibrationStep3()
    {
        textDisplay.text = "Calibration Step 3 \nMCP fixed @ 90, PIP changing";
        HandleCalibrationFile(calibrationStep3Lines);
    }

    // TODO: make this a function that takes in array of angles and updates the finger positions
    // Call this method after for all different modes
    private void ProcessAngles(string[] angles)
    {
        // Implementation for reading angles from Pico after calibration is done
        // Modify the code based on your specific requirements
        float index_mcp_angle = float.Parse(angles[0]);
        float index_pip_angle = float.Parse(angles[1]);
        float index_dip_angle = float.Parse(angles[1]);

        b_l_index1.transform.localEulerAngles = new Vector3(-90, 0, 180 - index_mcp_angle);
        b_l_index2.transform.localEulerAngles = new Vector3(0, 0, -index_pip_angle);
        b_l_index3.transform.localEulerAngles = new Vector3(0, 0, -index_dip_angle);

        Debug.Log("Read angles from Pico");
    }

    private void HandleCalibrationFile(string[] lines)
    {
        if(currentLineIndex < lines.Length){
            string line = lines[currentLineIndex];

            string[] angles = line.Split(',');
            
            ProcessAngles(angles);

            currentLineIndex++;
        }
        else{
            Debug.Log("Finished reading calibration file");
            currentLineIndex = 1;
        }
    }

    // TODO: change this to updateCalibration and update the message sent every time calibration step is done
    private void OnCalibrationButtonPress()
    {
        if(calibrationStep == 0 || calibrationStep == 4){
            SendMessage("calibration");
            calibrationStep = 1;
		    textDisplay.text = "Calibration Mode";
        }
        if(calibrationStep < 4){
            SendMessage("calibration_step_" + calibrationStep);
        }
    }

    private void OnUnityModeButtonPress()
    {
        // Send a special string to middleware to start unity mode
        SendMessage("unityMode");
		textDisplay.text = "Unity Display Mode";
    }
}
