using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using UnityEngine;

public class HandControllerPico : MonoBehaviour
{
    private SerialPort serialPort;
    // private string receivedString;
    // private string[] angles;

    public float sensitivity = 0.01f;
    public Transform b_l_index1, b_l_index2, b_l_index3;

    private void Start()
    {
        string portName = "COM7";
        int baudRate = 115200;

        // Open the serial port
        serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        try
        {
            serialPort.Open();
            Debug.Log("Serial port opened: " + portName);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to open the serial port: " + ex.Message);
        }

        InvokeRepeating("SerialDataReading", 0f, 0.01f);
    }

    // Update is called once per frame
    void Update()
    {
        string[] angles = SerialDataReading();

        float index_mcp_angle = float.Parse(angles[0]);
        float index_pip_angle = float.Parse(angles[1]);
        float index_dip_angle = float.Parse(angles[1]);

        b_l_index1.transform.localEulerAngles = new Vector3(-90, 0, 180-index_mcp_angle);
        b_l_index2.transform.localEulerAngles = new Vector3(0, 0, -index_pip_angle);
        b_l_index3.transform.localEulerAngles = new Vector3(0, 0, -index_dip_angle);
        
        // string inputString = stream.ReadLine();
    }

    // sync unity and serial port frequency 
    string[] SerialDataReading(){
        // Read data from the serial port
        // byte[] buffer = new byte[1024];
        // int bytesRead = serialPort.Read(buffer, 0, buffer.Length);

        // recievedString = System.Text.Encoding.ASCII.GetString(buffer, 0, bytesRead);
        string recievedString = serialPort.ReadLine();


        return recievedString.Split(',');
    }
}
