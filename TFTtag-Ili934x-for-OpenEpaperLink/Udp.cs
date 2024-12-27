using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TFTtag_Ili934x_for_OpenEpaperLink
{
    public static class Udp
    {
        public static void NetProcessDataReq(byte wakeupReason)
        {
            if (Program.LocalMacAddress == null)
            {
                return;
            }

            var eadr = new CommStructs.EspAvailDataReq
            {
                Src = new byte[8]
            };
            Array.Copy(Program.LocalMacAddress, eadr.Src, 6);

            eadr.Adr.LastPacketRSSI = -100; // WiFi.RSSI();
            eadr.Adr.CurrentChannel = 1; // WiFi.channel();
            eadr.Adr.HwType = 0xE5;
            eadr.Adr.WakeupReason = wakeupReason;
            eadr.Adr.Capabilities = 0;
            eadr.Adr.TagSoftwareVersion = 0;
            eadr.Adr.CustomMode = 0;

            var eadrData = CommStructs.StructureToByteArray(eadr);

            var eadrLen = Marshal.SizeOf<CommStructs.EspAvailDataReq>();

            var buffer = new byte[eadrLen + 1];
            Array.Copy(eadrData, 0, buffer, 1, eadrLen);

            buffer[0] = CommStructs.PKT_AVAIL_DATA_INFO;

            //Console.WriteLine($"Data {buffer.Length} {Convert.ToHexString(buffer)}");

            using var udpClient = new UdpClient(AddressFamily.InterNetwork);

            var address = IPAddress.Parse(Program.Udpip);
            var ipEndPoint = new IPEndPoint(address, Program.Udpport);
            udpClient.JoinMulticastGroup(address);

            udpClient.Send(buffer, buffer.Length, ipEndPoint);
            udpClient.Close();
        }

        public static void NetProcessXferComplete(byte[] targetMac)
        {
            var xfc = new CommStructs.EspXferComplete
            {
                Src = new byte[8]
            };
            Array.Copy(targetMac, xfc.Src, 8);

            var xfcData = CommStructs.StructureToByteArray(xfc);

            var xfcLen = Marshal.SizeOf<CommStructs.EspXferComplete>();

            var buffer = new byte[xfcLen + 1];
            Array.Copy(xfcData, 0, buffer, 1, xfcLen);

            buffer[0] = CommStructs.PKT_XFER_COMPLETE;

            //Console.WriteLine($"Data {buffer.Length} {Convert.ToHexString(buffer)}");

            using var udpClient = new UdpClient(AddressFamily.InterNetwork);

            var address = IPAddress.Parse(Program.Udpip);
            var ipEndPoint = new IPEndPoint(address, Program.Udpport);
            udpClient.JoinMulticastGroup(address);

            udpClient.Send(buffer, buffer.Length, ipEndPoint);
            udpClient.Close();
        }

    }
}
