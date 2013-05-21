#region License
// <copyright file="Message.cs" company="Infiks">
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
using System.Linq;

namespace Infiks.IPhone
{
    /// <summary>
    /// This class represents a SMS message.
    /// </summary>
    public class Message : IComparable<Message>
    {
        /// <summary>
        /// The phone number of the sender or receiver.
        /// </summary>
        public string Address { get; private set; }

        /// <summary>
        /// The timestamp of the message.
        /// </summary>
        public int Date { get; private set; }

        /// <summary>
        /// The content of the message.
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// The type, incoming or outgoing, of the message.
        /// </summary>
        public MessageType Type { get; private set; }

        /// <summary>
        /// The country code of the sender/receiver number.
        /// </summary>
        public string CountryCode
        {
            get { return GetCountryCode(Address); }
        }

        /// <summary>
        /// Creates a single Message with the specified values.
        /// </summary>
        /// <param name="address">The phone number</param>
        /// <param name="date">The date in unix timestamp format</param>
        /// <param name="text">The contents</param>
        /// <param name="type">The message type</param>
        public Message(string address, int date, string text, MessageType type)
        {
            Address = address;
            Date = date;
            Text = text;
            Type = type;
        }

        /// <summary>
        /// Creates a Message from a data row
        /// </summary>
        /// <param name="row">The data row. It must have the following four columns: Address, Date, Text, Type</param>
        public Message(DataRow row)
        {
            Address = row["Address"] as string;
            Date = Int32.Parse(row["Date"] as string);
            Text = row["Text"] as string;
            Type = ConvertType(row["Type"] as string);
        }

        /// <summary>
        /// Converts the data rows to a list of Messages
        /// </summary>
        /// <param name="rows">The data rows</param>
        /// <returns>The list of Messages.</returns>
        public static List<Message> FromDataTable(DataTable table)
        {
            return table.Rows.Cast<DataRow>().Select(row => new Message(row)).ToList();
        }

        /// <summary>
        /// Determines the language from the phone number.
        /// </summary>
        /// <example>
        /// +31612345678 is a Dutch number, the return value is: "nl".
        /// </example>
        /// <param name="address">The phone number</param>
        /// <returns>The corresponding country code</returns>
        private static string GetCountryCode(string address)
        {
            string code;
            switch (address.Substring(0, 3))
            {
                case "+10":
                case "+11":
                case "+12":
                case "+13":
                case "+14":
                case "+15":
                case "+16":
                case "+17":
                case "+18":
                case "+19":
                    code = "us";
                    break;
                case "+31":
                    code = "nl";
                    break;
                case "+32":
                    code = "be";
                    break;
                case "+33":
                    code = "fr";
                    break;
                case "+34":
                    code = "es";
                    break;
                case "+39":
                    code = "it";
                    break;
                case "+44":
                    code = "uk";
                    break;
                case "+49":
                    code = "de";
                    break;
                default:
                    code = "";
                    break;
            }
            return code;
        }

        /// <summary>
        /// Converts a string "s" or "r" to type "Outgoing" or "Incoming" respectively.
        /// </summary>
        /// <param name="str">The string containing "s" or "r".</param>
        /// <returns>The Message type</returns>
        private static MessageType ConvertType(string str)
        {
            MessageType type;
            switch (str)
            {
                case "s":
                    type = MessageType.Outgoing;
                    break;
                case "r":
                    type = MessageType.Incoming;
                    break;
                default:
                    type = MessageType.Unknown;
                    break;
            }
            return type;
        }

        /// <summary>
        /// Compares two messages to each other. The messages are compared on their date values.
        /// </summary>
        /// <param name="other">The other message.</param>
        /// <returns>0 when the two message are sent at the same time</returns>
        public int CompareTo(Message other)
        {
            if (other == null) return 1;
            if (this.Date > other.Date) return 1;
            if (this.Date < other.Date) return  -1;
            return 0;
        }
    }

    /// <summary>
    /// Indicate whether the Message is an incoming or outgoing message.
    /// </summary>
    public enum MessageType
    {
        Unknown  = 0,
        Incoming = 2,
        Outgoing = 3
    }
}
