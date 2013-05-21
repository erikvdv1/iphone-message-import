#region License
// <copyright file="MessageGroup.cs" company="Infiks">
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Infiks.IPhone
{
    /// <summary>
    /// Represents a group of messages from the same sender/receiver.
    /// </summary>
    public class MessageGroup : IEnumerable<Message>
    {
        /// <summary>
        /// The country code of this group.
        /// </summary>
        public string CountryCode { get { return _countryCode == "" ? _countryCode = GetCountryCode() : _countryCode; } }

        /// <summary>
        /// The number of the sender/receiver.
        /// </summary>
        public string Address { get { return _address == "" ? _address = GetAddress() : _address; } }

        /// <summary>
        /// The number of incoming messages in this group.
        /// </summary>
        public int IncomingCount { get { return (from m in _messages where m.Type == MessageType.Incoming select m).Count(); } }

        /// <summary>
        /// The number of outgoing messages in this group.
        /// </summary>
        public int OutgoingCount { get { return (from m in _messages where m.Type == MessageType.Outgoing select m).Count(); } }

        /// <summary>
        /// Offset in seconds between message that are considered to be in the same group (0 = unlimited).
        /// </summary>
        private const int Offset = 0;

        /// <summary>
        /// A private variable that holds the messages of this group.
        /// </summary>
        private readonly List<Message> _messages = new List<Message>();

        /// <summary>
        /// A private variable that holds the country code of this group.
        /// </summary>
        private string _countryCode = "";

        /// <summary>
        /// A private variable that holds the number of the sender/receiver.
        /// </summary>
        private string _address = "";

        /// <summary>
        /// Gets the country code of the first message.
        /// </summary>
        /// <returns>The country code.</returns>
        private string GetCountryCode()
        {
            Message message = this.FirstOrDefault();
            return message != null ? message.CountryCode : "";
        }

        /// <summary>
        /// Gets the address of the first message.
        /// </summary>
        /// <returns>The address.</returns>
        private string GetAddress()
        {
            Message message = this.FirstOrDefault();
            return message != null ? message.Address : "";
        }

        /// <summary>
        /// Adds a message to this group.
        /// </summary>
        /// <param name="message">The message to add.</param>
        public void Add(Message message)
        {
            _messages.Add(message);
        }

        /// <summary>
        /// Divides a sorted list of messages into groups.
        /// </summary>
        /// <param name="messages">The messages.</param>
        /// <returns>The resulting groups.</returns>
        public static IEnumerable<MessageGroup> CreateGroupsFromMessages(IEnumerable<Message> messages)
        {
            // Create new list ans sort on ascending date.
            var messageList = new List<Message>(messages);
            messageList.Sort();

            var groups = new List<MessageGroup>();

            while (messageList.FirstOrDefault() != null)
            {
                // Create the group
                var group = new MessageGroup();
                groups.Add(group);

                // Add the first (and maybe the only) message to the group
                var mainMessage = messageList.First();
                group.Add(mainMessage);
                messageList.Remove(mainMessage);

                // Keep date for tracking message in the same conversation
                var date = mainMessage.Date;

                // Loop through other messages
                var i = 0;
                var message = messageList.ElementAtOrDefault(i);

                // Check if there are more messages within the date offset
                while (message != null && (message.Date <= date + Offset || Offset == 0))
                {
                    if (message.Address == mainMessage.Address)
                    {
                        // Message does belong to the group
                        group.Add(message);
                        messageList.Remove(message);
                        date = message.Date;
                        message = messageList.ElementAtOrDefault(i);
                    }
                    else
                    {
                        // Message does not belong to the group
                        message = messageList.ElementAtOrDefault(++i);
                    }
                }
            }
            return groups;
        }

        /// <summary>
        /// Returns an enumerator that iterates through the messages.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the messages.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<Message> GetEnumerator()
        {
            return _messages.GetEnumerator();
        }

        /// <summary>
        /// Determines if two groups of messages are equal.
        /// </summary>
        /// <param name="obj">The other group.</param>
        /// <returns>True if and only if the groups contain the same messages.</returns>
        public override bool Equals(object obj)
        {
            var other = obj as MessageGroup;
            return other != null && this.SequenceEqual(other);
        }

        /// <summary>
        /// Return the hash code of this message group.
        /// </summary>
        /// <returns>The hash code.</returns>
        public override int GetHashCode()
        {
            return _messages.GetHashCode();
        }
    }
}
