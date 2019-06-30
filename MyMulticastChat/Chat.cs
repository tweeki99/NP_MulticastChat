using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MyMulticastChat
{
    public class Chat
    {
        MainWindow mainWindow;

        private Guid myId;
        private string myName;

        private List<User> allUsers;

        private bool alive;
        private UdpClient udpClient;
        private IPAddress multicastAddress;
        private IPEndPoint remoteep;

        public Chat(MainWindow mainWindow, string myName)
        {
            this.mainWindow = mainWindow;
            myId = Guid.NewGuid();
            this.myName = myName;
            allUsers = new List<User>();
            alive = false;
            multicastAddress = IPAddress.Parse("239.0.0.0");
            udpClient = new UdpClient();
            udpClient.JoinMulticastGroup(multicastAddress);
            remoteep = new IPEndPoint(multicastAddress, 12345);
        }

        public void SendMessage(string data)
        {
            Byte[] buffer = Encoding.UTF8.GetBytes(data);
            udpClient.Send(buffer, buffer.Length, remoteep);
        }

        public void Listen()
        {
            alive = true;
            udpClient = new UdpClient();
            udpClient.ExclusiveAddressUse = false;
            IPEndPoint localEp = new IPEndPoint(IPAddress.Any, 12345);
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(localEp);
            udpClient.JoinMulticastGroup(multicastAddress);

            try
            {
                while (alive)
                {
                    Byte[] data = udpClient.Receive(ref localEp);
                    DefineAttributeBehavior(Encoding.UTF8.GetString(data));
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        public void DefineAttributeBehavior(string attributeString)
        {
            string[] substrings = attributeString.Split(new char[] { (char)1 }, StringSplitOptions.RemoveEmptyEntries);

            switch (substrings[0])
            {
                case "Message":
                    MessageCome(substrings[1], substrings[2]);
                    break;
                case "Signin":
                    SigninUser(substrings[1]);
                    break;
                case "Signout":
                    SignoutUser(substrings[1]);
                    break;
                case "ListUsers":
                    AllUsgersList(substrings[1]);
                    break;
            }
        }

        public void MessageCome(string userName, string message)
        {
            mainWindow.Dispatcher.Invoke(new ThreadStart(() => mainWindow.allMessageTextBox.Text += DateTime.Now.ToShortTimeString() + ": " + userName + " - " + message + "\r\n"));
        }

        public void AllUsgersList(string jsonAllUser)
        {
            List<User> allUsersList = JsonConvert.DeserializeObject<List<User>>(jsonAllUser);
            if (allUsersList.LastOrDefault().UserId == myId)
            {
                allUsers = allUsersList;
                UpdateOnlineUsersListBox();
            }
        }

        public void SignoutUser(string jsonUser)
        {
            User signoutUser = JsonConvert.DeserializeObject<User>(jsonUser);
            if (myId != signoutUser.UserId)
            {
                for (int i = 0; i < allUsers.Count; i++)
                {
                    if (allUsers[i].UserId == signoutUser.UserId)
                    {
                        allUsers.Remove(allUsers[i]);
                        break;
                    }
                }
                mainWindow.Dispatcher.Invoke(new ThreadStart(() => mainWindow.allMessageTextBox.Text += DateTime.Now.ToShortTimeString() + ": " + signoutUser.UserName + " вышел из чата" + "\r\n"));
                UpdateOnlineUsersListBox();
            }
        }

        public void SigninUser(string jsonUser)
        {
            User signinUser = JsonConvert.DeserializeObject<User>(jsonUser);
            allUsers.Add(signinUser);
            mainWindow.Dispatcher.Invoke(new ThreadStart(() => mainWindow.allMessageTextBox.Text += DateTime.Now.ToShortTimeString() + ": " + signinUser.UserName + " вошел в чат" + "\r\n"));
            UpdateOnlineUsersListBox();
            if (allUsers.FirstOrDefault().UserId == myId && allUsers.LastOrDefault().UserId != myId)
            {
                SendAllUsersToTheConnectedUser();
            }
        }

        public void SendAllUsersToTheConnectedUser()
        {
            string jsonString = JsonConvert.SerializeObject(allUsers);
            SendMessage(FormAttributeString(Attribute.ListUsers, jsonString));
        }

        public void ThisUserConnection()
        {
            string jsonString = JsonConvert.SerializeObject(new User { UserId = myId, UserName = myName });
            SendMessage(FormAttributeString(Attribute.Signin, jsonString));
        }

        public void SendMessageToTheAllUsers(string message)
        {
            SendMessage(FormAttributeString(Attribute.Message, myName, message));
            mainWindow.messageTextBox.Text = string.Empty;
        }

        public string FormAttributeString(Attribute attribute, params string[] messageBlocks)
        {
            string attributeString = attribute.ToString();
            for (int i = 0; i < messageBlocks.Length; i++)
            {
                attributeString += (char)1 + messageBlocks[i];
            }
            return attributeString;
        }

        public void ThisUserDisconnection()
        {
            if (alive)
            {
                alive = false;

                string jsonString = JsonConvert.SerializeObject(new User { UserId = myId, UserName = myName });
                SendMessage(FormAttributeString(Attribute.Signout, jsonString));
                Thread.Sleep(5);
                udpClient.DropMulticastGroup(multicastAddress);

                udpClient.Close();
            }
        }

        public void UpdateOnlineUsersListBox()
        {
            int maxLength = GetMaxLenghtUserName();

            mainWindow.Dispatcher.Invoke(new ThreadStart(() => mainWindow.onlineUsersListBox.Items.Clear()));

            foreach (var user in allUsers)
            {
                mainWindow.Dispatcher.Invoke(new ThreadStart(() => mainWindow.onlineUsersListBox.Items.Add(String.Format("{0,-" + (maxLength + 5) + "}", user.UserName) + user.UserId.ToString())));
            }
        }

        private int GetMaxLenghtUserName()
        {
            int maxLength = 0;
            foreach (var user in allUsers)
            {
                if (user.UserName.Length > maxLength)
                {
                    maxLength = user.UserName.Length;
                }
            }
            return maxLength;
        }
    }
}
