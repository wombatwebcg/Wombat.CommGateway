using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Wombat.Extensions.AutoGenerator.Attributes;

namespace Wombat.CommGateway.Infrastructure.Communication
{
    /// <summary>
    /// 协议工厂实现
    /// </summary>
    /// 
    [AutoInject(typeof(IProtocolFactory))]
    public class ProtocolFactory : IProtocolFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, Type> _protocolTypes;

        public ProtocolFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _protocolTypes = new Dictionary<string, Type>
            {
                { "ModbusTcp", typeof(ModbusTcpProtocol) },
                { "ModbusRtu", typeof(ModbusRtuProtocol) },
                { "SiemensS7", typeof(SiemensS7Protocol) },
                { "MitsubishiMc", typeof(MitsubishiMcProtocol) },
                { "OmronFins", typeof(OmronFinsProtocol) }
            };
        }

        public IProtocol CreateProtocol(string protocolType, Dictionary<string, string> configuration)
        {
            if (!_protocolTypes.TryGetValue(protocolType, out var type))
            {
                throw new ArgumentException($"Unsupported protocol type: {protocolType}");
            }

            var protocol = (IProtocol)ActivatorUtilities.CreateInstance(_serviceProvider, type);
            protocol.InitializeAsync(configuration).Wait();
            return protocol;
        }

        public IEnumerable<string> GetSupportedProtocolTypes()
        {
            return _protocolTypes.Keys;
        }
    }
} 