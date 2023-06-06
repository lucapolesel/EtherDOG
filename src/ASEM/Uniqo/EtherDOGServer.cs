#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using QPlatform.HMIProject;
using QPlatform.EventLogger;
using QPlatform.NetLogic;
using QPlatform.NativeUI;
using QPlatform.UI;
using QPlatform.Recipe;
using QPlatform.SQLiteStore;
using QPlatform.Store;
using QPlatform.OmronEthernetIP;
using QPlatform.Retentivity;
using QPlatform.CoreBase;
using QPlatform.CommunicationDriver;
using QPlatform.Alarm;
using QPlatform.Core;
using EtherDOGNET;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Text;
#endregion

public class EtherDOGServer : BaseNetLogic
{
    private EtherDOG _server;

    private UAVariable _connectedNode;
    private Folder _receivedNode;
    private int _receivedDataSize;
    private Folder _sendNode;

    private int _stringLength = 64;

    public override void Start()
    {
        var pConnected = LogicObject.GetVariable("ConnectedNode")?.Value;
        var pReceived = LogicObject.GetVariable("ReceivedNode")?.Value;
        var pSend = LogicObject.GetVariable("SendNode")?.Value;

        var serverIp = LogicObject.GetVariable("IP")?.Value;
        var serverPort = LogicObject.GetVariable("Port")?.Value;

        _receivedDataSize = LogicObject.GetVariable("ReceivedDataSize")?.Value;
        _stringLength = LogicObject.GetVariable("StringLength")?.Value;

        if (_stringLength < 1)
        {
            // TODO: Throw some kind of exception instead
            _stringLength = 64;
        }

        _connectedNode = InformationModel.Get(pConnected) as UAVariable;
        _receivedNode = InformationModel.Get(pReceived) as Folder;
        _sendNode = InformationModel.Get(pSend) as Folder;

        _server = new EtherDOG(serverIp, serverPort);

        _server.OnLog += OnLog;
        _server.OnStatusChanged += OnStatusChanged;
        _server.OnReceivedData += OnReceivedData;
        _server.OnSendData += OnSendData;

        _server.Start();
    }

    private void OnLog(string message)
    {
        //
    }

    private void OnStatusChanged(bool connected)
    {
        if (_connectedNode != null)
        {
            _connectedNode.Value = connected;
        }
    }

    private void OnReceivedData(byte[] data)
    {
        if (_receivedNode != null)
        {
            // Check the data size
            if (data.Length != _receivedDataSize)
                return;

            int curIndex = 0;

            foreach (UAVariable item in GetVariables(_receivedNode))
            {
                var varSize = GetValueSize(item);

                UpdateFromBytes(item, CopyFromArray(data, curIndex, varSize));

                curIndex += varSize;
            }
        }
    }

    private byte[] OnSendData()
    {
        var data = new List<byte>();

        if (_sendNode != null)
        {
            // Format all the data in the node and send it to the PLC
            foreach (UAVariable item in GetVariables(_sendNode))
            {
                data.AddRange(GetValueBytes(item));
            }
        }

        return data.ToArray();
    }

    private static UAVariable[] GetVariables(UANode folder)
    {
        var variables = new List<UAVariable>();

        if (folder != null)
        {
            foreach (UANode node in folder.Children.Cast<UANode>())
            {
                if (node is UAVariable)
                {
                    variables.Add((UAVariable)node);
                }
                else if (node is Folder)
                {
                    variables.AddRange(GetVariables(node));
                }
            }
        }

        return variables.ToArray();
    }

    private static string GetStringFromArray(byte[] data, char divider)
    {
        var str = Encoding.UTF8.GetString(data);
        var divIndex = str.IndexOf(divider);
        return divIndex != -1 ? str.Substring(0, str.IndexOf(divider)) : str;
    }

