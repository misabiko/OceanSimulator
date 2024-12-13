using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using UnityEngine;
using static Ressources;
using System.Linq;

public class DSXManager : MonoBehaviour
{
    static UdpClient client;
    static IPEndPoint endPoint;

    static DateTime TimeSent;

    static List<Device> devices = new List<Device>();
    public static DSXManager instance {  get; private set; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("More than one DSX manager in the scene");
        }
        instance = this;
        Connect();
        GetConnectedDevicesFromDSX();
        if (!devices.Any())
        {
            GetConnectedDevicesFromDSX();
        }
    }
    public void changeTrigger(int mode)
    {
        TriggerMode t = TriggerMode.Soft;

        switch(mode)
        {
            case 1: t = TriggerMode.Hard;
            break;
            case 2: t = TriggerMode.Soft;
            break;

        }

        for (int i = 0; i < devices.Count; i++)
        {
            Packet packet = new Packet();

            int controllerIndex = devices[i].Index;

            packet = AddAdaptiveTriggerToPacket(packet, controllerIndex, Trigger.Right, t, new List<int>());
            packet = AddAdaptiveTriggerToPacket(packet, controllerIndex, Trigger.Left, t, new List<int>());

            SendDataToDSX(packet);
        }
    }

    /// <summary>
    /// Establishes a connection to the DSX server on a specified port.
    /// Initializes the UDP client and endpoint for communication with the server.
    /// </summary>
    static void Connect()
    {
        try
        {
            var port = FetchPortNumber();
            Console.WriteLine($"Connecting to Server on Port: {port}\n");
            client = new UdpClient();
            endPoint = new IPEndPoint(Triggers.localhost, port);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

    }

    /// <summary>
    /// Fetches the UDP port number from a configuration file in the AppData\Local\DSX directory.
    /// If the file is not found, contains invalid data, or an error occurs, it falls back to a default port number.
    /// Provides logging for all relevant steps and potential issues.
    /// </summary>
    /// <returns>The port number to use for communication (default: 6969 if an error occurs).</returns>
    static int FetchPortNumber()
    {
        // ONLY WORKS WITH DSX v3.1 BETA 1.37 AND ABOVE

        const int defaultPort = 6969;
        const string appFolderName = "DSX";
        const string fileName = "DSX_UDP_PortNumber.txt";

        try
        {
            Console.WriteLine("Fetching Port Number locally...");

            // Get the Local AppData path for the application
            string localAppDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appFolderName
            );

            string portFilePath = Path.Combine(localAppDataPath, fileName);

            // Check if the file exists
            if (File.Exists(portFilePath))
            {
                Console.WriteLine($"Port file found at: {portFilePath}");

                // Try to read and parse the port number
                string portNumberContent = File.ReadAllText(portFilePath).Trim();
                if (int.TryParse(portNumberContent, out int portNumber))
                {
                    Console.WriteLine($"Port Number successfully read: {portNumber}");
                    return portNumber;
                }
                else
                {
                    Console.WriteLine($"Invalid port number format in file: {portNumberContent}");
                }
            }
            else
            {
                Console.WriteLine($"Port file not found at: {portFilePath}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred while fetching the port number: {ex.Message}");
        }

        // Fallback to default port number
        Console.WriteLine($"Falling back to default port number: {defaultPort}");
        return defaultPort;
    }

    /// <summary>
    /// Sends a packet of data to the DSX server.
    /// Converts the packet to a JSON string, sends it via UDP, and logs the time the data was sent.
    /// </summary>
    /// <param name="data">The packet of data to be sent to the DSX server.</param>
    static void SendDataToDSX(Packet data)
    {
        try
        {
            var RequestData = Encoding.ASCII.GetBytes(Triggers.PacketToJson(data));
            client.Send(RequestData, RequestData.Length, endPoint);
            TimeSent = DateTime.Now;
            Console.WriteLine($"Instructions Sent at {DateTime.Now} with data: ({Triggers.PacketToJson(data)})\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }

    }

    /// <summary>
    /// Receives and processes data from the DSX server.
    /// Deserializes the JSON response from the server, logs the information about connected devices,
    /// and updates the device list with the data received.
    /// </summary>
    static void GetDataFromDSX()
    {
        Console.WriteLine("Waiting for Server Response...\n");

        try
        {
            // Receive the response bytes from the server.
            byte[] bytesReceivedFromServer = client.Receive(ref endPoint);

            // Check if the server has sent a response.
            if (bytesReceivedFromServer.Length > 0)
            {
                // Deserialize the received JSON response into a ServerResponse object.
                ServerResponse ServerResponseJson = JsonConvert.DeserializeObject<ServerResponse>(
                    Encoding.ASCII.GetString(bytesReceivedFromServer, 0, bytesReceivedFromServer.Length));

                // Print a visual separator in the console for better readability.
                Console.WriteLine("===================================================================");

                // Capture the current time to calculate the response time.
                DateTime CurrentTime = DateTime.Now;
                TimeSpan Timespan = CurrentTime - TimeSent;

                // Log the status and response time from the server.
                Console.WriteLine($"Status                  - {ServerResponseJson.Status}");
                Console.WriteLine($"Time Received           - {ServerResponseJson.TimeReceived}, took: {Timespan.TotalMilliseconds} ms to receive response from DSX");
                Console.WriteLine($"isControllerConnected   - {ServerResponseJson.isControllerConnected}");
                Console.WriteLine($"BatteryLevel            - {ServerResponseJson.BatteryLevel}\n");

                // Log the number of devices connected to the server (DSX).
                Console.WriteLine($"Devices Connected to DSX: {ServerResponseJson.Devices.Count}");

                // Clear the existing list of devices before populating it with new data.
                devices.Clear();

                // Iterate through each device in the server's response and log its details.
                foreach (Device device in ServerResponseJson.Devices)
                {
                    // Add the device to the devices list.
                    devices.Add(device);

                    // Log the device's properties.
                    Console.WriteLine("-------------------------------");
                    Console.WriteLine($"Controller Index        - {device.Index}");
                    Console.WriteLine($"MacAddress              - {device.MacAddress}");
                    Console.WriteLine($"DeviceType              - {device.DeviceType}");
                    Console.WriteLine($"ConnectionType          - {device.ConnectionType}");
                    Console.WriteLine($"BatteryLevel            - {device.BatteryLevel}");
                    Console.WriteLine($"IsSupportAT             - {device.IsSupportAT}");
                    Console.WriteLine($"IsSupportLightBar       - {device.IsSupportLightBar}");
                    Console.WriteLine($"IsSupportPlayerLED      - {device.IsSupportPlayerLED}");
                    Console.WriteLine($"IsSupportMicLED         - {device.IsSupportMicLED}");
                    Console.WriteLine("-------------------------------\n");
                }

                // Print a closing visual separator for better readability.
                Console.WriteLine("===================================================================\n");
            }
        }
        catch (Exception ex)
        {
            // Log any exceptions that occur during the process.
            // Possible that DSX Server is not running, or DSX is not running at all.
            Console.WriteLine(ex);
        }
    }

    /// <summary>
    /// Sends a request to the DSX server to retrieve information about connected devices.
    /// Combines several steps: prepares the packet, sends it to the server, and then retrieves the data.
    /// </summary>
    static void GetConnectedDevicesFromDSX()
    {
        // Get Data from DSX first about connected devices
        Packet packet = new Packet();

        packet = AddGetDSXStatusToPacket(packet);

        SendDataToDSX(packet);

        GetDataFromDSX();
    }

    /// <summary>
    /// Adds an adaptive trigger instruction to the packet for a specified controller index.
    /// This instruction configures the trigger mode and parameters for the adaptive trigger.
    /// </summary>
    /// <param name="packet">The packet to which the instruction will be added.</param>
    /// <param name="controllerIndex">The index of the controller to apply the trigger instruction.</param>
    /// <param name="trigger">The trigger (e.g., left or right trigger) to be configured.</param>
    /// <param name="triggerMode">The mode to set for the adaptive trigger.</param>
    /// <param name="parameters">Additional parameters required by the trigger mode.</param>
    /// <returns>Returns the packet with the adaptive trigger instruction added.</returns>
    static Packet AddAdaptiveTriggerToPacket(Packet packet, int controllerIndex, Trigger trigger, TriggerMode triggerMode, List<int> parameters)
    {
        int instCount;

        if (packet.instructions == null)
        {
            packet.instructions = new Instruction[1];
            instCount = 0;
        }
        else
        {
            instCount = packet.instructions.Length;
            Array.Resize(ref packet.instructions, instCount + 1);
        }

        // Combine the fixed and variable parameters
        var combinedParameters = new object[3 + parameters.Count];
        combinedParameters[0] = controllerIndex;
        combinedParameters[1] = trigger;
        combinedParameters[2] = triggerMode;

        // Copy the List<int> parameters into the combinedParameters array
        for (int i = 0; i < parameters.Count; i++)
        {
            combinedParameters[3 + i] = parameters[i];
        }

        packet.instructions[instCount] = new Instruction
        {
            type = InstructionType.TriggerUpdate,
            parameters = combinedParameters
        };

        return packet;
    }

    
    static Packet AddTriggerThresholdToPacket(Packet packet, int controllerIndex, Trigger trigger, int threshold)
    {
        int instCount;

        if (packet.instructions == null)
        {
            packet.instructions = new Instruction[1];
            instCount = 0;
        }
        else
        {
            instCount = packet.instructions.Length;
            Array.Resize(ref packet.instructions, instCount + 1);
        }

        packet.instructions[instCount] = new Instruction
        {
            type = InstructionType.TriggerThreshold,
            parameters = new object[] { controllerIndex, trigger, threshold }
        };

        return packet;
    }

    static Packet AddGetDSXStatusToPacket(Packet packet)
    {
        int instCount;

        if (packet.instructions == null)
        {
            packet.instructions = new Instruction[1];
            instCount = 0;
        }
        else
        {
            instCount = packet.instructions.Length;
            Array.Resize(ref packet.instructions, instCount + 1);
        }

        packet.instructions[instCount] = new Instruction
        {
            type = InstructionType.GetDSXStatus,
            parameters = new object[] { }
        };

        return packet;
    }

}

