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
        public static byte[]? LocalMacAddress;
        public static IPAddress? LocalIp;

        public static SpiDevice? DisplaySpi;
        public static Ili9341? Ili9341;
        public static GpioController? Gpio;

        public const string Udpip = "239.10.0.1";
        public const int Udpport = 16033;

        public const int Width = 320;
        public const int Height = 240;


        public static async Task Main(string[] args)
        {
            int pinDC = 24;
            int pinReset = 25;
            int pinLed = 18;

            SkiaSharpAdapter.Register();

            DisplaySpi = SpiDevice.Create(new SpiConnectionSettings(0, 0)
                { ClockFrequency = Ili9341.DefaultSpiClockFrequency, Mode = Ili9341.DefaultSpiMode });

            if (DisplaySpi == null)
            {
                Console.WriteLine("SPI device not created");
                return;
            }

            Gpio = new GpioController();
            if (Gpio == null)
            {
                Console.WriteLine("GPIO controller not created");
                return;
            }

            Ili9341 = new Ili9341(DisplaySpi, pinDC, pinReset, backlightPin: pinLed, gpioController: Gpio);
            if (Ili9341 == null)
            {
                Console.WriteLine("ILI9341 not created");
                return;
            }

            Ili9341.TurnBacklightOn();

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

                            LocalIp = ip.Address;

                            //var properties = adapter.GetIPProperties(); //  .GetIPInterfaceProperties();
                            var physicalAddress = adapter.GetPhysicalAddress();
                            LocalMacAddress = physicalAddress.GetAddressBytes();

                            Console.WriteLine(Convert.ToHexString(LocalMacAddress));
                            Console.WriteLine(LocalIp.ToString());
                        }
                    }
                }
            }

            if (LocalIp == null || LocalMacAddress == null)
            {
                return;
            }

            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddSystemd();

            builder.Services.AddHostedService<ReceiveWorker>();
            builder.Services.AddHostedService<SendWorker>();

            var host = builder.Build();
            await host.RunAsync();

        }


    }
}
