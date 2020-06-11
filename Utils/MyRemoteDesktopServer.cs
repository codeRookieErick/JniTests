﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScriptsManager.Utils
{
    public class MyRemoteDesktopServer:IDisposable
    {
        SocketsLayerWithQueue SocketsLayerWithQueue;
        public event EventHandler<RemoteServerAction> ActionRequestReceived;
        public event EventHandler<(string message, Exception e)> SocketExceptionReceived;
        public MyRemoteDesktopServer(int sendPort, int receivePort)
        {
            SocketsLayerWithQueue = new SocketsLayerWithQueue(sendPort, receivePort, callback: DataReceived);
            SocketsLayerWithQueue.ExceptionCatched += (o, e) => this.SocketExceptionReceived?.Invoke(o, e);
        }

        public void DataReceived(byte[] data)
        {
            RemotePacket packet = Deserialize<RemotePacket>(data);
            if(packet != default)
            {
                ActionRequestReceived?.Invoke(this, packet.Action);
                switch (packet.Action)
                {
                    case RemoteServerAction.GetMousePosition:
                        
                        Send(Serialize(new RemotePacket
                        {
                            Action = RemoteServerAction.GetMousePosition,
                            Data = Serialize(Cursor.Position),
                            DataType = typeof(Point)
                        }));
                        break;

                    case RemoteServerAction.GetScreen:
                        
                        Bitmap bitmap = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
                        Graphics.FromImage(bitmap).CopyFromScreen(new Point(), new Point(), bitmap.Size);
                        Send(Serialize(new RemotePacket { 
                            Action = RemoteServerAction.GetScreen,
                            Data = Serialize(bitmap),
                            DataType = typeof(Bitmap)
                        }));
                        break;

                    case RemoteServerAction.SendKeys:
                        if(packet.DataType == typeof(Keys))
                        {
                            Keys keys = Deserialize<Keys>(packet.Data);
                            
                        }
                        break;

                    case RemoteServerAction.SetMousePosition:
                        if (packet.DataType == typeof(Point))
                        {
                            Cursor.Position = Deserialize<Point>(packet.Data);
                        }
                        break;
                }
            }
        }

        public void Send(byte[] data)
        {
            SocketsLayerWithQueue.EnqueueSend(data);
        }
        public Point GetMousePosition()
        {
            return Cursor.Position;
        }

        public void Dispose()
        {
            SocketsLayerWithQueue?.Dispose();
        }

        public T Deserialize<T>(byte[] data)
        {
            T packet = default;
            try
            {
                packet = (T)(new BinaryFormatter().Deserialize(new MemoryStream(data)));
            }
            catch (Exception)
            {

            }
            return packet;
        }

        public byte[] Serialize<T>(T graph)
        {
            MemoryStream memoryStream = new MemoryStream();
            new BinaryFormatter().Serialize(memoryStream, graph);
            return memoryStream.ToArray();
        }
    }

    [Serializable]
    public class RemotePacket
    {
        public RemoteServerAction Action { get; set; }
        public Type DataType { get; set; }
        public byte[] Data{get;set;}
    }

    [Serializable]
    public enum RemoteServerAction
    {
        GetScreen,
        GetMousePosition,
        SetMousePosition,
        SendKeys
    }

}
