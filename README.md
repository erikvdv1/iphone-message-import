iPhone message import
===============

Imports tab-separated text messages (SMS messages from your previous phone) into the text message database of an iPhone backup.
This backup can then be restored to the iPhone with iTunes.

Requirements
------------
* [iBackupBot for iTunes](http://www.icopybot.com/itunes-backup-manager.htm), for extracting and replacing the SMS database (SQLite) in an iPhone backup.

Building
--------
Clone this repo and reinstall all NuGet packages to download the correct SQLite libraries. Run the following command in the Package Manager Console: `Update-Package -reinstall`

Usage
-----
`iPhoneMessageImport.exe [input file] [database file]`

Input format
------------
The input file is a tab-separated file. Each row corresponds to a text message.

`[timestamp]\t[phone no]\t[send/received]\t[text content]`

Check out this [sample](iPhoneMessageImport/sample/input.txt) for the input format.

FAQ
--------
*Error: Could not copy the file "iPhoneMessageImport\\[x86|x64]\SQLite.Interop.dll" because it was not found.*

Run the following command in the Package Manager Console to place the correct SQLite libraries `Update-Package -reinstall`.
