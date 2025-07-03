using AutoMapper;
using Wombat.CommGateway.Application.DTOs;
using Wombat.CommGateway.Domain.Entities;

namespace Wombat.CommGateway.Application.Mappings
{
    /// <summary>
    /// 规则映射配置
    /// </summary>
    public class RuleMappingProfile : Profile
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public RuleMappingProfile()
        {
            CreateMap<Rule, RuleDto>();
            CreateMap<RuleCondition, RuleConditionDto>();
            CreateMap<RuleAction, RuleActionDto>();

            CreateMap<CreateRuleDto, Rule>();
            CreateMap<CreateRuleConditionDto, RuleCondition>();
            CreateMap<CreateRuleActionDto, RuleAction>();

            CreateMap<UpdateRuleDto, Rule>();
            CreateMap<UpdateRuleConditionDto, RuleCondition>();
            CreateMap<UpdateRuleActionDto, RuleAction>();

            CreateMap<RuleResult, RuleResultDto>();
            CreateMap<RuleExecutionResult, RuleExecutionResultDto>();
            CreateMap<ActionExecutionResult, ActionExecutionResultDto>();
        }
    }
} 