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

public class TcpClient : MonoBehaviour
{
    private System.Net.Sockets.TcpClient socketConnection; 	
	private Thread clientReceiveThread; 
    public int connectionPort = 80;

    public float sensitivity = 0.01f;

    Boolean socketExceptionFlag;

	public string[] recievedAngles = new string[10];
	public Transform b_l_thumb1, b_l_thumb2, b_l_thumb3;
    public Transform b_l_index1, b_l_index2, b_l_index3;
    public Transform b_l_middle1, b_l_middle2, b_l_middle3;
    public Transform b_l_ring1, b_l_ring2, b_l_ring3;
    public Transform b_l_pinky1, b_l_pinky2, b_l_pinky3;

	[SerializeField] 
    public Text textDisplay;

	// Might have to make this static
    private static int calibrationStep = 1;
    private static int currentStep = 0;

    private int currentLineIndex = 1;

    public string[] calibrationStep1Lines;
    public string calibrationStep1FilePath = "Assets/Scripts/calibrationStep1.txt";

    public string[] calibrationStep2Lines;
    public string calibrationStep2FilePath = "Assets/Scripts/calibrationStep2.txt";

    public string[] calibrationStep3Lines;
    public string calibrationStep3FilePath = "Assets/Scripts/calibrationStep3.txt";

    void Start () {
        Application.targetFrameRate = 30; // Limit frame rate to 30 FPS

        calibrationStep1Lines = File.ReadAllLines(calibrationStep1FilePath);
        calibrationStep2Lines = File.ReadAllLines(calibrationStep2FilePath);
        calibrationStep3Lines = File.ReadAllLines(calibrationStep3FilePath);

		ConnectToTcpServer();
	}

	public void OnCalibrationButtonPress()
    {
        if(currentStep != 1 || currentStep != 2 || currentStep != 3){
            SendMessage("calibration");
		    textDisplay.text = "Calibration Mode";
        }
        currentStep = calibrationStep;
        if(currentStep < 4){
            SendMessage("calibration_step_" + calibrationStep);
        }
        Debug.Log(currentStep);
    }

    public void OnUnityModeButtonPress()
    {
        if(currentStep != 4){
            currentStep = 4;
            // Send a special string to middleware to start unity mode
            SendMessage("unityMode");
        }
        else{
            SendMessage("stopSending");
            currentStep = 0;
        }
    }  	

    public void OnWBAModeButtonPress()
    {
        if(currentStep != 5){
           currentStep = 5;
            // Send a special string to middleware to start WBA mode
            SendMessage("WBAMode");
        }
        else{
            SendMessage("stopSending");
            currentStep = 0;
        }
    }  	

	private void ConnectToTcpServer () { 		
		try {  			
			clientReceiveThread = new Thread(new ThreadStart(ListenForData)); 			
			clientReceiveThread.IsBackground = true; 			
			clientReceiveThread.Start();  		
		} 		
		catch (Exception e) { 			
			Debug.Log("On client connect exception " + e); 		
		} 	
	} 

