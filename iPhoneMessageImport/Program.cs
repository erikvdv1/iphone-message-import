#region License
// <copyright file="Program.cs" company="Infiks">
// 
// iPhone Message Import, imports messages into a backup of your iPhone.
// Copyright (c) 2013 Infiks
// 
// This file is part of iPhone Message Import
// 
// iPhone Message Import is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iPhone Message Import is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iPhone Message Import.  If not, see <http://www.gnu.org/licenses/>.
// </copyright>
// <author>Erik van der Veen</author>
// <date>2013-05-21 21:55</date>
#endregion
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace Infiks.IPhone
{
    /// <summary>
    /// The main class.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The default location for the input file.
        /// </summary>
        private const string DefaultInputLocation = "../../sample/input.txt";

        /// <summary>
        /// The default location for the output (database) file.
        /// </summary>
        private const string DefaultDbLocation = "sms.db";

        /// <summary>
        /// The actual input location.
        /// </summary>
        private static string _inputLocation = "";

        /// <summary>
        /// The actual database location.
        /// </summary>
        private static string _dbLocation = "";

        /// <summary>
        /// Application entry point
        /// </summary>
        /// <param name="args">The arguments. The first is the input file path, the second argument is the database path.</param>
        public static void Main(string[] args)
        {
            // Check arguments
            if (args.Length > 0 && (args.Length != 2 || args[0] == "/?"))
            {
                WriteHelp();
                return;
            }

            // Get locations
            _inputLocation = args.Length > 0 ? args[0] : DefaultInputLocation;
            _dbLocation = args.Length > 1 ? args[1] : DefaultDbLocation;

            Import();
            Console.ReadLine();
        }

        /// <summary>
        /// Writes the help text for commandline users.
        /// </summary>
        private static void WriteHelp()
        {
            String assemblyName = System.Reflection.Assembly.GetEntryAssembly().GetName().Name;
            Console.WriteLine("Imports tab-separated text messages into the text message database of an iPhone backup.");
            Console.WriteLine();
            Console.WriteLine(@"{0} [drive:\path\to\input\file drive:\path\to\database\file]", assemblyName);
        }

        /// <summary>
        /// Imports the text message from the input file to the SMS database.
        /// </summary>
        private static void Import()
        {
            // Read input
            Console.WriteLine("Reading input...");
            IEnumerable<Message> messages = Message.FromDataTable(ReadInput(_inputLocation));
            Console.WriteLine("{0} messages", messages.Count());

            // Create groups
            Console.WriteLine("Creating groups...");
            IEnumerable<MessageGroup> groups = MessageGroup.CreateGroupsFromMessages(messages);
            Console.WriteLine("{0} groups", groups.Count());

            // Check if database exists
            if (!File.Exists(_dbLocation))
            {
                Console.WriteLine("Database not found: {0}", _dbLocation);
                return;
            }

            // Insert messages
            int outgoingCount = 0;
            int incomingCount = 0;
            using (var db = new SmsDatabase(_dbLocation))
            {
                // Dropping triggers
                db.DropTriggers();
                Console.WriteLine("Triggers dropped");

                // Add groups to SQLite db
                foreach (var group in groups)
                {
                    db.SaveMessageGroup(group);
                    outgoingCount += group.OutgoingCount;
                    incomingCount += group.IncomingCount;
                }

                // Update counters
                db.UpdateCounters(outgoingCount, incomingCount);
                Console.WriteLine("{0} outgoing messages inserted", outgoingCount);
                Console.WriteLine("{0} incoming messages inserted", incomingCount);

                // Restore triggers
                db.CreateTriggers();
                Console.WriteLine("Triggers restored");
            }
            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        /// <summary>
        /// Reads the messages from the input file. Each row respresents a SMS message.
        /// The input file must have the following format:
        /// {timestamp}\t{phone number}\t{r|s}\t{text}\n
        /// </summary>
        /// <param name="fileName">The input file.</param>
        /// <returns>The data table that correspondents to the input messages.</returns>
        public static DataTable ReadInput(string fileName)
        {
            var dt = new DataTable();
            dt.Columns.Add("Date", typeof(string));
            dt.Columns.Add("Address", typeof(string));
            dt.Columns.Add("Type", typeof(string));
            dt.Columns.Add("Text", typeof(string));

            using (var sr = new StreamReader(fileName, Encoding.Default))
            {
                String line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] fields = line.Split('\t');
                    if (fields.Count() == 4)
                        dt.Rows.Add(fields);
                }
            }
            return dt;
        }
    }
}
