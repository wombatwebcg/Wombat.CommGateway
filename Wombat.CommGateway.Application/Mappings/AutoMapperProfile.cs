using AutoMapper;
using System;
using System.Linq;
using Wombat.CommGateway.Domain.Entities;
using Wombat.CommGateway.Application.DTOs;

namespace Wombat.CommGateway.Application.Mappings
{
    /// <summary>
    /// AutoMapper配置文件
    /// </summary>
    public class AutoMapperProfile : Profile
    {
        /// <summary>
        /// 构造函数，配置映射关系
        /// </summary>
        public AutoMapperProfile()
        {
            // 设备映射
            CreateMap<Device, DeviceDto>().ReverseMap();
            CreateMap<Device, CreateDeviceDto>().ReverseMap();
            CreateMap<Device, UpdateDeviceDto>().ReverseMap();

            // 设备点位映射
            CreateMap<DevicePoint, DevicePointDto>().ReverseMap();
            CreateMap<DevicePoint, CreateDevicePointDto>().ReverseMap();
            CreateMap<DevicePoint, UpdateDevicePointDto>().ReverseMap();

            // 通信通道映射
            CreateMap<Channel, ChannelDto>().ReverseMap();
            CreateMap<Channel, CreateChannelDto>().ReverseMap();
            CreateMap<Channel, UpdateChannelDto>().ReverseMap();

            // 协议配置映射
            CreateMap<ProtocolConfig, ProtocolConfigDto>().ReverseMap();
            CreateMap<ProtocolConfig, CreateProtocolConfigDto>().ReverseMap();
            CreateMap<ProtocolConfig, UpdateProtocolConfigDto>().ReverseMap();

            // 数据采集记录映射
            CreateMap<DataCollectionRecord, DataCollectionRecordDto>().ReverseMap();

            // 规则映射
            CreateMap<Rule, RuleDto>().ReverseMap();
            CreateMap<Rule, CreateRuleDto>().ReverseMap();
            CreateMap<Rule, UpdateRuleDto>().ReverseMap();

            // 规则结果映射
            CreateMap<RuleResult, RuleResultDto>().ReverseMap();

            //// 报警配置映射
            //CreateMap<AlarmConfig, AlarmConfigDto>().ReverseMap();
            //CreateMap<AlarmConfig, AlarmConfigCreateDto>().ReverseMap();
            //CreateMap<AlarmConfig, AlarmConfigUpdateDto>().ReverseMap();

            //// 报警记录映射
            //CreateMap<AlarmRecord, AlarmRecordDto>().ReverseMap();

            //// 用户映射
            //CreateMap<User, UserDto>().ReverseMap();
            //CreateMap<User, UserCreateDto>().ReverseMap();
            //CreateMap<User, UserUpdateDto>().ReverseMap();

            //// 角色映射
            //CreateMap<Role, RoleDto>().ReverseMap();
            //CreateMap<Role, RoleCreateDto>().ReverseMap();
            //CreateMap<Role, RoleUpdateDto>().ReverseMap();

            //// 系统配置映射
            //CreateMap<SystemConfig, SystemConfigDto>().ReverseMap();
            //CreateMap<SystemConfig, SystemConfigUpdateDto>().ReverseMap();
        }
    }
}