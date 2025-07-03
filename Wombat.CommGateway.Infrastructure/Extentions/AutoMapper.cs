using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Wombat;
using Wombat.Extensions.DataTypeExtensions;
using Wombat.Infrastructure;

namespace  Wombat.CommGateway.Infrastructure
{
    public static partial class Extention
    {

        /// <summary>
        /// 忽略所有不匹配的属性。
        /// </summary>
        /// <param name="expression">配置表达式</param>
        /// <param name="from">源类型</param>
        /// <param name="to">目标类型</param>
        /// <returns></returns>
        public static IMappingExpression IgnoreAllNonExisting(this IMappingExpression expression, Type from, Type to)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance;
            to.GetProperties(flags).Where(x => from.GetProperty(x.Name, flags) == null).ForEach(aProperty =>
            {
                expression.ForMember(aProperty.Name, opt => opt.Ignore());
            });

            return expression;
        }

    }
}
