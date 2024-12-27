using System.Device.Gpio;
using System.Device.Spi;
using System.Drawing;
using System.Net.Sockets;
using System.Net;
using System.Net.NetworkInformation;
using Iot.Device.Graphics.SkiaSharpAdapter;
using Iot.Device.Ili934x;
using System.Runtime.InteropServices;

	
namespace TFTtag_Ili934x_for_OpenEpaperLink
{
     internal class Program
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct EspBlockRequest
        {
            public byte Checksum;
            public ulong Ver;
            public byte BlockId;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Src;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct EspXferComplete
        {
            public byte Checksum;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Src;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct EspSetChannelPower
        {
            public byte Checksum;
            public byte Channel;
            public byte Power;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BlockData
        {
            public ushort Size;
            public ushort Checksum;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] // Size is dynamic, handle accordingly
            public byte[] Data; // This will need to be managed separately
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct AvailDataReq
        {
            public byte Checksum;
            public byte LastPacketLQI;
            public sbyte LastPacketRSSI;
            public sbyte Temperature;
            public ushort BatteryMv;
            public byte HwType;
            public byte WakeupReason;
            public byte Capabilities;
            public ushort TagSoftwareVersion;
            public byte CurrentChannel;
            public byte CustomMode;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Reserved;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct EspAvailDataReq
        {
            public byte Checksum;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Src;

            public AvailDataReq Adr;
        }

        private const int EPD_LUT_DEFAULT = 0;
        private const int EPD_LUT_NO_REPEATS = 1;
        private const int EPD_LUT_FAST_NO_REDS = 2;
        private const int EPD_LUT_FAST = 3;
        private const int EPD_LUT_OTA = 0x10;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct AvailDataInfo
        {
            public byte Checksum;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] DataVer; // MD5 of potential traffic

            public uint DataSize;
            public byte DataType; // allows for 16 different datatypes

            public byte
                DataTypeArgument; // extra specification or instruction for the tag (LUT to be used for drawing image)

            public ushort NextCheckIn; // when should the tag check-in again? Measured in minutes
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct PendingData
        {
            public AvailDataInfo AvailDataInfo;
            public ushort AttemptsLeft;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] TargetMac;
        }

        //private const int BLOCK_DATA_SIZE = 4096;
        //private const int BLOCK_XFER_BUFFER_SIZE = BLOCK_DATA_SIZE + Marshal.SizeOf(typeof(BlockData));

        private const byte PKT_AVAIL_DATA_REQ = 0xE5;
        private const byte PKT_AVAIL_DATA_INFO = 0xE6;
        private const byte PKT_XFER_COMPLETE = 0xEA;
        private const byte PKT_XFER_TIMEOUT = 0xED;
        private const byte PKT_CANCEL_XFER = 0xEC;
        private const byte PKT_APLIST_REQ = 0x80;
        private const byte PKT_APLIST_REPLY = 0x81;
        private const byte PKT_TAGINFO = 0x82;


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct APlist
        {
            public uint Src;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string Alias;

            public byte ChannelId;
            public byte TagCount;
            public ushort Version;
        }

        private const int SYNC_NOSYNC = 0;
        private const int SYNC_USERCFG = 1;
        private const int SYNC_TAGSTATUS = 2;
        private const int SYNC_DELETE = 3;
        private const int SYNC_VERSION = 0xAA00;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TagInfo
        {
            public ushort structVersion;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] mac;

            public byte syncMode;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string alias;

            public uint lastseen;
            public uint nextupdate;
            public bool pending;
            public uint expectedNextCheckin;
            public byte hwType;
            public byte wakeupReason;
            public byte capabilities;
            public ushort pendingIdle;
            public byte contentMode;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct TagSettings
        {
            public byte settingsVer;
            public byte enableFastBoot;
            public byte enableRFWake;
            public byte enableTagRoaming;
            public byte enableScanForAPAfterTimeout;
            public byte enableLowBatSymbol;
            public byte enableNoRFSymbol;
            public byte fastBootCapabilities;
            public byte customMode;
            public ushort batLowVoltage;
            public ushort minimumCheckInTime;
            public byte fixedChannel;
        }

        private const int DATATYPE_IMG_RAW_1BPP = 0x20;
        private const int DATATYPE_IMG_RAW_2BPP = 0x21;
        private const int DATATYPE_IMG_RAW_3BPP = 0x22;
        private const int DATATYPE_IMG_RAW_4BPP = 0x23;

        private static Thread? _receiveThread;

        private static byte[]? _localMacAddress;
        private static IPAddress? _localIp;

        private static SpiDevice? _displaySpi;
        private static Ili9341? _ili9341;
        private static GpioController? _gpio;

        private const string Udpip = "239.10.0.1";
        private const int Udpport = 16033;

        private const int Width = 320;
        private const int Height = 240;


        private static byte[] StructureToByteArray(object obj)
        {
            var len = Marshal.SizeOf(obj);

            var arr = new byte[len];

            var ptr = Marshal.AllocHGlobal(len);

            Marshal.StructureToPtr(obj, ptr, true);

            Marshal.Copy(ptr, arr, 0, len);

            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        private static void ByteArrayToPendingData(byte[] bytearray, ref PendingData pendingData)
        {
            var len = Marshal.SizeOf(pendingData);

            var i = Marshal.AllocHGlobal(len);

            Marshal.Copy(bytearray, 1, i, len);

            pendingData = Marshal.PtrToStructure<PendingData>(i);

            Marshal.FreeHGlobal(i);
        }

        private static void NetProcessDataReq(byte wakeupReason)
        {
            if (_localMacAddress == null)
            {
                return;
            }

            var eadr = new EspAvailDataReq
            {
                Src = new byte[8]
            };
            Array.Copy(_localMacAddress, eadr.Src, 6);

            eadr.Adr.LastPacketRSSI = -100; // WiFi.RSSI();
            eadr.Adr.CurrentChannel = 1; // WiFi.channel();
            eadr.Adr.HwType = 0xE5;
            eadr.Adr.WakeupReason = wakeupReason;
            eadr.Adr.Capabilities = 0;
            eadr.Adr.TagSoftwareVersion = 0;
            eadr.Adr.CustomMode = 0;

            var eadrData = StructureToByteArray(eadr);

            var eadrLen = Marshal.SizeOf<EspAvailDataReq>();

            var buffer = new byte[eadrLen + 1];
            Array.Copy(eadrData, 0, buffer, 1, eadrLen);

            buffer[0] = PKT_AVAIL_DATA_INFO;

            //Console.WriteLine($"Data {buffer.Length} {Convert.ToHexString(buffer)}");

            using var udpClient = new UdpClient(AddressFamily.InterNetwork);

            var address = IPAddress.Parse(Udpip);
            var ipEndPoint = new IPEndPoint(address, Udpport);
            udpClient.JoinMulticastGroup(address);

            udpClient.Send(buffer, buffer.Length, ipEndPoint);
            udpClient.Close();
        }

        private static void NetProcessXferComplete(byte[] targetMac)
        {
            var xfc = new EspXferComplete
            {
                Src = new byte[8]
            };
            Array.Copy(targetMac, xfc.Src, 8);

            var xfcData = StructureToByteArray(xfc);

            var xfcLen = Marshal.SizeOf<EspXferComplete>();

            var buffer = new byte[xfcLen + 1];
            Array.Copy(xfcData, 0, buffer, 1, xfcLen);

            buffer[0] = PKT_XFER_COMPLETE;

            //Console.WriteLine($"Data {buffer.Length} {Convert.ToHexString(buffer)}");

            using var udpClient = new UdpClient(AddressFamily.InterNetwork);

            var address = IPAddress.Parse(Udpip);
            var ipEndPoint = new IPEndPoint(address, Udpport);
            udpClient.JoinMulticastGroup(address);

            udpClient.Send(buffer, buffer.Length, ipEndPoint);
            udpClient.Close();
        }

        private static void Receive()
        {
            if (_localIp == null || _localMacAddress == null || _ili9341 == null)
            {
                return;
            }

            using var httpClient = new HttpClient();

            using var udpClient = new UdpClient();

            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, Udpport));

            udpClient.JoinMulticastGroup(IPAddress.Parse(Udpip), _localIp);
            udpClient.MulticastLoopback = true;

            while (true)
            {
                var ipEndPoint = new IPEndPoint(IPAddress.Any, 0);
                var buffer = udpClient.Receive(ref ipEndPoint);

                if (ipEndPoint.Address.ToString() != _localIp.ToString())
                {
                    //Console.WriteLine($"Received Data {buffer.Length} {Convert.ToHexString(buffer)} {ipEndPoint.Address.ToString()}");

                    switch (buffer[0])
                    {
                        case PKT_AVAIL_DATA_REQ:

                            //Console.WriteLine($"PKT_AVAIL_DATA_REQ");

                            var pending = new PendingData();
                            ByteArrayToPendingData(buffer, ref pending);

                            var macWithZeros = new byte[8];
                            Array.Copy(_localMacAddress, 0, macWithZeros, 0, 6);

                            //Console.WriteLine($"Data {Convert.ToHexString(pending.TargetMac)} {Convert.ToHexString(macAddress8)} {buffer.Length} {Convert.ToHexString(buffer)}");

                            if (pending.TargetMac.SequenceEqual(macWithZeros))
                            {
                                Console.WriteLine($"data type {pending.AvailDataInfo.DataType}");

                                switch (pending.AvailDataInfo.DataType)
                                {
                                    case DATATYPE_IMG_RAW_1BPP:
                                    case DATATYPE_IMG_RAW_2BPP:
                                    case DATATYPE_IMG_RAW_3BPP:
                                    case DATATYPE_IMG_RAW_4BPP:

                                        var imageUrl =
                                            $"http://{ipEndPoint.Address}/getdata?mac={Convert.ToHexString(pending.TargetMac.Reverse().ToArray())}&md5={Convert.ToHexString(pending.AvailDataInfo.DataVer.Reverse().ToArray())}";

                                        using (var response = httpClient.GetAsync(imageUrl).Result)
                                        {
                                            if (response.StatusCode == HttpStatusCode.OK)
                                            {
                                                var fileContents = response.Content.ReadAsByteArrayAsync().Result;

                                                if (fileContents.Length == Height * Width * 2)
                                                {
                                                    //Console.WriteLine($"Data {imageUrl} {response.StatusCode}");

                                                    var rotatedArray = new byte[fileContents.Length];

                                                    /*
                                                    // rotate +90 degrees
                                                    for (var y = 0; y < Height; y++)
                                                    {
                                                        var destinationX = Height - 1 - y;

                                                        for (var x = 0; x < Width; x++)
                                                        {
                                                            var sourcePosition = (x + y * Width);
                                                            var destinationY = x;
                                                            var destinationPosition = (destinationX + destinationY * Height);
                                                            rotatedArray[destinationPosition*2] = fileContents[sourcePosition*2];
                                                            rotatedArray[destinationPosition * 2 +1] = fileContents[sourcePosition * 2 +1];
                                                        }
                                                    }*/

                                                    // rotate -90 degrees
                                                    for (var y = 0; y < Height; y++)
                                                    {
                                                        var destinationX = y;

                                                        for (var x = 0; x < Width; x++)
                                                        {
                                                            var sourcePosition = (x + y * Width);
                                                            var destinationY = Width - 1 - x;
                                                            var destinationPosition = (destinationX + destinationY * Height);
                                                            rotatedArray[destinationPosition * 2] = fileContents[sourcePosition * 2];
                                                            rotatedArray[destinationPosition * 2 + 1] = fileContents[sourcePosition * 2 + 1];
                                                        }
                                                    }

                                                    _ili9341.SendBitmapPixelData(rotatedArray, new Rectangle(0, 0, Height, Width));
                                                }
                                            }
                                        }

                                        Console.WriteLine($"Data {imageUrl}");

                                        NetProcessXferComplete(pending.TargetMac);

                                        break;
                                }

                            }

                            break;
                    }
                }

            }
        }

        static void Main( /*string[] args*/)
        {
            int pinDC = 24;
            int pinReset = 25;
            int pinLed = 18;

            SkiaSharpAdapter.Register();

            _displaySpi = SpiDevice.Create(new SpiConnectionSettings(0, 0)
                { ClockFrequency = Ili9341.DefaultSpiClockFrequency, Mode = Ili9341.DefaultSpiMode });

            if (_displaySpi == null)
            {
                Console.WriteLine("SPI device not created");
                return;
            }

            _gpio = new GpioController();
            if (_gpio == null)
            {
                Console.WriteLine("GPIO controller not created");
                return;
            }

            _ili9341 = new Ili9341(_displaySpi, pinDC, pinReset, backlightPin: pinLed, gpioController: _gpio);
            if (_ili9341 == null)
            {
                Console.WriteLine("ILI9341 not created");
                return;
            }

            _ili9341.TurnBacklightOn();

            var computerProperties = IPGlobalProperties.GetIPGlobalProperties();
            var nics = NetworkInterface.GetAllNetworkInterfaces();
            Console.WriteLine($"{computerProperties.HostName}");
            if (nics == null || nics.Length < 1)
            {
                Console.WriteLine("No network interfaces found.");
                return;
            }

            Console.WriteLine($"Number of interfaces {nics.Length}");

            foreach (var adapter in nics)
            {
                if (adapter.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (var ip in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            Console.WriteLine(adapter.Name);
                            Console.WriteLine($"{adapter.NetworkInterfaceType}");

                            _localIp = ip.Address;

                            var properties = adapter.GetIPProperties(); //  .GetIPInterfaceProperties();
                            var physicalAddress = adapter.GetPhysicalAddress();
                            _localMacAddress = physicalAddress.GetAddressBytes();

                            Console.WriteLine(Convert.ToHexString(_localMacAddress));
                            Console.WriteLine(_localIp.ToString());
                        }
                    }
                }
            }

            if (_localIp == null || _localMacAddress == null)
            {
                return;
            }

            _receiveThread = new Thread(Receive);
            _receiveThread.Start();

            NetProcessDataReq(0xFC);

            while (true)
            {
                NetProcessDataReq(0);
                Thread.Sleep(60000);
            }


        }


    }
}
