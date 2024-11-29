using System;
using System.Net.Sockets;
using UnityEngine;
using System.Collections.Generic;
public class ClientSocket
{
    public bool connected = false;

    private byte[] m_recvBuff;
    private AsyncCallback m_recvCb;
    private Queue<string> m_msgQueue = new Queue<string>();
    private Socket m_socket;
    private Socket init()
    {
        Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        m_recvBuff = new byte[0x4000];
        m_recvCb = new AsyncCallback(RecvCallBack);
        return clientSocket;
    }
    
    public void Connect(string host, int port)
    {
        if (m_socket == null)
            m_socket = init();
        try
        {
            Debug.Log("connect: " + host + ":" + port);
            m_socket.SendTimeout = 3;
            m_socket.Connect(host, port);
            connected = true;

        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }
    
    public void SendData(string msg)
    {
        if (connected)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(msg);
            m_socket.Send(data, 0, data.Length, SocketFlags.None);
        }
    }
    
    public void BeginReceive()
    {
        m_socket.BeginReceive(m_recvBuff, 0, m_recvBuff.Length, SocketFlags.None, m_recvCb, this);
    }
    
    private void RecvCallBack(IAsyncResult ar)
    {
        var len = m_socket.EndReceive(ar);
        byte[] msg = new byte[len];
        Array.Copy(m_recvBuff, msg, len);
        var msgStr = System.Text.Encoding.UTF8.GetString(msg);
        m_msgQueue.Enqueue(msgStr);
        for (int i = 0; i < m_recvBuff.Length; ++i)
        {
            m_recvBuff[i] = 0;
        }
    }
    
    public string GetMsgFromQueue()
    {
        if (m_msgQueue.Count > 0)
            return m_msgQueue.Dequeue();
        return null;
    }
    
    public void CloseSocket()
    {
        Debug.Log("close socket");
        try
        {
            m_socket.Shutdown(SocketShutdown.Both);
            m_socket.Close();
        }
        catch (Exception e)
        {
            
        }
        finally
        {
            m_socket = null;
            connected = false;
        }
    }

}

