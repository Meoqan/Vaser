using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Security;
using Vaser.global;

namespace Vaser
{
    public class Container
    {
        private Packet_Send _SendPacket = new Packet_Send();
        private List<racefield> _FieldList = new List<racefield>();

        private MemoryStream _ms = null;
        private BinaryWriter _bw = null;

        private int _ReadCounter = 0;
        private int _ReadSize = 0;

        private long _pos1 = 0;
        private long _pos2 = 0;

        private int _ID = 0;

        public void RegisterID(int i)
        {
            _ID = i;
        }

        //public int ID;
        public delegate void readdata(FieldInfo field, Portal portal);
        public delegate void writedata(FieldInfo field);
        public class racefield
        {
            public racefield(FieldInfo f, readdata r, writedata w)
            {
                FI = f;
                rd = r;
                wd = w;
            }

            public FieldInfo FI = null;
            public readdata rd = null;
            public writedata wd = null;
        }

        public Container()
        {
            _ms = new MemoryStream();
            _bw = new BinaryWriter(_ms);

            ScanObjects();
        }

        void ScanObjects()
        {
            try
            {
                object obj = this;

                Type myType = obj.GetType();

                MemberInfo[] myMemberInfo = myType.GetMembers();

                foreach (MemberInfo member in myType.GetMembers())
                {
                    if (member.MemberType == MemberTypes.Field)
                    {
                        racefield rf = BuildRaceField(myType.GetField(member.Name));
                        if(rf != null) _FieldList.Add(rf);
                    }
                }

            }
            catch (SecurityException e)
            {
                Console.WriteLine("Exception : " + e.Message);
            }
        }

        public Packet_Send PackDataObject()
        {
            _SendPacket = new Packet_Send();
            _ms.SetLength(0);

            foreach (racefield field in _FieldList)
            {
                field.wd(field.FI);
                //Write(field.FI.GetValue(this));
            }
            _SendPacket.SendData = _ms.ToArray();

            return _SendPacket;
        }

        public void UnpackDataObject(Packet_Recv pak, Portal portal)
        {
            portal.rms2.Position = pak.StreamPosition;
            _ReadCounter = 0;
            _ReadSize = pak.PacketSize;

            foreach (racefield Rfield in _FieldList)
            {
                Rfield.rd(Rfield.FI, portal);
            }
        }

