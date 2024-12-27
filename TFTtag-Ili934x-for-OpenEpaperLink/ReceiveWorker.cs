using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace TFTtag_Ili934x_for_OpenEpaperLink
{
    public class ReceiveWorker : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (Program.LocalIp == null || Program.LocalMacAddress == null || Program.Ili9341 == null)
            {
                return;
            }

            using var httpClient = new HttpClient();

            using var udpClient = new UdpClient();

            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, Program.Udpport));

            udpClient.JoinMulticastGroup(IPAddress.Parse(Program.Udpip), Program.LocalIp);
            udpClient.MulticastLoopback = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                var ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var buffer = udpClient.Receive(ref ipEndPoint);

                if (ipEndPoint.Address.ToString() != Program.LocalIp.ToString())
                {
                    //Console.WriteLine($"Received Data {buffer.Length} {Convert.ToHexString(buffer)} {ipEndPoint.Address.ToString()}");

                    switch (buffer[0])
                    {
                        case CommStructs.PKT_AVAIL_DATA_REQ:

                            //Console.WriteLine($"PKT_AVAIL_DATA_REQ");

                            var pending = new CommStructs.PendingData();
                            CommStructs.ByteArrayToPendingData(buffer, ref pending);

                            var macWithZeros = new byte[8];
                            Array.Copy(Program.LocalMacAddress, 0, macWithZeros, 0, 6);

                            //Console.WriteLine($"Data {Convert.ToHexString(pending.TargetMac)} {Convert.ToHexString(macAddress8)} {buffer.Length} {Convert.ToHexString(buffer)}");

                            if (pending.TargetMac.SequenceEqual(macWithZeros))
                            {
                                Console.WriteLine($"data type {pending.AvailDataInfo.DataType}");

                                switch (pending.AvailDataInfo.DataType)
                                {
                                    case CommStructs.DATATYPE_IMG_RAW_1BPP:
                                    case CommStructs.DATATYPE_IMG_RAW_2BPP:
                                    case CommStructs.DATATYPE_IMG_RAW_3BPP:
                                    case CommStructs.DATATYPE_IMG_RAW_4BPP:

                                        var imageUrl =
                                            $"http://{ipEndPoint.Address}/getdata?mac={Convert.ToHexString(pending.TargetMac.Reverse().ToArray())}&md5={Convert.ToHexString(pending.AvailDataInfo.DataVer.Reverse().ToArray())}";

                                        using (var response = await httpClient.GetAsync(imageUrl, stoppingToken))
                                        {
                                            if (response.StatusCode == HttpStatusCode.OK)
                                            {
                                                var fileContents = await response.Content.ReadAsByteArrayAsync(stoppingToken);

                                                if (fileContents.Length == Program.Height * Program.Width * 2)
                                                {
                                                    //Console.WriteLine($"Data {imageUrl} {response.StatusCode}");

                                                    var rotatedArray = new byte[fileContents.Length];

                                                    /*
                                                    // rotate +90 degrees
                                                    for (var y = 0; y < Program.Height; y++)
                                                    {
                                                        var destinationX = Program.Height - 1 - y;

                                                        for (var x = 0; x < Program.Width; x++)
                                                        {
                                                            var sourcePosition = (x + y * Program.Width);
                                                            var destinationY = x;
                                                            var destinationPosition = (destinationX + destinationY * Program.Height);
                                                            rotatedArray[destinationPosition*2] = fileContents[sourcePosition*2];
                                                            rotatedArray[destinationPosition * 2 +1] = fileContents[sourcePosition * 2 +1];
                                                        }
                                                    }*/

                                                    // rotate -90 degrees
                                                    for (var y = 0; y < Program.Height; y++)
                                                    {
                                                        var destinationX = y;

                                                        for (var x = 0; x < Program.Width; x++)
                                                        {
                                                            var sourcePosition = (x + y * Program.Width);
                                                            var destinationY = Program.Width - 1 - x;
                                                            var destinationPosition = (destinationX + destinationY * Program.Height);
                                                            rotatedArray[destinationPosition * 2] = fileContents[sourcePosition * 2];
                                                            rotatedArray[destinationPosition * 2 + 1] = fileContents[sourcePosition * 2 + 1];
                                                        }
                                                    }

                                                    Program.Ili9341.SendBitmapPixelData(rotatedArray, new Rectangle(0, 0, Program.Height, Program.Width));
                                                }
                                            }
                                        }

                                        Console.WriteLine($"Data {imageUrl}");

                                        Udp.NetProcessXferComplete(pending.TargetMac);

                                        break;
                                }

                            }

                            break;
                    }
                }

                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
