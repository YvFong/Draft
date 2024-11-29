using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System;

public class TestPanel : MonoBehaviour
{
    private const string IP = "172.18.34.155";
    private const int PORT = 50000;

    public InputField unameInput;
    public InputField msgInput;
    public Button loginBtn;
    public Button sendBtn;
    public Text stateTxt;
    public Text connectBtnText;
    public Text chatMsgTxt;
    public InputField receiver;
    private ClientSocket clientSocket = new ClientSocket();
    JSONObject jsonObj = new JSONObject();

    void Start()
    {
        chatMsgTxt.text = "";

        loginBtn.onClick.AddListener(() =>
        {
            if (clientSocket.connected)
            {
                clientSocket.CloseSocket();
                stateTxt.text = "已断开";
                connectBtnText.text = "连接";
                unameInput.enabled = true;
            }
            else
            {
                clientSocket.Connect(IP, PORT);
                stateTxt.text = clientSocket.connected ? "已连接" : "未连接";
                connectBtnText.text = clientSocket.connected ? "断开" : "连接";
                if (clientSocket.connected)
                {
                    unameInput.enabled = false;
                }
                Send("login");
            }
        });

        sendBtn.onClick.AddListener(() =>
        {
            if (receiver.text == "")
            {
                Send("chat", msgInput.text);
            }
            else
            {
                Send("personalchat", msgInput.text, receiver.text);
            }
            msgInput.text = "";
        });
    }

    private void Update()
    {
        if (clientSocket.connected)
        {
            clientSocket.BeginReceive();
        }
        var msg = clientSocket.GetMsgFromQueue();
        if (!string.IsNullOrEmpty(msg))
        {
            chatMsgTxt.SetAllDirty();
            chatMsgTxt.text += msg + "\n";
            Debug.Log("RecvCallBack: " + msg);
        }
    }

    private void Send(string protocol, string msg = "", string receiver = "")
    {
        jsonObj["protocol"] = protocol;
        jsonObj["uname"] = unameInput.text;
        jsonObj["msg"] = msg;
        if (receiver != "")
        {
            jsonObj["receiver"] = receiver;
        }
        string jsonStr = JSONConvert.SerializeObject(jsonObj);
        clientSocket.SendData(jsonStr);
    }

    private void OnApplicationQuit()
    {
        if (clientSocket.connected)
        {
            clientSocket.CloseSocket();
        }
    }
}