        public racefield BuildRaceField(FieldInfo field)
        {
            Type typ = field.FieldType;

            //Byte
            if (typ == tbyte)
            {
                return new racefield(field, ReadByte, Writebyte);
            }

            //SByte
            if (typ == tsbyte)
            {
                return new racefield(field, ReadSByte, Writesbyte);
            }

            //Int32
            if (typ == tint32)
            {
                return new racefield(field, ReadInt32, Writeint);
            }

            //UInt32
            if (typ == tuint32)
            {
                return new racefield(field, ReadUInt32, Writeuint);
            }

            //short
            if (typ == tshort)
            {
                return new racefield(field, ReadInt16, Writeshort);
            }

            //ushort
            if (typ == tushort)
            {
                return new racefield(field, ReadUInt16, Writeushort);
            }

            //long
            if (typ == tlong)
            {
                return new racefield(field, ReadInt64, Writelong);
            }

            //ulong
            if (typ == tulong)
            {
                return new racefield(field, ReadUInt64, Writeulong);
            }

            //float
            if (typ == tfloat)
            {
                return new racefield(field, ReadSingle, Writefloat);
            }

            //double
            if (typ == tdouble)
            {
                return new racefield(field, ReadDouble, Writedouble);
            }

            //char
            if (typ == tchar)
            {
                return new racefield(field, ReadChar, Writechar);
            }

            //bool
            if (typ == tbool)
            {
                return new racefield(field, ReadBoolean, Writebool);
            }

            //string
            if (typ == tstring)
            {
                return new racefield(field, ReadString, Writestring);
            }

            //decimal
            if (typ == tdecimal)
            {
                return new racefield(field, ReadDecimal, Writedecimal);
            }

            //NetVector2
            if (typ == tnetvector2)
            {
                return new racefield(field, ReadNetVector2, WriteNetVector2);
            }

            //Byte
            if (typ == tbyteA)
            {
                return new racefield(field, ReadByteA, WritebyteA);
            }

            //SByte
            if (typ == tsbyteA)
            {
                return new racefield(field, ReadSByteA, WritesbyteA);
            }

            //Int32
            if (typ == tint32A)
            {
                return new racefield(field, ReadInt32A, WriteintA);
            }

            //UInt32
            if (typ == tuint32A)
            {
                return new racefield(field, ReadUInt32A, WriteuintA);
            }

            //short
            if (typ == tshortA)
            {
                return new racefield(field, ReadInt16A, WriteshortA);
            }

            //ushort
            if (typ == tushortA)
            {
                return new racefield(field, ReadUInt16A, WriteushortA);
            }

            //long
            if (typ == tlongA)
            {
                return new racefield(field, ReadInt64A, WritelongA);
            }

            //ulong
            if (typ == tulongA)
            {
                return new racefield(field, ReadUInt64A, WriteulongA);
            }

            //float
            if (typ == tfloatA)
            {
                return new racefield(field, ReadSingleA, WritefloatA);
            }

            //double
            if (typ == tdoubleA)
            {
                return new racefield(field, ReadDoubleA, WritedoubleA);
            }

            //char
            if (typ == tcharA)
            {
                return new racefield(field, ReadCharA, WritecharA);
            }

            //bool
            if (typ == tboolA)
            {
                return new racefield(field, ReadBooleanA, WriteboolA);
            }

            //string
            if (typ == tstringA)
            {
                return new racefield(field, ReadStringA, WritestringA);
            }

            //decimal
            if (typ == tdecimalA)
            {
                return new racefield(field, ReadDecimalA, WritedecimalA);
            }

            //NetVector2
            if (typ == tnetvector2A)
            {
                return new racefield(field, ReadNetVector2A, WriteNetVector2A);
            }

            return null;

        }

        public Packet_Send GetSendPacket()
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

        void ReadByte(FieldInfo field, Portal portal)
        {
            field.SetValue(this, portal.rbr2.ReadByte());
        }

        void ReadSByte(FieldInfo field, Portal portal)
        {
            field.SetValue(this, portal.rbr2.ReadSByte());
        }
        
        void ReadInt32(FieldInfo field, Portal portal)
        {
            field.SetValue(this, portal.rbr2.ReadInt32());
        }

        void ReadUInt32(FieldInfo field, Portal portal)
        {
            field.SetValue(this, portal.rbr2.ReadUInt32());
        }
        
        void ReadInt16(FieldInfo field, Portal portal)
        {
            field.SetValue(this, portal.rbr2.ReadInt16());
        }
        
        void ReadUInt16(FieldInfo field, Portal portal)
        {
            field.SetValue(this, portal.rbr2.ReadUInt16());
        }
        
        void ReadInt64(FieldInfo field, Portal portal)
        {
            field.SetValue(this, portal.rbr2.ReadInt64());
        }
        
        void ReadUInt64(FieldInfo field, Portal portal)
        {
            field.SetValue(this, portal.rbr2.ReadUInt64());
        }
        
        void ReadSingle(FieldInfo field, Portal portal)
        {
            field.SetValue(this, portal.rbr2.ReadSingle());
        }
        
        void ReadDouble(FieldInfo field, Portal portal)
        {
            field.SetValue(this, portal.rbr2.ReadDouble());
        }
        
        void ReadChar(FieldInfo field, Portal portal)
        {
            field.SetValue(this, portal.rbr2.ReadChar());
        }
        
        void ReadBoolean(FieldInfo field, Portal portal)
        {
            field.SetValue(this, portal.rbr2.ReadBoolean());
        }
        
        void ReadString(FieldInfo field, Portal portal)
        {
            field.SetValue(this, portal.rbr2.ReadString());
        }
        
