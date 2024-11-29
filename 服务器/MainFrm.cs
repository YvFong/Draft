using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatDemo
{
    public partial class MainFrm : Form
    {
        List<Socket> ClientProxSocketList = new List<Socket>();
        Dictionary<Socket,string> ClientDic = new Dictionary<Socket,string>();
        JSONObject jsonObj = new JSONObject();
        public MainFrm()
        {
            InitializeComponent();
        }
        private void BtnStart_Click(object sender, EventArgs e)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            const string IP = "172.18.34.155";
            socket.Bind(new IPEndPoint(IPAddress.Parse(IP), 50000));
            socket.Listen(10);
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.AcceptClientConnect), socket);
        }
        public void AcceptClientConnect(object socket)
        {
            var serverSocket = socket as Socket;
            this.AppendTextToTxtLog("服务器端开始接受客户端的链接。");
            while (true)
            {
                var proxSocket = serverSocket.Accept();
                this.AppendTextToTxtLog(string.Format("客户端：{0}链接上了", proxSocket.RemoteEndPoint.ToString()));
                ClientProxSocketList.Add(proxSocket);
                ThreadPool.QueueUserWorkItem(new WaitCallback(ReceiveData), proxSocket);
            }
        }
        public void ReceiveData(object socket)
        {
            var proxSocket = socket as Socket;
            byte[] data = new byte[1024 * 1024];
            while (true)
            {
                int len = 0;
                try
                {
                     len= proxSocket.Receive(data, 0, data.Length, SocketFlags.None);
                }
                catch (Exception ex)
                {
                    AppendTextToTxtLog(string.Format("客户端：{0}非正常退出",
                    proxSocket.RemoteEndPoint.ToString()));
                    ClientProxSocketList.Remove(proxSocket);
                    ClientDic.Remove(proxSocket);
                    StopConnect(proxSocket);
                    return;
                }
                if (len <= 0)
                {
                    AppendTextToTxtLog(string.Format("客户端：{0}正常退出",
                    proxSocket.RemoteEndPoint.ToString()));
                    string name=FindNameBySocket(proxSocket);
                    string msg=name+"已下线";
                    SendMsg(msg);
                    ClientProxSocketList.Remove(proxSocket);
                    ClientDic.Remove(proxSocket);
                    StopConnect(proxSocket);
                    return;
                }
                string str = Encoding.UTF8.GetString(data, 0, len);
                jsonObj = JSONConvert.DeserializeObject(str);
                string uname = (string)jsonObj["uname"];
                string protocol = (string)jsonObj["protocol"];
                if (protocol == "login")
                {
                    ClientDic.Add(proxSocket,uname);
                    AppendTextToTxtLog(string.Format("客户端 {0} 登陆了",
                    uname));
                    SendMsg(uname + "进入了房间");
                }else if (protocol == "chat")
                {
                    string msg = (string)jsonObj["msg"];
                    AppendTextToTxtLog(string.Format("接受到客户端 {0} 的消息是：{1}",
                    uname, msg));
                    SendMsg(uname + ":" + msg);
                }else if (protocol == "personalchat")
                {
                    string msg = (string)jsonObj["msg"];
                    string receiver = (string)jsonObj["receiver"];
                    SendPersonalMsg(msg, receiver, proxSocket);
                }
            }
        }
        private void StopConnect(Socket proxSocket)
        {
            try
            {
                if (proxSocket.Connected)
                {
                    proxSocket.Shutdown(SocketShutdown.Both);
                    proxSocket.Close(100);
                }
            }
            catch (Exception ex)
            {

            }
        }
        public void AppendTextToTxtLog(string txt)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.BeginInvoke(new Action<string>(s =>
                {
                    this.txtLog.Text = string.Format("{0}\r\n{1}", s, txtLog.Text);
                }), txt);
            }
            else
            {
                this.txtLog.Text = string.Format("{0}\r\n{1}", txt, txtLog.Text);//不考虑跨线程
            }

        }
        private void btnSendMsg_Click(object sender, EventArgs e)
        {
            SendMsg("服务端发送消息"+txtMsg.Text);
        }
        private void SendMsg(string msg)
        {
            foreach (var proxSocket in ClientProxSocketList)
            {
                if (proxSocket.Connected)
                {
                    byte[] data = Encoding.UTF8.GetBytes(msg);
                    proxSocket.Send(data, 0, data.Length, SocketFlags.None);
                }
            }
        }
        private void SendPersonalMsg(string msg,string name,Socket sender)
        {
            var proxSocket = FindSocketByName(name);
            if (proxSocket == null)
            {
                msg = "服务器发送消息:对方不在线";
                byte[] data = Encoding.UTF8.GetBytes(msg);
                sender.Send(data, 0, data.Length, SocketFlags.None);
            }
            else
            {
                string senderName = FindNameBySocket(sender);
                string newmsg = string.Format("{0}发送给你消息:{1}",senderName,msg);
                byte[] data = Encoding.UTF8.GetBytes(newmsg);
                proxSocket.Send(data, 0, data.Length, SocketFlags.None);
                string sendermsg=string.Format("发送给{0}消息:{1}",name,msg);
                byte[] data2=Encoding.UTF8.GetBytes(sendermsg);
                sender.Send(data2,0,data2.Length,SocketFlags.None);
            }
        }
        private Socket FindSocketByName(string name)
        {
            foreach (KeyValuePair<Socket,string> kv in ClientDic)
            {
                if (kv.Value == name)
                {
                    return kv.Key;
                }
            }
            return null;
        }
        private string FindNameBySocket(Socket target)
        {
            foreach (KeyValuePair<Socket, string> kv in ClientDic)
            {
                if (kv.Key == target)
                {
                    return kv.Value;
                }
            }
            return null;
        }

    }
}
