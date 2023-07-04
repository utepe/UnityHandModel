using System;
using System.Collections; 
using System.Collections.Generic; 
using System.Net; 
using System.Net.Sockets; 
using System.Text; 
using System.Threading; 
using UnityEngine;  

public class TcpServer : MonoBehaviour
{
    Thread tcpListenerThread;
    TcpListener tcpListener;
    System.Net.Sockets.TcpClient connectedTcpClient;
    public int connectionPort = 25001;

    string[] angles = {"0", "0"};
    public float sensitivity = 0.01f;
    public Transform b_l_index1, b_l_index2, b_l_index3;

    void Start()
    {
        // Start the server background thread
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequests));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();
    }

    private void ListenForIncommingRequests() {
        try {
            tcpListener = new TcpListener(IPAddress.Any, connectionPort);
            tcpListener.Start();
            Debug.Log("Server is listening");
            Byte[] bytes = new Byte[1024];
            while (true) {
                using (connectedTcpClient = tcpListener.AcceptTcpClient()) {
                    using (NetworkStream stream = connectedTcpClient.GetStream()) {
                        int length;
                        while ((length = stream.Read(bytes, 0, bytes.Length)) != 0) {
                            var incommingData = new byte[length];
                            Array.Copy(bytes, 0, incommingData, 0, length);
                            string clientMessage = Encoding.ASCII.GetString(incommingData);
                            // parse string of data into an array of angles
                            angles = ParseData(clientMessage);
                            Debug.Log("client message received as: " + clientMessage);
                        }
                    }
                }
            }
        } catch (SocketException socketException) {
            Debug.Log("Socket exception: " + socketException.ToString());
        }
    }

    private void SendMessage() { 		
		if (connectedTcpClient == null) {             
			return;         
		}  		
		
		try { 			
			// Get a stream object for writing. 			
			NetworkStream stream = connectedTcpClient.GetStream(); 			
			if (stream.CanWrite) {                 
				string serverMessage = "This is a message from your server."; 			
				// Convert string message to byte array.                 
				byte[] serverMessageAsByteArray = Encoding.ASCII.GetBytes(serverMessage); 				
				// Write byte array to socketConnection stream.               
				stream.Write(serverMessageAsByteArray, 0, serverMessageAsByteArray.Length);               
				Debug.Log("Server sent his message - should be received by client");           
			}       
		} 		
		catch (SocketException socketException) {             
			Debug.Log("Socket exception: " + socketException);         
		} 	
	}

    public static string[] ParseData(string dataString)
    {
        // Debug.Log(dataString);
        // Remove the parentheses
        return dataString.Split(',');
    }

    // Update is called once per frame
    void Update()
    {
        float index_mcp_angle = float.Parse(angles[0]);
        float index_pip_angle = float.Parse(angles[1]);
        float index_dip_angle = float.Parse(angles[1]);

        b_l_index1.transform.localEulerAngles = new Vector3(-90, 0, 180-index_mcp_angle);
        b_l_index2.transform.localEulerAngles = new Vector3(0, 0, -index_pip_angle);
        b_l_index3.transform.localEulerAngles = new Vector3(0, 0, -index_dip_angle);
        // if no issues with the connection, send a message to the client
        SendMessage();
    }
}
