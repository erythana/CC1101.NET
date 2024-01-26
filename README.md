# CC1101 Library for .NET

This is a .NET library for RaspberryPi to communicate with CC1101 Tranceivers.<br />
Please make sure to first <a href="https://www.raspberrypi.com/documentation/computers/configuration.html">enable the
SPI-Interface</a> on your RPi.<br />

## Requirements

* Raspberry PI (or similar) with a Linux Distribution of your choice<br />
* <a href="https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-install-script">.NET 8 Runtime</a><br />
  <i>TODO: Maybe checker whether to add dotnet to path variable is required - probably needed so this program can run
  with dotnet..</i>

## Wiring

Please refer to the <a href="https://www.raspberrypi.com/documentation/computers/raspberry-pi.html">Raspberry Pi
Documentation<a/> for the GPIO Header scheme

The default settings for this library are listed here:

Either use <br />
### BUS 00
| CC1101              | Raspberry Pi |
|---------------------|--------------|
| Vdd                 | 3.3V (01)    |
| SI                  | MOSI (19)    |
| SO                  | MISO (21)    |
| CS                  | SS   (24)    |
| SCLK                | SCK  (23)    |
| GDO2                | GPIO (22)    |
| GDO0                | not used     |
| GND                 | GND (25)     |

OR

### BUS 01
| CC1101              | Raspberry Pi |
|---------------------|--------------|
| Vdd                 | 3.3V (17)    |
| SI                  | MOSI (38)    |
| SO                  | MISO (35)    |
| CS                  | SS   (36)    |
| SCLK                | SCK  (40)    |
| GDO2                | GPIO (22)    |
| GDO0                | not used     |
| GND                 | GND (34)     |

OR

### Custom BUS

If you really need to, you can also supply your own ConnectionConfiguration when instantiating the CC1101 library.<br />
Make sure the configured PIN supports the specific SPI operation.
Read up on Device Trees and the <a href="https://github.com/raspberrypi/documentation/blob/develop/documentation/asciidoc/computers/raspberry-pi/spi-bus-on-raspberry-pi.adoc">Raspberry SPI</a><br />

| CC1101                            |
|-----------------------------------|
| SI                                |
| SO                                |
| CS                                |
| SCLK                              |
| GDO2 (whatever GPIO Pin you like) |

## Usage
Instantiate a new 'CC1101Controller', if you go with the default configuration (Bus 00, see 'Wiring' for more details ) you don't need to specify anything.<br />
If you want to choose a different Bus, you can create supply a 'ConnectionConfiguration' with your settings.<br />
<br />
After creating the controller, you have to call "Init" and initially set a DeviceAddress (whatever you like).<br />
This method returns a CC1101 object which allows you to talk with your CC1101 module.

Example:
```c#
var controller = new CC1101Controller(null);
var cc1101 = controller.Initialize(0x03);
```

## Dependencies

This project uses System.Device.Gpio

## Work in progress
This is still work in progress and pretty much untested.<br />
Also, at this point in time, the exposed interfaces might change...<br />

## Credits

Thanks to SpaceTeddy to share his C++ Project - i ported this library to this .NET library:<br />
https://github.com/SpaceTeddy/CC1101