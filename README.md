# ComfoAir Client
## Description
Reads &amp; analyzes data of Zehnder ComfoAir/ComfoD/WHR ventilation units, using the on-board RS232 port. I'm using this to read the air  temperatures on my ComfoD 450 unit and calculate real life heat exchanger (WTW) efficiences. The RS232 port is located near the power/remote unit connectivity and exposes (in my case) the communication between the ComfoSense control panel and the Zehnder unit.

Should also work for other Zehnder units such as the ComfoD 250/350/450/550 series and new ComfoAir series, the WHR variants. Not tested on Paul units though.

>**Warning:** You can get electrocuted or blow up your ventilation unit by trying to connect to the serial port inside the unit. If you don't know what you are doing, then don't go there -- (*serious face*): only official and real qualified technicians with all the necessary degrees and ... ehrr... all certifications and stuff can do this. Don't come whining I didn't warn you, and warn your next of kin upfront that they are not allowed to sue me for you turning into a half-fried vegetable.

Credits for the description of the Zehnder ComfoAir RS232 protocol go to **See-Solutions**, see their original description [here](http://www.see-solutions.de/sonstiges/Protokollbeschreibung_ComfoAir.pdf).

## Features
Current features:
* Live data processing / raw data dump for serial port connections.
* Export of raw data to files.
* Load previously saved data dumps for new analysis.
* Allows replay to serial port (e.g. to mimic the ComfoSense/CC-Ease control panel).

Todo in a future release, in a near and/or distant future, whenever I feel like it:
* At the moment, about 20% of the commands detected are also analyzed and all parameters outputted to screen.
* Support sending of individual commands to unit

Don't hold your breath though ;).

## Hardware requirements
You need a serial cable to connect the RS232 (female) DSUB-9 connector from the connection board to your computer. If like most people, your computer no longer has a serial port, you can buy a USB-to-RS232 cable. People who know more about that stuff advised me to buy an FTDI based adapter because the drivers (Windows/Linux/...) are basically everywhere and in every OS you would use today and in the near future. Just for reference, I bought [this](https://www.amazon.de/dp/B01E0R8L90/) one on Amazon.

## Downloads

Version | Package
------------ | -------------
v0.2 | Win32 Executable: [ZIP](https://github.com/jacobstim/comfoairclient/files/505209/20161002_ComfoAirClient.v0.2.zip)
v0.1 | Win32 Executable: [ZIP](https://github.com/jacobstim/comfoairclient/files/504331/20161001_ComfoAirClient.v0.1.zip)

## Version History

Version | Date | Description
------------ | ------------- | -------------
0.2 | 02 October 2016 | Saving of captured data, playback to serial device, advanced serial settings
0.1 | 01 October 2016 | First release

## Legal & disclaimer & license

By using this application you agree that you won't blame me for anything that goes wrong, including, but not limited to: exploding harddisks, robot uprisings and zombie apocalypses(TM). Of course, you are solely responsible for whatever you do to your ventilation unit so don't come whining if it explodes in your face. Thank you, come again.

License: you are free to use this application and its source code for personal use only. If you want to use it commercially, then contact me first. I have a particular fondness of beer and whiskey so I am sure we can work something out.