    private static byte[] StringToFixedByteArray(string str, int length)
    {
        var bytes = Encoding.UTF8.GetBytes(str ?? "");
        Array.Resize(ref bytes, length);
        return bytes;
    }
    private static byte[] CopyFromArray(byte[] arr, int startIndex, int length)
    {
        byte[] rArr = new byte[length];
        Array.Copy(arr, startIndex, rArr, 0, length);
        return rArr;
    }

    private static int GetValueSize(UAVariable value)
    {
        int dataSize = 0;
        var dataType = variable.DataType;

        if (dataType == OpcUa.DataTypes.Boolean)
        {
            dataSize += 1;
        }
        else if (dataType == OpcUa.DataTypes.Byte)
        {
            dataSize += 1;
        }
        else if (dataType == OpcUa.DataTypes.Float)
        {
            dataSize += 4;
        }
        else if (dataType == OpcUa.DataTypes.Int16)
        {
            dataSize += 2;
        }
        else if (dataType == OpcUa.DataTypes.Int32)
        {
            dataSize += 4;
        }
        else if (dataType == OpcUa.DataTypes.String)
        {
            dataSize += _stringLength;
        }
        else if (dataType == OpcUa.DataTypes.UInt16)
        {
            dataSize += 2;
        }
        else if (dataType == OpcUa.DataTypes.UInt32)
        {
            dataSize += 4;
        }

        return dataSize;
    }

    private static byte[] GetValueBytes(UAVariable variable)
    {
        var dataType = variable.DataType;
        var dataValue = variable.Value;

        if (dataType == OpcUa.DataTypes.Boolean)
        {
            return BitConverter.GetBytes((bool)dataValue);
        }
        else if (dataType == OpcUa.DataTypes.Byte)
        {
            return new byte[] { (byte)dataValue };
        }
        else if (dataType == OpcUa.DataTypes.Float)
        {
            return BitConverter.GetBytes((float)dataValue);
        }
        else if (dataType == OpcUa.DataTypes.Int16)
        {
            return BitConverter.GetBytes((short)dataValue);
        }
        else if (dataType == OpcUa.DataTypes.Int32)
        {
            return BitConverter.GetBytes((int)dataValue);
        }
        else if (dataType == OpcUa.DataTypes.String)
        {
            return StringToFixedByteArray((string)dataValue, _stringLength);
        }
        else if (dataType == OpcUa.DataTypes.UInt16)
        {
            return BitConverter.GetBytes((ushort)dataValue);
        }
        else if (dataType == OpcUa.DataTypes.UInt32)
        {
            return BitConverter.GetBytes((uint)dataValue);
        }

        return Array.Empty<byte>();
    }

    private static void UpdateFromBytes(UAVariable variable, byte[] bytes)
    {
        var dataType = variable.DataType;

        if (dataType == OpcUa.DataTypes.Boolean)
        {
            variable.Value = BitConverter.ToBoolean(bytes);
        }
        else if (dataType == OpcUa.DataTypes.Byte)
        {
            variable.Value = bytes[0];
        }
        else if (dataType == OpcUa.DataTypes.Float)
        {
            variable.Value = BitConverter.ToSingle(bytes);
        }
        else if (dataType == OpcUa.DataTypes.Int16)
        {
            variable.Value = BitConverter.ToInt16(bytes);
        }
        else if (dataType == OpcUa.DataTypes.Int32)
        {
            variable.Value = BitConverter.ToInt32(bytes);
        }
        else if (dataType == OpcUa.DataTypes.String)
        {
            variable.Value = GetStringFromArray(bytes, '\0');
        }
        else if (dataType == OpcUa.DataTypes.UInt16)
        {
            variable.Value = BitConverter.ToUInt16(bytes);
        }
        else if (dataType == OpcUa.DataTypes.UInt32)
        {
            variable.Value = BitConverter.ToInt32(bytes);
        }
    }

    public override void Stop()
    {
        if (_server != null)
        {
            _server.OnLog -= OnLog;
            _server.OnStatusChanged -= OnStatusChanged;
            _server.OnReceivedData -= OnReceivedData;
            _server.OnSendData -= OnSendData;

            _server.Stop();
        }
    }
}
