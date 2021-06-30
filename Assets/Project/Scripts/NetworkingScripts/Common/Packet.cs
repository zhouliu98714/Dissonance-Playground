using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CEMSIM
{
    namespace Network
    {
        /// <summary>
        /// Packet class deals with packet formulation and digestion.
        /// </summary>
        public class Packet : IDisposable
        {
            // data first add to buffer, then transfer to readableBuffer
            private List<byte> buffer;     ///> packet bytes (including header and payload)
            private long utcticks;         ///> packet generation time. 0 (not enabled) or DateTime.UtcNow.Ticks
            private byte[] readableBuffer; 
            private int readPos;
            private int id;                ///> packet id

            private bool disposed = false; ///> true: this object has been manually disposed

            #region Constructors
            /// <summary>
            /// Create an empty packet
            /// </summary>
            public Packet()
            {
                buffer = new List<byte>(); // initialize an empty buffer
                utcticks = 0;
                readPos = 0;
                id = 0;
            }

            /// <summary>
            /// Create an empty packet with id
            /// </summary>
            /// <param name="_id"></param>
            public Packet(int _id)
            {
                buffer = new List<byte>();
                utcticks = 0;
                readPos = 0;

                //Write(_id); // write id to the buffer
                id = _id;
            }

            /// <summary>
            /// Create a packet with known data
            /// </summary>
            /// <param name="_data"></param>
            public Packet(byte[] _data)
            {
                buffer = new List<byte>();
                utcticks = 0;
                readPos = 0;

                SetBytes(_data);
            }
            #endregion

            #region functions
            /// <summary>
            /// Set the packet data, and copy the packet to readableBuffer.
            /// Append input data to the end of a List<byte>, and then convert to byte[]
            /// </summary>
            /// <param name="_data"></param>
            public void SetBytes(byte[] _data)
            {
                Write(_data);
                readableBuffer = buffer.ToArray();
            }

            /// <summary>
            /// Insert int32 to the beginning of the packet
            /// </summary>
            /// <param name="_value"></param>
            public void InsertInt32(int _value)
            {
                buffer.InsertRange(0, BitConverter.GetBytes(_value));
            }

            /// <summary>
            /// Insert long(int64) to the beginning of the packet
            /// </summary>
            /// <param name="_value"></param>
            public void InsertInt64(long _value)
            {
                buffer.InsertRange(0, BitConverter.GetBytes(_value));
            }

            /// <summary>
            /// Compute the packet size and add it at the very beginning of the packet
            /// </summary>
            public void WriteLength()
            {
                buffer.InsertRange(0, BitConverter.GetBytes(buffer.Count));
            }

            /// <summary>
            /// Generate the header information and add it at the beginning of the packet
            /// The header includes:
            ///     Packet length: int32 - everything in the packet except the integer packet length
            ///     Sending time : int64 (or long) 0 if not used, utctick (e.g. DateTime.UtcNow.Ticks)
            /// </summary>
            public void WriteHeader(bool addTime=false)
            {
                // we insert from the last segment in the header to the first.

                // add packet id
                InsertInt32(id);

                // generation time
                if (addTime)
                {
                    InsertInt64(DateTime.UtcNow.Ticks);
                }
                else
                {
                    InsertInt64(0);
                }

                // Packet length
                WriteLength();
            }

            /// <summary>
            /// DigestHeader reads the header information except the packet length,
            /// and assigns the packet object attributions based on the data.
            /// Packet length is not read because the TCP/UDP handling function
            /// will first read the packet length to determine the packet size.
            /// Therefore, the "data" includes everything after the packet length
            /// segment.
            /// </summary>
            public int DigestServerHeader()
            {
                // extract packet time
                utcticks = ReadInt64();

                // extract packet id
                id = ReadInt32();
                if (!Enum.IsDefined(typeof(ServerPackets), id))
                    id = (int)ServerPackets.invalidPacket;

                return id;
            }

            public int DigestClientHeader()
            {
                // extract packet time
                utcticks = ReadInt64();

                // extract packet id
                id = ReadInt32();
                if (!Enum.IsDefined(typeof(ClientPackets), id))
                    id = (int)ClientPackets.invalidPacket;

                return id;
            }

            public long getUtcTicks()
            {
                return utcticks;
            }

            public int GetPacketId()
            {
                return id;
            }

            /// <summary>
            /// Get the packet content in bytes
            /// </summary>
            /// <returns></returns>
            public byte[] ToArray()
            {
                readableBuffer = buffer.ToArray();
                return readableBuffer;
            }

            /// <summary>
            /// Get the length of buffer
            /// </summary>
            /// <returns></returns>
            public int Length()
            {
                return buffer.Count;
            }

            /// <summary>
            /// Return the number of unread bytes in buffer
            /// </summary>
            /// <returns></returns>
            public int UnreadLength()
            {
                return buffer.Count - readPos;
            }

            /// <summary>
            /// Clear the package for reuse
            /// </summary>
            /// <param name="_shouldReset">Whether to reset the packet or move read pointer backward to previous location</param>
            public void Reset(bool _shouldReset = true)
            {
                if (_shouldReset)
                {
                    buffer.Clear();
                    readableBuffer = null;
                    readPos = 0;
                }
                else
                {
                    readPos -= 4; // if the packet is a sub-packet, the first 4 bytes is not the data length but 4 bytes of data.
                }
            }

            #endregion

            #region Write functions
            /// <summary>
            /// Write function for all possible input types
            /// </summary>
            /// <param name="_value"></param>
            public void Write(byte _value)
            {
                buffer.Add(_value);
            }

            /// <summary>
            /// Add a byte array to the buffer
            /// </summary>
            /// <param name="_value"></param>
            public void Write(byte[] _value)
            {
                // may need to check whether _value is longer than the size of the buffer.
                buffer.AddRange(_value);
            }

            /// <summary>
            /// Convert a short variable to bytes and add bytes to buffer
            /// </summary>
            /// <param name="_value"></param>
            public void Write(Int16 _value)
            {
                buffer.AddRange(BitConverter.GetBytes(_value));
            }

            /// <summary>
            /// Convert a int variable to bytes and add bytes to buffer
            /// </summary>
            /// <param name="_value"></param>
            public void Write(Int32 _value)
            {
                buffer.AddRange(BitConverter.GetBytes(_value));
            }

            /// <summary>
            /// Convert a long variable to bytes and add bytes to buffer
            /// </summary>
            /// <param name="_value"></param>
            public void Write(Int64 _value)
            {
                buffer.AddRange(BitConverter.GetBytes(_value));
            }

            /// <summary>
            /// Convert a float variable to bytes and add bytes to buffer
            /// </summary>
            /// <param name="_value"></param>
            public void Write(float _value)
            {
                buffer.AddRange(BitConverter.GetBytes(_value));
            }

            /// <summary>
            /// Convert a boolean variable to bytes and add bytes to buffer
            /// </summary>
            /// <param name="_value"></param>
            public void Write(bool _value)
            {
                buffer.AddRange(BitConverter.GetBytes(_value));
            }

            /// <summary>
            /// Convert a double float variable to bytes and add bytes to buffer
            /// </summary>
            /// <param name="_value"></param>
            public void Write(double _value)
            {
                buffer.AddRange(BitConverter.GetBytes(_value));
            }

            /// <summary>
            /// Write a string with a prefix of the string length to the packet
            /// </summary>
            /// <param name="_value">string</param>
            public void Write(string _value)
            {
                // we first write the length of the string to the buffer
                Write(_value.Length);
                // then the bytes of the string
                buffer.AddRange(Encoding.UTF8.GetBytes(_value));
            }

            /// <summary>
            /// Serialize the Vector3 object into three int32 instances and write them to the packet
            /// </summary>
            /// <param name="_value">Vector3 instance</param>
            public void Write(Vector3 _value)
            {
                Write((float)_value.x);
                Write((float)_value.y);
                Write((float)_value.z);
            }

            /// <summary>
            /// Serialize the Quaternion object into four int32 instances and write them to the packet
            /// </summary>
            /// <param name="_value">Quaternion instance</param>
            public void Write(Quaternion _value)
            {
                Write((float)_value.x);
                Write((float)_value.y);
                Write((float)_value.z);
                Write((float)_value.w);
            }
            #endregion

            #region Read functions
            /// <summary>
            /// Get the byte in the buffer at $readPos
            /// </summary>
            /// <param name="_moveReadPos"></param>
            /// <returns></returns>
            public byte ReadByte(bool _moveReadPos = true)
            {
                if (readPos < buffer.Count)
                {
                    byte _value = buffer[readPos];
                    if (_moveReadPos)
                        readPos += 1;
                    return _value;
                }
                else
                {
                    throw new Exception("No space in buffer for value of type 'byte'!");
                }
            }

            /// <summary>
            /// Get $_length bytes from the buffer
            /// </summary>
            /// <param name="_moveReadPos"></param>
            /// <returns></returns>
            public byte[] ReadBytes(int _length, bool _moveReadPos = true)
            {
                if (readPos + _length <= buffer.Count)
                {
                    byte[] _value = buffer.GetRange(readPos, _length).ToArray();
                    if (_moveReadPos)
                        readPos += _length;
                    return _value;
                }
                else
                {
                    throw new Exception($"No space in buffer for {_length} length of 'bytes'");
                }
            }

            public Int16 ReadInt16(bool _moveReadPos = true)
            {
                if (readPos + 2 <= buffer.Count)
                {
                    Int16 _value = BitConverter.ToInt16(readableBuffer, readPos);
                    if (_moveReadPos)
                        readPos += 2;
                    return _value;
                }
                else
                {
                    throw new Exception("No space in buffer for value of type 'int16'");
                }

            }

            public Int32 ReadInt32(bool _moveReadPos = true)
            {
                if (readPos + 4 <= buffer.Count)
                {
                    Int32 _value = BitConverter.ToInt32(readableBuffer, readPos);
                    if (_moveReadPos)
                        readPos += 4;
                    return _value;
                }
                else
                {
                    throw new Exception("No space in buffer for value of type 'int32'");
                }

            }

            public Int64 ReadInt64(bool _moveReadPos = true)
            {
                if (readPos + 8 <= buffer.Count)
                {
                    Int64 _value = BitConverter.ToInt32(readableBuffer, readPos);
                    if (_moveReadPos)
                        readPos += 8;
                    return _value;
                }
                else
                {
                    throw new Exception("No space in buffer for value of type 'int64'");
                }
            }

            public float ReadFloat(bool _moveReadPos = true)
            {
                if (readPos + 4 <= buffer.Count)
                {
                    float _value = BitConverter.ToSingle(readableBuffer, readPos);
                    if (_moveReadPos)
                        readPos += 4;
                    return _value;
                }
                else
                {
                    throw new Exception("No space in buffer for value of type 'float'");
                }
            }

            public double ReadDouble(bool _moveReadPos = true)
            {
                if (readPos + 8 <= buffer.Count)
                {
                    double _value = BitConverter.ToDouble(readableBuffer, readPos);
                    if (_moveReadPos)
                        readPos += 8;
                    return _value;
                }
                else
                {
                    throw new Exception("No space in buffer for value of type 'double'");
                }
            }

            public bool ReadBool(bool _moveReadPos = true)
            {
                if (readPos < buffer.Count)
                {
                    bool _value = BitConverter.ToBoolean(readableBuffer, readPos);
                    if (_moveReadPos)
                        readPos += 1;
                    return _value;
                }
                else
                {
                    throw new Exception("No space in buffer for value of type 'bool'");
                }
            }

            public string ReadString(bool _moveReadPos = true)
            {
                try
                {
                    int _stringLen = ReadInt32();
                    string _value = Encoding.UTF8.GetString(readableBuffer, readPos, _stringLen);
                    if (_moveReadPos && _value.Length > 0) // we also need to guarantee the string is not empty
                        readPos += _stringLen;
                    return _value;
                }
                catch
                {
                    throw new Exception("No space in buffer for value of type 'String'");
                }
            }

            public Vector3 ReadVector3(bool _moveReadPos = true)
            {
                try
                {
                    float _X = ReadFloat();
                    float _Y = ReadFloat();
                    float _Z = ReadFloat();

                    return new Vector3(_X, _Y, _Z);
                }
                catch
                {
                    throw new Exception("No space in buffer for value of type 'Vector3'");
                }
            }

            public Quaternion ReadQuaternion(bool _moveReadPos = true)
            {
                try
                {
                    float _X = ReadFloat();
                    float _Y = ReadFloat();
                    float _Z = ReadFloat();
                    float _W = ReadFloat();

                    return new Quaternion(_X, _Y, _Z, _W);
                }
                catch
                {
                    throw new Exception("No space in buffer for value of type 'Quaternion'");
                }
            }
            #endregion

            /// <summary>
            /// A distructor
            /// </summary>
            /// <param name="_disposing"></param>
            protected virtual void Dispose(bool _disposing)
            {
                if (!disposed)
                {
                    if (_disposing)
                    {
                        buffer = null;
                        readableBuffer = null;
                        readPos = 0;
                    }
                    disposed = true;
                }
            }

            public void Dispose()
            {
                // since we manually dispose the object, no need to call the system garbage collector.
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }
    }
}