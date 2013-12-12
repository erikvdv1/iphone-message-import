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
        /// The timestamp of the message since Unix Epoch (01-01-1970).
        /// </summary>
        public int Timestamp { get; private set; }

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
        public CountryCode CountryCode
        {
            get { return GetCountryCode(Address); }
        }

        /// <summary>
        /// The date of the message.
        /// </summary>
        public DateTime Date
        {
            get
            {
                return new DateTime(1970, 1, 1).AddSeconds(Timestamp);
            }
        }

        /// <summary>
        /// The timestamp of the message since Apple Epoch (01-01-2001).
        /// </summary>
        public int AppleTimestamp
        {
            get
            {
                return Timestamp - 978307200;
            }
        }

        /// <summary>
        /// Creates a single Message with the specified values.
        /// </summary>
        /// <param name="address">The phone number</param>
        /// <param name="timestamp">The date in unix timestamp format</param>
        /// <param name="text">The contents</param>
        /// <param name="type">The message type</param>
        public Message(string address, int timestamp, string text, MessageType type)
        {
            Address = address;
            Timestamp = timestamp;
            Text = text;
            Type = type;
        }

        /// <summary>
        /// Creates a Message from a data row
        /// </summary>
        /// <param name="row">The data row. It must have the following four columns: Address, Timestamp, Text, Type</param>
        public Message(DataRow row)
        {
            Address = row["Address"] as string;
            Timestamp = Int32.Parse(row["Timestamp"] as string);
            Text = row["Text"] as string;
            Type = ConvertType(row["Type"] as string);
        }

        /// <summary>
        /// Converts the data rows to a list of Messages
        /// </summary>
        /// <param name="table">The data table</param>
        /// <returns>The list of Messages.</returns>
        public static List<Message> FromDataTable(DataTable table)
        {
            return table.Rows.Cast<DataRow>().Select(row => new Message(row)).ToList();
        }

        /// <summary>
        /// Determines the language from the phone number.
        /// </summary>
        /// <example>
        /// +31612345678 is a Dutch number, the return value is: "NL".
        /// </example>
        /// <param name="address">The phone number</param>
        /// <returns>The corresponding country code</returns>
        private static CountryCode GetCountryCode(string address)
        {
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
                    return CountryCode.US;
                case "+31":
                    return CountryCode.NL;
                case "+32":
                    return CountryCode.BE;
                case "+33":
                    return CountryCode.FR;
                case "+34":
                    return CountryCode.ES;
                case "+39":
                    return CountryCode.IT;
                case "+44":
                    return CountryCode.UK;
                case "+49":
                    return CountryCode.DE;
                default:
                    return CountryCode.XX;
            }
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
        /// Compares two messages to each other. The messages are compared on their timestamp values.
        /// </summary>
        /// <param name="other">The other message.</param>
        /// <returns>0 when the two message are sent at the same time</returns>
        public int CompareTo(Message other)
        {
            if (other == null) return 1;
            if (Timestamp > other.Timestamp) return 1;
            if (Timestamp < other.Timestamp) return  -1;
            return 0;
        }
    }

    /// <summary>
    /// Indicate whether the Message is an incoming or outgoing message.
    /// </summary>
    public enum MessageType
    {
        Unknown = 0,
        Incoming = 2,
        Outgoing = 3
    }

    /// <summary>
    /// Code for indicating a country.
    /// </summary>
    public enum CountryCode
    {
        XX,
        BE,
        DE,
        ES,
        FR,
        IT,
        NL,
        UK,
        US,
    }
}
