# TFTtag-Ili934x-for-OpenEpaperLink

Uses an ILI9341 240x320 TFT display + a raspberry pi as an OpenEpaperLink Tag using net9

Connections Display To Raspberry Pi:

| Display     | Raspberry Pi     | Pin    |
|-------------|------------------|--------|
| SDO (MISO)  |  MISO / GPIO9    | Pin 21 |
| LED         |  3.3V            |        |
| SCK         |  SCLK            | Pin 23 |
| SDI (MOSI)  |  MOSI / GPIO 10  | Pin 19 |
| D/C         |  GPIO 24         | Pin 18 | 
| RESET       |  GPIO 25         | Pin 22 |
| CS          |  GPIO 8          | Pin 24 |
| GND         |  GND             |        |
| VCC         |  3.3V            |        |

Based on https://github.com/nlimper/TFTtag-for-OpenEpaperLink

Also see https://github.com/OpenEPaperLink/OpenEPaperLink

![TFTtag-Ili934x-for-OpenEpaperLink](https://i.imgur.com/DkUYh0O.jpeg)

## installation

Copy all the published application files and subdirectories to ~/TFTtag-Ili934x-for-OpenEpaperLink

Make application runnable using :
```
sudo chmod +x ~/TFTtag-Ili934x-for-OpenEpaperLink/TFTtag-Ili934x-for-OpenEpaperLink
```

Install the application as a service (Adjust TFTtag-Ili934x-for-OpenEpaperLink.service if user or directory is different) :
```
sudo systemctl stop TFTtag-Ili934x-for-OpenEpaperLink

sudo cp ~/TFTtag-Ili934x-for-OpenEpaperLink/TFTtag-Ili934x-for-OpenEpaperLink.service /etc/systemd/system/TFTtag-Ili934x-for-OpenEpaperLink.service

sudo systemctl daemon-reload

sudo systemctl enable TFTtag-Ili934x-for-OpenEpaperLink

sudo systemctl start TFTtag-Ili934x-for-OpenEpaperLink
```

Check if service is running ok :
```
sudo systemctl status TFTtag-Ili934x-for-OpenEpaperLink

or

sudo journalctl -u TFTtag-Ili934x-for-OpenEpaperLink 
```



## Visual Studio Publish Action

When using 'Publish' -> Visual Studio automatically synchronises all files to ~/TFTtag-Ili934x-for-OpenEpaperLink using WinSCP.

Adjust paths, ip address, user and password in .csproj file as required :
```
<Target Name="PiCopy" AfterTargets="Publish">
  <Exec Command="&quot;C:\Program Files (x86)\WinSCP\WinSCP.com&quot; /command &quot;open sftp://pi:raspberry@192.168.2.60/&quot; &quot;synchronize remote C:\dotnet\projects\TFTtag-Ili934x-for-OpenEpaperLink\TFTtag-Ili934x-for-OpenEpaperLink\bin\Release\net9.0\publish /home/pi/TFTtag-Ili934x-for-OpenEpaperLink/&quot; &quot;exit&quot;" />
</Target>
```
Alternatively, you could also copy all the files using pscp, that comes with putty:
```
Target Name="PiCopy" AfterTargets="Publish">
   <Exec Command="pscp -r -pw raspberry C:\dotnet\projects\TFTtag-Ili934x-for-OpenEpaperLink\TFTtag-Ili934x-for-OpenEpaperLink\bin\Release\net9.0\publish\ pi@192.168.2.60:/home/pi/TFTtag-Ili934x-for-OpenEpaperLink/" />
</Target>
```
First stop the application, before updating the files :
```
sudo systemctl stop TFTtag-Ili934x-for-OpenEpaperLink
```
