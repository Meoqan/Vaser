using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Vaser.global;
using System.Threading;
using System.Security.Cryptography;

namespace Vaser
{
    public class DualCryptoStream
    {
        private SemaphoreSlim _Settings_ThreadLock = new SemaphoreSlim(1);
        private Thread _ClientThread;

        private MemoryStream _sms1 = new MemoryStream();
        private MemoryStream _sms2 = new MemoryStream();

        private MemoryStream _rms1 = new MemoryStream();
        private MemoryStream _rms2 = new MemoryStream();


        private bool _ThreadIsRunning = true;
        public bool ThreadIsRunning
        {
            get
            {
                _Settings_ThreadLock.Wait();
                bool ret = _ThreadIsRunning;
                _Settings_ThreadLock.Release();
                return ret;
            }
            set
            {
                _Settings_ThreadLock.Wait();
                _ThreadIsRunning = value;
                _Settings_ThreadLock.Release();
            }
        }

        public DualCryptoStream()
        {
            _ClientThread = new Thread(CryptoThread);
            _ClientThread.Start();
        }

        private void CryptoThread()
        {
            byte[] Key = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };
            byte[] IV = { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16 };

            RijndaelManaged RijndaelCipher = new RijndaelManaged();

            RijndaelCipher.Mode = CipherMode.CBC;

            ICryptoTransform Encryptor = RijndaelCipher.CreateEncryptor(Key, IV);


            MemoryStream memoryStream = new MemoryStream();

            CryptoStream cryptoStream = new CryptoStream(memoryStream, Encryptor, CryptoStreamMode.Write);

            

            while (ThreadIsRunning && Options.Operating)
            {

                byte[] ret = _sms1.ToArray();
                cryptoStream.Write(ret, 0, ret.Length);
                cryptoStream.FlushFinalBlock();
                byte[] ret2 = memoryStream.ToArray();
                _sms1.Write(ret2, 0, ret2.Length);
            }

            memoryStream.Close();
            cryptoStream.Close();
        }

        public void SendWrite(byte[] Data)
        {
            _sms1.Write(Data, 0, Data.Length);
        }
        public byte[] SendRead()
        {
            byte[] ret = _sms2.ToArray();
            _sms2.SetLength(0);
            return ret;
        }

        public void ReceiveWrite(byte[] Data)
        {
            _rms1.Write(Data, 0, Data.Length);
        }
        public byte[] ReceiveRead()
        {
            byte[] ret = _rms2.ToArray();
            _rms2.SetLength(0);
            return ret;
        }

        
    }
}
