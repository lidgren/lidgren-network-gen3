using System;
using System.Collections.Generic;

using Lidgren.Network;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Lidgren.Network.Xna
{
	public static class XNAExtensions
	{
		/// <summary>
		/// Write a Point
		/// </summary>
		public static void Write(this NetOutgoingMessage message, Point value)
		{
			message.Write(value.X);
			message.Write(value.Y);
		}

		/// <summary>
		/// Read a Point
		/// </summary>
		public static Point ReadPoint(this NetIncomingMessage message)
		{
			return new Point(message.ReadInt32(), message.ReadInt32());
		}

		/// <summary>
		/// Write a Single with half precision (16 bits)
		/// </summary>
		public static void WriteHalfPrecision(this NetOutgoingMessage message, float value)
		{
			message.Write(new HalfSingle(value).PackedValue);
		}

		/// <summary>
		/// Reads a half precision Single written using WriteHalfPrecision(float)
		/// </summary>
		public static float ReadHalfPrecisionSingle(this NetIncomingMessage message)
		{
			HalfSingle h = new HalfSingle();
			h.PackedValue = message.ReadUInt16();
			return h.ToSingle();
		}

		/// <summary>
		/// Writes a Vector2
		/// </summary>
		public static void Write(this NetOutgoingMessage message, Vector2 vector)
		{
			message.Write(vector.X);
			message.Write(vector.Y);
		}

		/// <summary>
		/// Reads a Vector2
		/// </summary>
		public static Vector2 ReadVector2(this NetIncomingMessage message)
		{
			Vector2 retval;
			retval.X = message.ReadSingle();
			retval.Y = message.ReadSingle();
			return retval;
		}

		/// <summary>
		/// Writes a Vector3
		/// </summary>
		public static void Write(this NetOutgoingMessage message, Vector3 vector)
		{
			message.Write(vector.X);
			message.Write(vector.Y);
			message.Write(vector.Z);
		}

		/// <summary>
		/// Writes a Vector3 at half precision
		/// </summary>
		public static void WriteHalfPrecision(this NetOutgoingMessage message, Vector3 vector)
		{
			message.Write(new HalfSingle(vector.X).PackedValue);
			message.Write(new HalfSingle(vector.Y).PackedValue);
			message.Write(new HalfSingle(vector.Z).PackedValue);
		}

		/// <summary>
		/// Reads a Vector3
		/// </summary>
		public static Vector3 ReadVector3(this NetIncomingMessage message)
		{
			Vector3 retval;
			retval.X = message.ReadSingle();
			retval.Y = message.ReadSingle();
			retval.Z = message.ReadSingle();
			return retval;
		}

		/// <summary>
		/// Writes a Vector3 at half precision
		/// </summary>
		public static Vector3 ReadHalfPrecisionVector3(this NetIncomingMessage message)
		{
			HalfSingle hx = new HalfSingle();
			hx.PackedValue = message.ReadUInt16();

			HalfSingle hy = new HalfSingle();
			hy.PackedValue = message.ReadUInt16();

			HalfSingle hz = new HalfSingle();
			hz.PackedValue = message.ReadUInt16();

			Vector3 retval;
			retval.X = hx.ToSingle();
			retval.Y = hy.ToSingle();
			retval.Z = hz.ToSingle();
			return retval;
		}

		/// <summary>
		/// Writes a Vector4
		/// </summary>
		public static void Write(this NetOutgoingMessage message, Vector4 vector)
		{
			message.Write(vector.X);
			message.Write(vector.Y);
			message.Write(vector.Z);
			message.Write(vector.W);
		}

		/// <summary>
		/// Reads a Vector4
		/// </summary>
		public static Vector4 ReadVector4(this NetIncomingMessage message)
		{
			Vector4 retval;
			retval.X = message.ReadSingle();
			retval.Y = message.ReadSingle();
			retval.Z = message.ReadSingle();
			retval.W = message.ReadSingle();
			return retval;
		}


		/// <summary>
		/// Writes a unit vector (ie. a vector of length 1.0, for example a surface normal) 
		/// using specified number of bits
		/// </summary>
		public static void WriteUnitVector3(this NetOutgoingMessage message, Vector3 unitVector, int numberOfBits)
		{
			float x = unitVector.X;
			float y = unitVector.Y;
			float z = unitVector.Z;
			double invPi = 1.0 / Math.PI;
			float phi = (float)(Math.Atan2(x, y) * invPi);
			float theta = (float)(Math.Atan2(z, Math.Sqrt(x * x + y * y)) * (invPi * 2));

			int halfBits = numberOfBits / 2;
			message.WriteSignedSingle(phi, halfBits);
			message.WriteSignedSingle(theta, numberOfBits - halfBits);
		}

		/// <summary>
		/// Reads a unit vector written using WriteUnitVector3(numberOfBits)
		/// </summary>
		public static Vector3 ReadUnitVector3(this NetIncomingMessage message, int numberOfBits)
		{
			int halfBits = numberOfBits / 2;
			float phi = message.ReadSignedSingle(halfBits) * (float)Math.PI;
			float theta = message.ReadSignedSingle(numberOfBits - halfBits) * (float)(Math.PI * 0.5);

			Vector3 retval;
			retval.X = (float)(Math.Sin(phi) * Math.Cos(theta));
			retval.Y = (float)(Math.Cos(phi) * Math.Cos(theta));
			retval.Z = (float)Math.Sin(theta);

			return retval;
		}

		/// <summary>
		/// Writes a unit quaternion using the specified number of bits per element
		/// for a total of 4 x bitsPerElements bits. Suggested value is 8 to 24 bits.
		/// </summary>
		public static void WriteRotation(this NetOutgoingMessage message, Quaternion quaternion, int bitsPerElement)
		{
			if (quaternion.X > 1.0f)
				quaternion.X = 1.0f;
			if (quaternion.Y > 1.0f)
				quaternion.Y = 1.0f;
			if (quaternion.Z > 1.0f)
				quaternion.Z = 1.0f;
			if (quaternion.W > 1.0f)
				quaternion.W = 1.0f;
			if (quaternion.X < -1.0f)
				quaternion.X = -1.0f;
			if (quaternion.Y < -1.0f)
				quaternion.Y = -1.0f;
			if (quaternion.Z < -1.0f)
				quaternion.Z = -1.0f;
			if (quaternion.W < -1.0f)
				quaternion.W = -1.0f;

			message.WriteSignedSingle(quaternion.X, bitsPerElement);
			message.WriteSignedSingle(quaternion.Y, bitsPerElement);
			message.WriteSignedSingle(quaternion.Z, bitsPerElement);
			message.WriteSignedSingle(quaternion.W, bitsPerElement);
		}

		/// <summary>
		/// Reads a unit quaternion written using WriteRotation(... ,bitsPerElement)
		/// </summary>
		public static Quaternion ReadRotation(this NetIncomingMessage message, int bitsPerElement)
		{
			Quaternion retval;
			retval.X = message.ReadSignedSingle(bitsPerElement);
			retval.Y = message.ReadSignedSingle(bitsPerElement);
			retval.Z = message.ReadSignedSingle(bitsPerElement);
			retval.W = message.ReadSignedSingle(bitsPerElement);
			return retval;
		}

		/// <summary>
		/// Writes an orthonormal matrix (rotation, translation but not scaling or projection)
		/// </summary>
		public static void WriteMatrix(this NetOutgoingMessage message, ref Matrix matrix)
		{
			Quaternion rot = Quaternion.CreateFromRotationMatrix(matrix);
			WriteRotation(message, rot, 24);
			message.Write(matrix.M41);
			message.Write(matrix.M42);
			message.Write(matrix.M43);
		}

		/// <summary>
		/// Writes an orthonormal matrix (rotation, translation but no scaling or projection)
		/// </summary>
		public static void WriteMatrix(this NetOutgoingMessage message, Matrix matrix)
		{
			Quaternion rot = Quaternion.CreateFromRotationMatrix(matrix);
			WriteRotation(message, rot, 24);
			message.Write(matrix.M41);
			message.Write(matrix.M42);
			message.Write(matrix.M43);
		}

		/// <summary>
		/// Reads a matrix written using WriteMatrix()
		/// </summary>
		public static Matrix ReadMatrix(this NetIncomingMessage message)
		{
			Quaternion rot = ReadRotation(message, 24);
			Matrix retval = Matrix.CreateFromQuaternion(rot);
			retval.M41 = message.ReadSingle();
			retval.M42 = message.ReadSingle();
			retval.M43 = message.ReadSingle();
			return retval;
		}

		/// <summary>
		/// Reads a matrix written using WriteMatrix()
		/// </summary>
		public static void ReadMatrix(this NetIncomingMessage message, ref Matrix destination)
		{
			Quaternion rot = ReadRotation(message, 24);
			destination = Matrix.CreateFromQuaternion(rot);
			destination.M41 = message.ReadSingle();
			destination.M42 = message.ReadSingle();
			destination.M43 = message.ReadSingle();
		}

		/// <summary>
		/// Writes a bounding sphere
		/// </summary>
		public static void Write(this NetOutgoingMessage message, BoundingSphere bounds)
		{
			message.Write(bounds.Center.X);
			message.Write(bounds.Center.Y);
			message.Write(bounds.Center.Z);
			message.Write(bounds.Radius);
		}

		/// <summary>
		/// Reads a bounding sphere written using Write(message, BoundingSphere)
		/// </summary>
		public static BoundingSphere ReadBoundingSphere(this NetIncomingMessage message)
		{
			BoundingSphere retval;
			retval.Center.X = message.ReadSingle();
			retval.Center.Y = message.ReadSingle();
			retval.Center.Z = message.ReadSingle();
			retval.Radius = message.ReadSingle();
			return retval;
		}
	}
}
