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
