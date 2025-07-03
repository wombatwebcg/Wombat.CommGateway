using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NModbus;

namespace Wombat.CommGateway.Infrastructure.Communication
{
    /// <summary>
    /// Modbus RTU协议实现
    /// </summary>
    public class ModbusRtuProtocol : IProtocol
    {
        private IModbusMaster _master;
        private string _portName;
        private int _baudRate;
        private int _dataBits;
        private System.IO.Ports.Parity _parity;
        private System.IO.Ports.StopBits _stopBits;
        private byte _slaveId;
        private string _lastError;

        public string Name => "ModbusRtu";

        public bool IsConnected => _master != null;

        public async Task InitializeAsync(Dictionary<string, string> configuration)
        {
            try
            {
                //_portName = configuration["PortName"];
                //_baudRate = int.Parse(configuration["BaudRate"]);
                //_dataBits = int.Parse(configuration["DataBits"]);
                //_parity = (System.IO.Ports.Parity)Enum.Parse(typeof(System.IO.Ports.Parity), configuration["Parity"]);
                //_stopBits = (System.IO.Ports.StopBits)Enum.Parse(typeof(System.IO.Ports.StopBits), configuration["StopBits"]);
                //_slaveId = byte.Parse(configuration["SlaveId"]);

                //var factory = new ModbusFactory();
                //var serialPort = new System.IO.Ports.SerialPort(_portName, _baudRate, _parity, _dataBits, _stopBits);
                //var adapter = new SerialPortAdapter(serialPort);
                //_master = factory.CreateMaster(adapter);
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                throw;
            }
        }

        public async Task<object> ReadAsync(string address, string dataType)
        {
            try
            {
                //var (functionCode, registerAddress) = ParseAddress(address);
                //switch (dataType.ToLower())
                //{
                //    case "bool":
                //        return await _master.ReadCoilsAsync(_slaveId, registerAddress, 1)[0];
                //    case "int16":
                //        return await _master.ReadHoldingRegistersAsync(_slaveId, registerAddress, 1)[0];
                //    case "uint16":
                //        return (ushort)await _master.ReadHoldingRegistersAsync(_slaveId, registerAddress, 1)[0];
                //    case "int32":
                //        var registers = await _master.ReadHoldingRegistersAsync(_slaveId, registerAddress, 2);
                //        return BitConverter.ToInt32(BitConverter.GetBytes(registers[0]), 0);
                //    case "float":
                //        registers = await _master.ReadHoldingRegistersAsync(_slaveId, registerAddress, 2);
                //        return BitConverter.ToSingle(BitConverter.GetBytes(registers[0]), 0);
                //    default:
                //        throw new ArgumentException($"Unsupported data type: {dataType}");
                //}

                return null;
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                throw;
            }
        }

        public async Task WriteAsync(string address, string dataType, object value)
        {
            try
            {
                //var (functionCode, registerAddress) = ParseAddress(address);
                //switch (dataType.ToLower())
                //{
                //    case "bool":
                //        await _master.WriteSingleCoilAsync(_slaveId, registerAddress, (bool)value);
                //        break;
                //    case "int16":
                //        await _master.WriteSingleRegisterAsync(_slaveId, registerAddress, (short)value);
                //        break;
                //    case "uint16":
                //        await _master.WriteSingleRegisterAsync(_slaveId, registerAddress, (ushort)value);
                //        break;
                //    case "int32":
                //        var int32Value = (int)value;
                //        var int32Bytes = BitConverter.GetBytes(int32Value);
                //        await _master.WriteMultipleRegistersAsync(_slaveId, registerAddress, new ushort[] { BitConverter.ToUInt16(int32Bytes, 0), BitConverter.ToUInt16(int32Bytes, 2) });
                //        break;
                //    case "float":
                //        var floatValue = (float)value;
                //        var floatBytes = BitConverter.GetBytes(floatValue);
                //        await _master.WriteMultipleRegistersAsync(_slaveId, registerAddress, new ushort[] { BitConverter.ToUInt16(floatBytes, 0), BitConverter.ToUInt16(floatBytes, 2) });
                //        break;
                //    default:
                //        throw new ArgumentException($"Unsupported data type: {dataType}");
                //}
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                throw;
            }
        }

        public async Task<Dictionary<string, object>> BatchReadAsync(Dictionary<string, string> addressDataTypeMap)
        {
            var result = new Dictionary<string, object>();
            foreach (var kvp in addressDataTypeMap)
            {
                result[kvp.Key] = await ReadAsync(kvp.Key, kvp.Value);
            }
            return result;
        }

        public async Task BatchWriteAsync(Dictionary<string, (string DataType, object Value)> addressValueMap)
        {
            foreach (var kvp in addressValueMap)
            {
                await WriteAsync(kvp.Key, kvp.Value.DataType, kvp.Value.Value);
            }
        }

        public async Task ConnectAsync()
        {
            try
            {
                //var factory = new ModbusFactory();
                //var serialPort = new System.IO.Ports.SerialPort(_portName, _baudRate, _parity, _dataBits, _stopBits);
                //var adapter = new SerialPortAdapter(serialPort);
                //_master = factory.CreateMaster(adapter);
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                throw;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_master != null)
            {
                _master.Dispose();
                _master = null;
            }
        }

        public string GetLastError()
        {
            return _lastError;
        }

        private (byte FunctionCode, ushort RegisterAddress) ParseAddress(string address)
        {
            // 地址格式：功能码:寄存器地址
            // 例如：3:40001 表示保持寄存器，地址40001
            var parts = address.Split(':');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid address format");
            }

            var functionCode = byte.Parse(parts[0]);
            var registerAddress = ushort.Parse(parts[1]);

            return (functionCode, registerAddress);
        }
    }
} 