/* Copyright (c) 2010 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
using System;

namespace Lidgren.Network
{
	// publicly visible subset of NetMessageType

	/// <summary>
	/// How the library deals with dropped and delayed messages
	/// </summary>
	public enum NetDeliveryMethod : byte
	{
		Unknown = 0,
		Unreliable = 2,
		UnreliableSequenced = 3,
		ReliableUnordered = 35,
		ReliableSequenced = 36,
		ReliableOrdered = 68,
	}

	internal enum NetMessageLibraryType : byte
	{
		Error = 0,
		KeepAlive = 1, // used for piggybacking acks
		Ping = 2, // used for RTT calculation
		Pong = 3, // used for RTT calculation
		Connect = 4,
		ConnectResponse = 5,
		ConnectionEstablished = 6,
		Acknowledge = 7,
		Disconnect = 8,
		Discovery = 9,
		DiscoveryResponse = 10,
		NatIntroduction = 11,
	}

	internal enum NetMessageType : byte
	{
		Error = 0,

		Library = 1, // NetMessageLibraryType byte follows

		UserUnreliable = 2,

		// 3 to 34 = UserSequenced 0 to 31
		UserSequenced = 3,
		UserSequenced1 = 4,
		UserSequenced2 = 5,
		UserSequenced3 = 6,
		UserSequenced4 = 7,
		UserSequenced5 = 8,
		UserSequenced6 = 9,
		UserSequenced7 = 10,
		UserSequenced8 = 11,
		UserSequenced9 = 12,
		UserSequenced10 = 13,
		UserSequenced11 = 14,
		UserSequenced12 = 15,
		UserSequenced13 = 16,
		UserSequenced14 = 17,
		UserSequenced15 = 18,
		UserSequenced16 = 19,
		UserSequenced17 = 20,
		UserSequenced18 = 21,
		UserSequenced19 = 22,
		UserSequenced20 = 23,
		UserSequenced21 = 24,
		UserSequenced22 = 25,
		UserSequenced23 = 26,
		UserSequenced24 = 27,
		UserSequenced25 = 28,
		UserSequenced26 = 29,
		UserSequenced27 = 30,
		UserSequenced28 = 31,
		UserSequenced29 = 32,
		UserSequenced30 = 33,
		UserSequenced31 = 34,

		UserReliableUnordered = 35,

		// 36 to 67 = UserReliableSequenced 0 to 31
		UserReliableSequenced = 36,
		UserReliableSequenced1 = 37,
		UserReliableSequenced2 = 38,
		UserReliableSequenced3 = 39,
		UserReliableSequenced4 = 40,
		UserReliableSequenced5 = 41,
		UserReliableSequenced6 = 42,
		UserReliableSequenced7 = 43,
		UserReliableSequenced8 = 44,
		UserReliableSequenced9 = 45,
		UserReliableSequenced10 = 46,
		UserReliableSequenced11 = 47,
		UserReliableSequenced12 = 48,
		UserReliableSequenced13 = 49,
		UserReliableSequenced14 = 50,
		UserReliableSequenced15 = 51,
		UserReliableSequenced16 = 52,
		UserReliableSequenced17 = 53,
		UserReliableSequenced18 = 54,
		UserReliableSequenced19 = 55,
		UserReliableSequenced20 = 56,
		UserReliableSequenced21 = 57,
		UserReliableSequenced22 = 58,
		UserReliableSequenced23 = 59,
		UserReliableSequenced24 = 60,
		UserReliableSequenced25 = 61,
		UserReliableSequenced26 = 62,
		UserReliableSequenced27 = 63,
		UserReliableSequenced28 = 64,
		UserReliableSequenced29 = 65,
		UserReliableSequenced30 = 66,
		UserReliableSequenced31 = 67,

		// 68 to 99 = UserReliableOrdered 0 to 31
		UserReliableOrdered = 68,
		UserReliableOrdered1 = 69,
		UserReliableOrdered2 = 70,
		UserReliableOrdered3 = 71,
		UserReliableOrdered4 = 72,
		UserReliableOrdered5 = 73,
		UserReliableOrdered6 = 74,
		UserReliableOrdered7 = 75,
		UserReliableOrdered8 = 76,
		UserReliableOrdered9 = 77,
		UserReliableOrdered10 = 78,
		UserReliableOrdered11 = 79,
		UserReliableOrdered12 = 80,
		UserReliableOrdered13 = 81,
		UserReliableOrdered14 = 82,
		UserReliableOrdered15 = 83,
		UserReliableOrdered16 = 84,
		UserReliableOrdered17 = 85,
		UserReliableOrdered18 = 86,
		UserReliableOrdered19 = 87,
		UserReliableOrdered20 = 88,
		UserReliableOrdered21 = 89,
		UserReliableOrdered22 = 90,
		UserReliableOrdered23 = 91,
		UserReliableOrdered24 = 92,
		UserReliableOrdered25 = 93,
		UserReliableOrdered26 = 94,
		UserReliableOrdered27 = 95,
		UserReliableOrdered28 = 96,
		UserReliableOrdered29 = 97,
		UserReliableOrdered30 = 98,
		UserReliableOrdered31 = 99,
	}
}