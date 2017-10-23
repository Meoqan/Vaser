using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Diagnostics;

namespace Vaser
{
    /// <summary>
    /// The Container class is for inheritance your data packets.
    /// use: public class my_datapacket : Container
    /// </summary>
    public class Container
    {
        private Packet_Send _SendPacket;
        private racefield[] _FieldList = null;

        private MemoryStream _ms = null;
        private BinaryWriter _bw = null;

        private int _ReadCounter = 0;
        private int _ReadSize = 0;

        private long _pos1 = 0;
        private long _pos2 = 0;

        internal delegate void readdata(FieldInfo field);
        internal delegate void writedata(FieldInfo field);
        internal struct racefield
        {
            internal racefield(FieldInfo f, readdata r, writedata w)
            {
                FI = f;
                rd = r;
                wd = w;
            }

            internal FieldInfo FI;
            internal readdata rd;
            internal writedata wd;
        }

        /// <summary>
        /// The Container class is for inheritance your data packets.
        /// </summary>
        public Container()
        {
            //_ms = new MemoryStream();
            //_bw = new BinaryWriter(_ms);

            _DecodeMS = new MemoryStream();
            _DecodeMSReader = new BinaryReader(_DecodeMS);
            //_DecodeMSWriter = new BinaryWriter(_DecodeMS);

            ScanObjects();
        }

        /// <summary>
        /// Free all container resources.
        /// </summary>
        public void Dispose()
        {
            _bw = null;
            _ms = null;
            _FieldList = null;

        }

        void ScanObjects()
        {
            try
            {
                object obj = this;

                Type myType = obj.GetType();

                FieldInfo[] myFieldInfo = myType.GetFields();
                int counter = 0;
                foreach (FieldInfo member in myFieldInfo)
                {
                    if (member.IsPublic && !member.IsStatic && !member.IsInitOnly)
                    {
                        counter++;
                    }
                }
                _FieldList = new racefield[counter];
                counter = 0;
                foreach (FieldInfo member in myFieldInfo)
                {
                    if (member.IsPublic && !member.IsStatic && !member.IsInitOnly)
                    {
                        if (BuildRaceField(myType.GetField(member.Name), out racefield rf))
                        {
                            _FieldList[counter] = rf;
                            counter++;
                        }
                    }
                }

            }
            catch (SecurityException e)
            {
                Debug.WriteLine("Exception : " + e.Message);
            }
        }

        internal Packet_Send PackContainer(BinaryWriter bwpc, MemoryStream mspc)
        {
            _SendPacket = new Packet_Send(null, false);

            //write direct into Portalstream
            _bw = bwpc;
            _ms = mspc;
            foreach (racefield field in _FieldList)
            {
                field.wd(field.FI);
            }
            // set to null, saftey first
            _bw = null;
            _ms = null;

            return _SendPacket;
        }