    private void ListenForData()
    {
        try
        {
            socketConnection = new System.Net.Sockets.TcpClient("192.168.1.131", connectionPort);
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
                            recievedAngles = parsedData;
                        }
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            socketExceptionFlag = true;
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

    public string[] ParseData(string dataString)
    {
        int firstNewLineIndex = dataString.IndexOf('\n');

        // Check if the data represents angles
        if (firstNewLineIndex >= 0)
        {
            string trimmedData = dataString.Substring(0, firstNewLineIndex);
            string[] data = trimmedData.Split(',');

            if(data.Length != 10){
                int secondNewLineIndex = dataString.IndexOf('\n', firstNewLineIndex+1);
                if(secondNewLineIndex >= 0){
                    string trimmedData2 = dataString.Substring(firstNewLineIndex+1, secondNewLineIndex-firstNewLineIndex-1);
                    data = trimmedData2.Split(',');
                }
            }

            // Data represents angles, return it as is
            return data;
        }

        // Check if the data represents a calibration step
        if (dataString.Contains("complete"))
        {
            // Data represents a calibration step, update the calibrationStep variable
            calibrationStep++;
            currentStep = 0;

            if(calibrationStep == 4){
                calibrationStep = 1;
                currentStep = 0;
            }
            return new string[0];
        }

        // Return an empty array if the data doesn't match any recognized format
        return new string[0];
    }

	/// Update is called once per frame
    void Update()
    {
        // Move finger based on calibration step
        switch (currentStep)
        {
            case 0:
                // IDLE state
                // Do nothing during the initial default state
                textDisplay.text = "WBA Capstone IDLE Mode";
                ProcessAngles(new string[10] {"0", "0", "0", "0", "0", "0", "0", "0", "0", "0"});

                if(1 < calibrationStep && calibrationStep < 4){
                    textDisplay.text = "Calibration Step: " + (calibrationStep-1) + " Complete \nPress button to continue";
                }
                else if(calibrationStep == 4){
                    textDisplay.text = "Calibration Complete!";
                }

                if(socketExceptionFlag){
                    textDisplay.text = "Socket Exception: Connection Attempt Failed \nCheck Connection";
                }

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
            case 4:
                // VR Mode: Move finger based on recieved angles
		        textDisplay.text = "Unity Display Mode \nPress Button to return to IDLE Mode";
                ProcessAngles(recievedAngles);
                break;
            case 5:
                // WBA Mode
                textDisplay.text = "WBA Mode \nPlease direct attention to WBA Hand";
                ProcessAngles(new string[10] {"0", "0", "0", "0", "0", "0", "0", "0", "0", "0"});
                break;
            default:
                break;
        }
    }

	// MCP changing, PIP fixed @ 0
    private void CalibrationStep1()
    {
        textDisplay.text = "Calibration Step 1 \nPlease mimic this hand motion";
        HandleCalibrationFile(calibrationStep1Lines);
    }

    // MCP fixed @ 0, PIP changing
    private void CalibrationStep2()
    {
        textDisplay.text = "Calibration Step 2 \nPlease mimic this hand motion";
        HandleCalibrationFile(calibrationStep2Lines);
    }

    // MCP fixed @ 90, PIP changing 
    private void CalibrationStep3()
    {
        textDisplay.text = "Calibration Step 3 \nPlease mimic this hand motion";
        HandleCalibrationFile(calibrationStep3Lines);
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

	private void ProcessAngles(string[] angles)
    {
        try{
            // float thumb_mcp_angle = float.Parse(angles[0]);
            float thumb_pip_angle = float.Parse(angles[0]);
            float thumb_dip_angle = float.Parse(angles[1]);

            float index_mcp_angle = float.Parse(angles[2]);
            float index_pip_angle = float.Parse(angles[3]);
            float index_dip_angle = float.Parse(angles[3]);

            float middle_mcp_angle = float.Parse(angles[4]);
            float middle_pip_angle = float.Parse(angles[5]);
            float middle_dip_angle = float.Parse(angles[5]);

            float ring_mcp_angle = float.Parse(angles[6]);
            float ring_pip_angle = float.Parse(angles[7]);
            float ring_dip_angle = float.Parse(angles[7]);

            float pinky_mcp_angle = float.Parse(angles[8]);
            float pinky_pip_angle = float.Parse(angles[9]);
            float pinky_dip_angle = float.Parse(angles[9]);

            b_l_thumb1.transform.localEulerAngles = new Vector3(0, 180, 45);
            b_l_thumb2.transform.localEulerAngles = new Vector3(-15, 0, -thumb_pip_angle);
            b_l_thumb3.transform.localEulerAngles = new Vector3(0, 0, -thumb_dip_angle);

            b_l_index1.transform.localEulerAngles = new Vector3(-90, 0, 180 - index_mcp_angle);
            b_l_index2.transform.localEulerAngles = new Vector3(0, 0, -index_pip_angle);
            b_l_index3.transform.localEulerAngles = new Vector3(0, 0, -index_dip_angle);

            b_l_middle1.transform.localEulerAngles = new Vector3(-90, 0, 180 - middle_mcp_angle);
            b_l_middle2.transform.localEulerAngles = new Vector3(0, 0, -middle_pip_angle);
            b_l_middle3.transform.localEulerAngles = new Vector3(0, 0, -middle_dip_angle);
            
            b_l_ring1.transform.localEulerAngles = new Vector3(-90, 0, 180 - ring_mcp_angle);
            b_l_ring2.transform.localEulerAngles = new Vector3(0, 0, -ring_pip_angle);
            b_l_ring3.transform.localEulerAngles = new Vector3(0, 0, -ring_dip_angle);
            
            b_l_pinky1.transform.localEulerAngles = new Vector3(0, 0, 180 - pinky_mcp_angle);
            b_l_pinky2.transform.localEulerAngles = new Vector3(0, 0, -pinky_pip_angle);
            b_l_pinky3.transform.localEulerAngles = new Vector3(0, 0, -pinky_dip_angle);
        }
        catch(Exception e){
            Debug.Log("Exception" + e + "\n Angles: "+ string.Join(", ", angles));
        }
        
    }

}
