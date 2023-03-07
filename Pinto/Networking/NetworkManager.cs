﻿using PintoNS.Forms.Notification;
using PintoNS.Networking;
using PintoNS;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Security.Cryptography;
using PintoNS.General;

namespace PintoNS.Networking
{
    public class NetworkManager
    {
        private MainForm mainForm;
        public NetworkClient NetClient;
        public NetworkHandler NetHandler;
        public bool IsActive;

        public NetworkManager(MainForm mainForm)
        {
            this.mainForm = mainForm;
            NetClient = new NetworkClient();
            NetHandler = new NetworkHandler(mainForm, NetClient);
            IsActive = true;

            NetClient.ReceivedPacket = delegate (IPacket packet)
            {
                NetClient_ReceivedPacket(packet);
            };

            NetClient.Disconnected = delegate (string reason)
            {
                NetClient_Disconnected(reason);
            };
        }

        public async Task<(bool, Exception)> Connect(string ip, int port)
        {
            (bool, Exception) connectResult = await NetClient.Connect(ip, port);
            return connectResult;
        }

        public void Disconnect(string reason)
        {
            if (NetClient != null && NetClient.IsConnected)
                NetClient.Disconnect(reason);
            NetClient = null;
            NetHandler = null;
            mainForm = null;
            IsActive = false;
        }

        public async Task Login(string username, string password) 
        {
            string passwordHash = BitConverter.ToString(
                new SHA256Managed()
                .ComputeHash
                (Encoding
                .UTF8
                .GetBytes(password)))
                .Replace("-", "")
                .ToLower();
            await NetHandler.SendLoginPacket(11, username, passwordHash);
        }

        public async Task ChangeStatus(UserStatus status) 
        {
            await NetHandler.SendStatusPacket(status);
        }

        private void NetClient_ReceivedPacket(IPacket packet)
        {
            NetHandler.HandlePacket(packet);
        }

        private void NetClient_Disconnected(string reason)
        {
            bool wasActive = IsActive;
            IsActive = false;

            if (!reason.Equals("User requested disconnect")) 
            {
                mainForm.Invoke(new Action(() =>
                {
                    mainForm.Disconnect();
                    if (!NetHandler.LoggedIn && wasActive)
                        NotificationUtil.ShowNotification(mainForm, reason, "Error", NotificationIconType.ERROR);
                }));
            }

            Disconnect(reason);
        }
    }
}