# IoT-Audio-Visualization-Center

## Change Log

- Changed the output to a more realistic spectrum analyzer. The ideal curve would be 10Hz for the first bar, the center 1.5KHz and the last bar 20KHz. (TODO requires to rewrite all the FFT functions, and the resulting animation would look less spectacular. https://youtu.be/4Otqdwql63c?t=73, https://github.com/mborgerding/kissfft)
- Removed a lot options from the device dialog
- Using the broadcast IP address will send UDP multicast packets to all devices in the network
- If an instance is already running, starting the app will bring it to the foreground instead of starting a new instance
- Support for NET 4.8
- Visual Studio 2022 is supported
- Removed the old nuget dependencies and replaced them with the built-in nuget tool
- Added the original libraries from https://www.un4seen.com/ for x86 and x64
- Changed the old Bass API to ManagedBase and ManagedBase.WasApi

## Readme

<a href="https://github.com/NimmLor/IoT-Audio-Visualization-Center/graphs/contributors" alt="Contributors"><img src="https://img.shields.io/github/contributors/NimmLor/IoT-Audio-Visualization-Center" /></a> <a href="https://github.com/NimmLor/IoT-Audio-Visualization-Center/releases" alt="Downloads"><img src="https://img.shields.io/github/downloads/NimmLor/IoT-Audio-Visualization-Center/total"/></a><a href="https://github.com/NimmLor/IoT-Audio-Visualization-Center/commits/master" alt="Downloads">
<img src="https://img.shields.io/github/commits-since/NimmLor/IoT-Audio-Visualization-Center/latest?include_prereleases" /></a>

A tool to sync my esp8266-fastled-iot-webserver with windows music.

Original github page: https://github.com/NimmLor/esp8266-fastled-iot-webserver


### Work in progress...

and still very experimental.



### Docs and Instructions still work in progress...

![](screenshot_alpha.jpg?raw=true)
