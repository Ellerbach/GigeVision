﻿using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GenICam
{
    public class GenStringReg : GenCategory, IString
    {
        /// <summary>
        /// Register Address in hex format
        /// </summary>
        public Int64 Address { get; private set; }

        /// <summary>
        /// Register Length
        /// </summary>
        public Int64 Length { get; private set; }

        /// <summary>
        /// Register Access Mode
        /// </summary>
        public GenAccessMode AccessMode { get; private set; }

        public IPort GenPort { get; }
        public string Value { get; set; }
        public string ValueToWrite { get; set; }

        public GenStringReg(CategoryProperties categoryProperties, Int64 address, ushort length, GenAccessMode accessMode, IPort genPort)
        {
            CategoryProperties = categoryProperties;
            Address = address;
            Length = length;
            AccessMode = accessMode;
            GenPort = genPort;
            GetValueCommand = new DelegateCommand(ExecuteGetValueCommand);
            SetValueCommand = new DelegateCommand(ExecuteSetValueCommand);
        }

        private async void ExecuteGetValueCommand()
        {
            Value = await GetValueAsync();
            ValueToWrite = Value;
            RaisePropertyChanged(nameof(Value));
            RaisePropertyChanged(nameof(ValueToWrite));
        }

        private async void ExecuteSetValueCommand()
        {
            if (Value != ValueToWrite)
                await SetValueAsync(ValueToWrite);
        }

        public async Task<string> GetValueAsync()
        {
            var reply = await Get(Length);
            try
            {
                if (!(reply.MemoryValue is null))
                    Value = Encoding.ASCII.GetString(reply.MemoryValue);
            }
            catch (Exception ex)
            {
                throw ex;
            }  
            return Value;
        }

        public async Task<IReplyPacket> SetValueAsync(string value)
        {
            IReplyPacket reply = null ;
            if (PValue is IRegister Register)
            {
                if (Register.AccessMode != GenAccessMode.RO)
                {
                    var length = Register.GetLength();
                    byte[] pBuffer = new byte[length];
                    pBuffer = ASCIIEncoding.ASCII.GetBytes(value);

                    reply = await Register.SetAsync(pBuffer, length);
                    if (reply.IsSentAndReplyReceived && reply.Reply[0] == 0)
                    {
                        if (reply.MemoryValue != null)
                            Value = Encoding.ASCII.GetString(reply.MemoryValue);
                    }
                }
            }

            return reply;
        }

        public long GetMaxLength()
        {
            return Length;
        }

        public async Task<IReplyPacket> Get(long length)
        {
            return await GenPort.ReadAsync(Address, Length);
        }

        public async Task<IReplyPacket> SetAsync(byte[] pBuffer, long length)
        {
            return await GenPort.WriteAsync(pBuffer, Address, length);
        }

        public async Task<long?> GetAddressAsync()
        {
            return Address;
        }

        public long GetLength()
        {
            return Length;
        }

        public async Task<byte[]> GetAsync()
        {
            byte[] addressBytes = Array.Empty<byte>();
            if (await GetAddressAsync() is long address)
            {
                addressBytes = BitConverter.GetBytes(address);
                Array.Reverse(addressBytes);
            }

            return addressBytes;
        }
    }
}