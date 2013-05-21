iPhone message import
===============

Imports tab-separated text messages (SMS messages from your previous phone) into the text message database of an iPhone backup.
This backup can then be restored to the iPhone with iTunes.

Requirements
------------
* [iBackupBot for iTunes](http://www.icopybot.com/itunes-backup-manager.htm), for extracting and replacing the SMS database (SQLite) in an iPhone backup.
* [ADO.NET 2.0 Provider for SQLite](http://sourceforge.net/projects/sqlite-dotnet2/), libraries for communicating with a SQLite database from C#.

Usage
-----
`iPhoneMessageImport.exe [input file] [database file]`

Input format
------------
The input file is a tab-separated file. Each row corresponds to a text message.

`[timestamp]   [phone no]   [send/received]   [text content]`

Check out this [sample](iPhoneMessageImport/sample/input.txt) for the input format.
