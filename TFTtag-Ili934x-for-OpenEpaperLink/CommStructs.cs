using Iot.Device.Ili934x;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Spi;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TFTtag_Ili934x_for_OpenEpaperLink
{
    public static class CommStructs
    {
        public static byte[] StructureToByteArray(object obj)
        {
            var len = Marshal.SizeOf(obj);

            var arr = new byte[len];

            var ptr = Marshal.AllocHGlobal(len);

            Marshal.StructureToPtr(obj, ptr, true);

            Marshal.Copy(ptr, arr, 0, len);

            Marshal.FreeHGlobal(ptr);

            return arr;
        }

        public static void ByteArrayToPendingData(byte[] bytearray, ref CommStructs.PendingData pendingData)
        {
            var len = Marshal.SizeOf(pendingData);

            var i = Marshal.AllocHGlobal(len);

            Marshal.Copy(bytearray, 1, i, len);

            pendingData = Marshal.PtrToStructure<CommStructs.PendingData>(i);

            Marshal.FreeHGlobal(i);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct EspBlockRequest
        {
            public byte Checksum;
            public ulong Ver;
            public byte BlockId;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Src;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct EspXferComplete
        {
            public byte Checksum;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Src;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct EspSetChannelPower
        {
            public byte Checksum;
            public byte Channel;
            public byte Power;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BlockData
        {
            public ushort Size;
            public ushort Checksum;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)] // Size is dynamic, handle accordingly
            public byte[] Data; // This will need to be managed separately
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct AvailDataReq
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
        public struct EspAvailDataReq
        {
            public byte Checksum;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] Src;

            public AvailDataReq Adr;
        }

        public const int EPD_LUT_DEFAULT = 0;
        public const int EPD_LUT_NO_REPEATS = 1;
        public const int EPD_LUT_FAST_NO_REDS = 2;
        public const int EPD_LUT_FAST = 3;
        public const int EPD_LUT_OTA = 0x10;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct AvailDataInfo
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
        public struct PendingData
        {
            public AvailDataInfo AvailDataInfo;
            public ushort AttemptsLeft;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] TargetMac;
        }

        //public const int BLOCK_DATA_SIZE = 4096;
        //public const int BLOCK_XFER_BUFFER_SIZE = BLOCK_DATA_SIZE + Marshal.SizeOf(typeof(BlockData));

        public const byte PKT_AVAIL_DATA_REQ = 0xE5;
        public const byte PKT_AVAIL_DATA_INFO = 0xE6;
        public const byte PKT_XFER_COMPLETE = 0xEA;
        public const byte PKT_XFER_TIMEOUT = 0xED;
        public const byte PKT_CANCEL_XFER = 0xEC;
        public const byte PKT_APLIST_REQ = 0x80;
        public const byte PKT_APLIST_REPLY = 0x81;
        public const byte PKT_TAGINFO = 0x82;


        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct APlist
        {
            public uint Src;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string Alias;

            public byte ChannelId;
            public byte TagCount;
            public ushort Version;
        }

        public const int SYNC_NOSYNC = 0;
        public const int SYNC_USERCFG = 1;
        public const int SYNC_TAGSTATUS = 2;
        public const int SYNC_DELETE = 3;
        public const int SYNC_VERSION = 0xAA00;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TagInfo
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
        public struct TagSettings
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

        public const int DATATYPE_IMG_RAW_1BPP = 0x20;
        public const int DATATYPE_IMG_RAW_2BPP = 0x21;
        public const int DATATYPE_IMG_RAW_3BPP = 0x22;
        public const int DATATYPE_IMG_RAW_4BPP = 0x23;


    }
}