        void ReadDecimal(FieldInfo field, Portal portal)
        {
            field.SetValue(this, portal.rbr2.ReadDecimal());
        }
        
        void ReadNetVector2(FieldInfo field, Portal portal)
        {
            field.SetValue(this, new NetVector2(portal.rbr2.ReadSingle(), portal.rbr2.ReadSingle()));
        }
        
        void ReadByteA(FieldInfo field, Portal portal)
        {
            field.SetValue(this, portal.rbr2.ReadBytes(portal.rbr2.ReadInt32()));
        }
        
        void ReadSByteA(FieldInfo field, Portal portal)
        {
            sbyte[] b = new sbyte[portal.rbr2.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = portal.rbr2.ReadSByte();
            field.SetValue(this, b);
        }
        
        void ReadInt32A(FieldInfo field, Portal portal)
        {
            int[] b = new int[portal.rbr2.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = portal.rbr2.ReadInt32();
            field.SetValue(this, b);
        }
        
        void ReadUInt32A(FieldInfo field, Portal portal)
        {
            uint[] b = new uint[portal.rbr2.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = portal.rbr2.ReadUInt32();
            field.SetValue(this, b);
        }
        
        void ReadInt16A(FieldInfo field, Portal portal)
        {
            short[] b = new short[portal.rbr2.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = portal.rbr2.ReadInt16();
            field.SetValue(this, b);
        }
        
        void ReadUInt16A(FieldInfo field, Portal portal)
        {
            ushort[] b = new ushort[portal.rbr2.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = portal.rbr2.ReadUInt16();
            field.SetValue(this, b);
        }
        
        void ReadInt64A(FieldInfo field, Portal portal)
        {
            long[] b = new long[portal.rbr2.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = portal.rbr2.ReadInt64();
            field.SetValue(this, b);
        }
        
        void ReadUInt64A(FieldInfo field, Portal portal)
        {
            ulong[] b = new ulong[portal.rbr2.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = portal.rbr2.ReadUInt64();
            field.SetValue(this, b);
        }
        
        void ReadSingleA(FieldInfo field, Portal portal)
        {
            float[] b = new float[portal.rbr2.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = portal.rbr2.ReadSingle();
            field.SetValue(this, b);
        }
        
        void ReadDoubleA(FieldInfo field, Portal portal)
        {
            double[] b = new double[portal.rbr2.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = portal.rbr2.ReadDouble();
            field.SetValue(this, b);
        }
        
        void ReadCharA(FieldInfo field, Portal portal)
        {
            char[] b = new char[portal.rbr2.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = portal.rbr2.ReadChar();
            field.SetValue(this, b);
        }
        
        void ReadBooleanA(FieldInfo field, Portal portal)
        {
            bool[] b = new bool[portal.rbr2.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = portal.rbr2.ReadBoolean();
            field.SetValue(this, b);
        }
        
        void ReadStringA(FieldInfo field, Portal portal)
        {
            string[] b = new string[portal.rbr2.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = portal.rbr2.ReadString();
            field.SetValue(this, b);
        }
        
        void ReadDecimalA(FieldInfo field, Portal portal)
        {
            decimal[] b = new decimal[portal.rbr2.ReadInt32()];
            for (int x = 0; x < b.Length; x++) b[x] = portal.rbr2.ReadDecimal();
            field.SetValue(this, b);
        }
        
        void ReadNetVector2A(FieldInfo field, Portal portal)
        {
            int c = portal.rbr2.ReadInt32();
            if (c*8 > (_ReadSize - _ReadCounter) || _ReadSize > Options.MaximumPacketSize) throw new Exception("Array is beond the packetlimits! Hacking attempt?");
            _ReadCounter += c * 8;
            NetVector2[] b = new NetVector2[c];
            for (int x = 0; x < b.Length; x++) b[x] = new NetVector2(portal.rbr2.ReadSingle(), portal.rbr2.ReadSingle());
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
            _SendPacket.Counter += 4 + (ia.Length );
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