        internal MemoryStream _DecodeMS = null;
        internal BinaryReader _DecodeMSReader = null;
        //internal BinaryWriter _DecodeMSWriter = null;
        /// <summary>
        /// Unpacks a Packet_Recv data packet.
        /// </summary>
        /// <param name="pak">the packet</param>
        /// <returns>Is true if the unpacking was successful.</returns>
        public bool UnpackContainer(Packet_Recv pak)
        {
            try
            {
                if (pak.Data == null) return false;

                _DecodeMS.SetLength(0);
                _DecodeMS.Write(pak.Data, 0, pak.Data.Length);
                _DecodeMS.Position = 0;

                _ReadCounter = 0;
                _ReadSize = pak.Data.Length;

                foreach (racefield Rfield in _FieldList)
                {
                    Rfield.rd(Rfield.FI);
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Vaser packet decode error: ClassID> " + pak.ClassID + " ContainerID> " + pak.ContainerID + " ObjectID> " + pak.ObjectID + " Packetsize> " + pak.Data.Length + " MEM L> " + _DecodeMS.Length + " P> " + _DecodeMS.Position + "  ERROR> " + e.ToString());
                return false;
            }
        }

        internal bool BuildRaceField(FieldInfo field, out racefield race)
        {
            Type typ = field.FieldType;

            //Byte
            if (typ == tbyte)
            {
                race = new racefield(field, ReadByte, Writebyte);
                return true;
            }

            //SByte
            if (typ == tsbyte)
            {
                race = new racefield(field, ReadSByte, Writesbyte);
                return true;
            }

            //Int32
            if (typ == tint32)
            {
                race = new racefield(field, ReadInt32, Writeint);
                return true;
            }

            //UInt32
            if (typ == tuint32)
            {
                race = new racefield(field, ReadUInt32, Writeuint);
                return true;
            }

            //short
            if (typ == tshort)
            {
                race = new racefield(field, ReadInt16, Writeshort);
                return true;
            }

            //ushort
            if (typ == tushort)
            {
                race = new racefield(field, ReadUInt16, Writeushort);
                return true;
            }

            //long
            if (typ == tlong)
            {
                race = new racefield(field, ReadInt64, Writelong);
                return true;
            }

            //ulong
            if (typ == tulong)
            {
                race = new racefield(field, ReadUInt64, Writeulong);
                return true;
            }

            //float
            if (typ == tfloat)
            {
                race = new racefield(field, ReadSingle, Writefloat);
                return true;
            }

            //double
            if (typ == tdouble)
            {
                race = new racefield(field, ReadDouble, Writedouble);
                return true;
            }

            //char
            if (typ == tchar)
            {
                race = new racefield(field, ReadChar, Writechar);
                return true;
            }

            //bool
            if (typ == tbool)
            {
                race = new racefield(field, ReadBoolean, Writebool);
                return true;
            }

            //string
            if (typ == tstring)
            {
                race = new racefield(field, ReadString, Writestring);
                return true;
            }

            //decimal
            if (typ == tdecimal)
            {
                race = new racefield(field, ReadDecimal, Writedecimal);
                return true;
            }

            //NetVector2
            if (typ == tnetvector2)
            {
                race = new racefield(field, ReadNetVector2, WriteNetVector2);
                return true;
            }

            //Byte
            if (typ == tbyteA)
            {
                race = new racefield(field, ReadByteA, WritebyteA);
                return true;
            }

            //SByte
            if (typ == tsbyteA)
            {
                race = new racefield(field, ReadSByteA, WritesbyteA);
                return true;
            }

            //Int32
            if (typ == tint32A)
            {
                race = new racefield(field, ReadInt32A, WriteintA);
                return true;
            }

            //UInt32
            if (typ == tuint32A)
            {
                race = new racefield(field, ReadUInt32A, WriteuintA);
                return true;
            }

            //short
            if (typ == tshortA)
            {
                race = new racefield(field, ReadInt16A, WriteshortA);
                return true;
            }

            //ushort
            if (typ == tushortA)
            {
                race = new racefield(field, ReadUInt16A, WriteushortA);
                return true;
            }

            //long
            if (typ == tlongA)
            {
                race = new racefield(field, ReadInt64A, WritelongA);
                return true;
            }

            //ulong
            if (typ == tulongA)
            {
                race = new racefield(field, ReadUInt64A, WriteulongA);
                return true;
            }

            //float
            if (typ == tfloatA)
            {
                race = new racefield(field, ReadSingleA, WritefloatA);
                return true;
            }

            //double
            if (typ == tdoubleA)
            {
                race = new racefield(field, ReadDoubleA, WritedoubleA);
                return true;
            }

            //char
            if (typ == tcharA)
            {
                race = new racefield(field, ReadCharA, WritecharA);
                return true;
            }

            //bool
            if (typ == tboolA)
            {
                race = new racefield(field, ReadBooleanA, WriteboolA);
                return true;
            }

            //string
            if (typ == tstringA)
            {
                race = new racefield(field, ReadStringA, WritestringA);
                return true;
            }

            //decimal
            if (typ == tdecimalA)
            {
                race = new racefield(field, ReadDecimalA, WritedecimalA);
                return true;
            }

            //NetVector2
            if (typ == tnetvector2A)
            {
                race = new racefield(field, ReadNetVector2A, WriteNetVector2A);
                return true;
            }

            race = new racefield(null, null, null);
            return false;

        }

        internal Packet_Send GetSendPacket()
        {
            return _SendPacket;
        }

        private static readonly Type tbyte = typeof(byte);
        private static readonly Type tbyteA = typeof(byte[]);

        private static readonly Type tsbyte = typeof(sbyte);
        private static readonly Type tsbyteA = typeof(sbyte[]);

        private static readonly Type tint32 = typeof(int);
        private static readonly Type tint32A = typeof(int[]);

        private static readonly Type tuint32 = typeof(uint);
        private static readonly Type tuint32A = typeof(uint[]);

        private static readonly Type tshort = typeof(short);
        private static readonly Type tshortA = typeof(short[]);

        private static readonly Type tushort = typeof(ushort);
        private static readonly Type tushortA = typeof(ushort[]);

        private static readonly Type tlong = typeof(long);
        private static readonly Type tlongA = typeof(long[]);

        private static readonly Type tulong = typeof(ulong);
        private static readonly Type tulongA = typeof(ulong[]);

        private static readonly Type tfloat = typeof(float);
        private static readonly Type tfloatA = typeof(float[]);

        private static readonly Type tdouble = typeof(double);
        private static readonly Type tdoubleA = typeof(double[]);

        private static readonly Type tchar = typeof(char);
        private static readonly Type tcharA = typeof(char[]);

        private static readonly Type tbool = typeof(bool);
        private static readonly Type tboolA = typeof(bool[]);

        private static readonly Type tstring = typeof(string);
        private static readonly Type tstringA = typeof(string[]);

        private static readonly Type tdecimal = typeof(decimal);
        private static readonly Type tdecimalA = typeof(decimal[]);

        private static readonly Type tnetvector2 = typeof(NetVector2);
        private static readonly Type tnetvector2A = typeof(NetVector2[]);

        void ReadByte(FieldInfo field)
        {
            field.SetValue(this, _DecodeMSReader.ReadByte());
        }

        void ReadSByte(FieldInfo field)
        {
            field.SetValue(this, _DecodeMSReader.ReadSByte());
        }

        void ReadInt32(FieldInfo field)
        {
            field.SetValue(this, _DecodeMSReader.ReadInt32());
        }

        void ReadUInt32(FieldInfo field)
        {
            field.SetValue(this, _DecodeMSReader.ReadUInt32());
        }

        void ReadInt16(FieldInfo field)
        {
            field.SetValue(this, _DecodeMSReader.ReadInt16());
        }

        void ReadUInt16(FieldInfo field)
        {
            field.SetValue(this, _DecodeMSReader.ReadUInt16());
        }

        void ReadInt64(FieldInfo field)
        {
            field.SetValue(this, _DecodeMSReader.ReadInt64());
        }

        void ReadUInt64(FieldInfo field)
        {
            field.SetValue(this, _DecodeMSReader.ReadUInt64());
        }

        void ReadSingle(FieldInfo field)
        {
            field.SetValue(this, _DecodeMSReader.ReadSingle());
        }

        void ReadDouble(FieldInfo field)
        {
            field.SetValue(this, _DecodeMSReader.ReadDouble());
        }

        void ReadChar(FieldInfo field)
        {
            field.SetValue(this, _DecodeMSReader.ReadChar());
        }

        void ReadBoolean(FieldInfo field)
        {
            field.SetValue(this, _DecodeMSReader.ReadBoolean());
        }

        void ReadString(FieldInfo field)
        {
            field.SetValue(this, _DecodeMSReader.ReadString());
        }

        void ReadDecimal(FieldInfo field)
        {
            field.SetValue(this, _DecodeMSReader.ReadDecimal());
        }

        void ReadNetVector2(FieldInfo field)
        {
            field.SetValue(this, new NetVector2(_DecodeMSReader.ReadSingle(), _DecodeMSReader.ReadSingle()));
        }

        void ReadByteA(FieldInfo field)
        {
            field.SetValue(this, _DecodeMSReader.ReadBytes(_DecodeMSReader.ReadInt32()));
        }

        void ReadSByteA(FieldInfo field)
        {
            sbyte[] b = new sbyte[_DecodeMSReader.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = _DecodeMSReader.ReadSByte();
            field.SetValue(this, b);
        }

        void ReadInt32A(FieldInfo field)
        {
            int[] b = new int[_DecodeMSReader.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = _DecodeMSReader.ReadInt32();
            field.SetValue(this, b);
        }

        void ReadUInt32A(FieldInfo field)
        {
            uint[] b = new uint[_DecodeMSReader.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = _DecodeMSReader.ReadUInt32();
            field.SetValue(this, b);
        }

        void ReadInt16A(FieldInfo field)
        {
            short[] b = new short[_DecodeMSReader.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = _DecodeMSReader.ReadInt16();
            field.SetValue(this, b);
        }

        void ReadUInt16A(FieldInfo field)
        {
            ushort[] b = new ushort[_DecodeMSReader.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = _DecodeMSReader.ReadUInt16();
            field.SetValue(this, b);
        }

        void ReadInt64A(FieldInfo field)
        {
            long[] b = new long[_DecodeMSReader.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = _DecodeMSReader.ReadInt64();
            field.SetValue(this, b);
        }

        void ReadUInt64A(FieldInfo field)
        {
            ulong[] b = new ulong[_DecodeMSReader.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = _DecodeMSReader.ReadUInt64();
            field.SetValue(this, b);
        }

        void ReadSingleA(FieldInfo field)
        {
            float[] b = new float[_DecodeMSReader.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = _DecodeMSReader.ReadSingle();
            field.SetValue(this, b);
        }

        void ReadDoubleA(FieldInfo field)
        {
            double[] b = new double[_DecodeMSReader.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = _DecodeMSReader.ReadDouble();
            field.SetValue(this, b);
        }

        void ReadCharA(FieldInfo field)
        {
            char[] b = new char[_DecodeMSReader.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = _DecodeMSReader.ReadChar();
            field.SetValue(this, b);
        }

        void ReadBooleanA(FieldInfo field)
        {
            bool[] b = new bool[_DecodeMSReader.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = _DecodeMSReader.ReadBoolean();
            field.SetValue(this, b);
        }

        void ReadStringA(FieldInfo field)
        {
            string[] b = new string[_DecodeMSReader.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = _DecodeMSReader.ReadString();
            field.SetValue(this, b);
        }

        void ReadDecimalA(FieldInfo field)
        {
            decimal[] b = new decimal[_DecodeMSReader.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = _DecodeMSReader.ReadDecimal();
            field.SetValue(this, b);
        }

        int MaximumPacketSize = Options.MaximumPacketSize;
        void ReadNetVector2A(FieldInfo field)
        {
            int c = _DecodeMSReader.ReadInt32();
            if (c * 8 > (_ReadSize - _ReadCounter) || _ReadSize > MaximumPacketSize) throw new Exception("Array is beond the packetlimits! Hacking attempt?");
            _ReadCounter += c * 8;
            NetVector2[] b = new NetVector2[c];
            for (int x = 0; x < b.Length; x++) b[x] = new NetVector2(_DecodeMSReader.ReadSingle(), _DecodeMSReader.ReadSingle());
            field.SetValue(this, b);
        }








        void Writebyte(FieldInfo FI)
        {
            byte b = (byte)FI.GetValue(this);
            _bw.Write(b);
            _SendPacket.Counter += 1;
        }
        void WritebyteA(FieldInfo FI)
        {
            byte[] ba = (byte[])FI.GetValue(this);
            _bw.Write(ba.Length);
            _bw.Write(ba);
            _SendPacket.Counter += 4 + ba.Length;
        }

        void Writesbyte(FieldInfo FI)
        {
            sbyte sb = (sbyte)FI.GetValue(this);
            _bw.Write(sb);
            _SendPacket.Counter += 1;
        }
        void WritesbyteA(FieldInfo FI)
        {
            sbyte[] sba = (sbyte[])FI.GetValue(this);
            _bw.Write(sba.Length);
            foreach (sbyte sb in sba) _bw.Write(sb);
            _SendPacket.Counter += 4 + sba.Length;
        }

        void Writeint(FieldInfo FI)
        {
            int i = (int)FI.GetValue(this);
            _bw.Write(i);
            _SendPacket.Counter += 4;
        }
        void WriteintA(FieldInfo FI)
        {
            int[] ia = (int[])FI.GetValue(this);
            _bw.Write(ia.Length);
            foreach (int i in ia) _bw.Write(i);
            _SendPacket.Counter += 4 + (ia.Length * 4);
        }

        void Writeuint(FieldInfo FI)
        {
            uint i = (uint)FI.GetValue(this);
            _bw.Write(i);
            _SendPacket.Counter += 4;
        }
        void WriteuintA(FieldInfo FI)
        {
            uint[] ia = (uint[])FI.GetValue(this);
            _bw.Write(ia.Length);
            foreach (uint i in ia) _bw.Write(i);
            _SendPacket.Counter += 4 + (ia.Length * 4);
        }

        void Writeshort(FieldInfo FI)
        {
            short i = (short)FI.GetValue(this);
            _bw.Write(i);
            _SendPacket.Counter += 2;
        }
        void WriteshortA(FieldInfo FI)
        {
            short[] ia = (short[])FI.GetValue(this);
            _bw.Write(ia.Length);
            foreach (short i in ia) _bw.Write(i);
            _SendPacket.Counter += 4 + (ia.Length * 2);
        }

        void Writeushort(FieldInfo FI)
        {
            ushort i = (ushort)FI.GetValue(this);
            _bw.Write(i);
            _SendPacket.Counter += 2;
        }
        void WriteushortA(FieldInfo FI)
        {
            ushort[] ia = (ushort[])FI.GetValue(this);
            _bw.Write(ia.Length);
            foreach (ushort i in ia) _bw.Write(i);
            _SendPacket.Counter += 4 + (ia.Length * 2);
        }

        void Writelong(FieldInfo FI)
        {
            long i = (long)FI.GetValue(this);
            _bw.Write(i);
            _SendPacket.Counter += 8;
        }
        void WritelongA(FieldInfo FI)
        {
            long[] ia = (long[])FI.GetValue(this);
            _bw.Write(ia.Length);
            foreach (long i in ia) _bw.Write(i);
            _SendPacket.Counter += 4 + (ia.Length * 8);
        }


        void Writeulong(FieldInfo FI)
        {
            ulong i = (ulong)FI.GetValue(this);
            _bw.Write(i);
            _SendPacket.Counter += 8;
        }
        void WriteulongA(FieldInfo FI)
        {
            ulong[] ia = (ulong[])FI.GetValue(this);
            _bw.Write(ia.Length);
            foreach (ulong i in ia) _bw.Write(i);
            _SendPacket.Counter += 4 + (ia.Length * 8);
        }

        void Writefloat(FieldInfo FI)
        {
            float i = (float)FI.GetValue(this);
            _bw.Write(i);
            _SendPacket.Counter += 4;
        }
        void WritefloatA(FieldInfo FI)
        {
            float[] ia = (float[])FI.GetValue(this);
            _bw.Write(ia.Length);
            foreach (float i in ia) _bw.Write(i);
            _SendPacket.Counter += 4 + (ia.Length * 4);
        }

        void Writedouble(FieldInfo FI)
        {
            double i = (double)FI.GetValue(this);
            _bw.Write(i);
            _SendPacket.Counter += 8;
        }
        void WritedoubleA(FieldInfo FI)
        {
            double[] ia = (double[])FI.GetValue(this);
            _bw.Write(ia.Length);
            foreach (double i in ia) _bw.Write(i);
            _SendPacket.Counter += 4 + (ia.Length * 8);
        }

        void Writechar(FieldInfo FI)
        {
            char i = (char)FI.GetValue(this);
            _pos1 = _ms.Position;
            _bw.Write(i);
            _pos2 = _ms.Position;

            _SendPacket.Counter += (int)(_pos2 - _pos1);
        }
        void WritecharA(FieldInfo FI)
        {
            char[] ia = (char[])FI.GetValue(this);
            _bw.Write(ia.Length);
            foreach (char i in ia)
            {
                _pos1 = _ms.Position;
                _bw.Write(i);
                _pos2 = _ms.Position;

                _SendPacket.Counter += (int)(_pos2 - _pos1);
            }
            _SendPacket.Counter += 4;
        }

        void Writebool(FieldInfo FI)
        {
            bool i = (bool)FI.GetValue(this);
            _bw.Write(i);

            _SendPacket.Counter += 1;
        }
        void WriteboolA(FieldInfo FI)
        {
            bool[] ia = (bool[])FI.GetValue(this);
            _bw.Write(ia.Length);
            foreach (bool i in ia) _bw.Write(i);
            _SendPacket.Counter += 4 + (ia.Length);
        }

        void Writestring(FieldInfo FI)
        {
            string S = (string)FI.GetValue(this);

            if (S == null) S = "";

            _pos1 = _ms.Position;
            _bw.Write(S);
            _pos2 = _ms.Position;

            _SendPacket.Counter += (int)(_pos2 - _pos1);
        }
        void WritestringA(FieldInfo FI)
        {
            string[] ia = (string[])FI.GetValue(this);

            if (ia == null) ia = new string[1];

            _bw.Write(ia.Length);

            foreach (string i in ia)
            {
                _pos1 = _ms.Position;
                if (i != null)
                {
                    _bw.Write(i);
                }
                else
                {
                    _bw.Write("");
                }
                _pos2 = _ms.Position;

                _SendPacket.Counter += (int)(_pos2 - _pos1);
            }
            _SendPacket.Counter += 4;
        }

        void Writedecimal(FieldInfo FI)
        {
            decimal i = (decimal)FI.GetValue(this);
            _bw.Write(i);

            _SendPacket.Counter += 16;
        }
        void WritedecimalA(FieldInfo FI)
        {
            decimal[] ia = (decimal[])FI.GetValue(this);
            _bw.Write(ia.Length);

            foreach (decimal i in ia)
            {
                _bw.Write(i);

                _SendPacket.Counter += 16;
            }
            _SendPacket.Counter += 4;
        }

        void WriteNetVector2(FieldInfo FI)
        {
            NetVector2 nv2 = (NetVector2)FI.GetValue(this);
            _bw.Write(nv2.Data[0]);
            _bw.Write(nv2.Data[1]);
            _SendPacket.Counter += 8;
        }
        void WriteNetVector2A(FieldInfo FI)
        {
            NetVector2[] ia = (NetVector2[])FI.GetValue(this);
            _bw.Write(ia.Length);
            foreach (NetVector2 i in ia)
            {
                _bw.Write(i.Data[0]);
                _bw.Write(i.Data[1]);
            }
            _SendPacket.Counter += 4 + (ia.Length * 8);
        }
    }
}
