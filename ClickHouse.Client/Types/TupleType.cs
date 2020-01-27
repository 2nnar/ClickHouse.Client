﻿using System;
using System.Linq;
using System.Runtime.CompilerServices;
using ClickHouse.Client.Utility;

namespace ClickHouse.Client.Types
{
    internal class TupleType : ParameterizedType
    {
        private Type frameworkType;
        private ClickHouseType[] underlyingTypes;

        public override ClickHouseTypeCode TypeCode => ClickHouseTypeCode.Tuple;

        public ClickHouseType[] UnderlyingTypes
        {
            get => underlyingTypes;
            set
            {
                underlyingTypes = value;
                frameworkType = DeviseFrameworkType(underlyingTypes);
            }
        }

        private static Type DeviseFrameworkType(ClickHouseType[] underlyingTypes)
        {
            var count = underlyingTypes.Length;
            var typeArgs = new Type[count];
            for (var i = 0; i < count; i++)
                typeArgs[i] = underlyingTypes[i].FrameworkType;

            var genericType = Type.GetType("System.Tuple`" + typeArgs.Length);
            return genericType.MakeGenericType(typeArgs);
        }

        public ITuple MakeTuple(params object[] values)
        {
            var count = values.Length;
            if (underlyingTypes.Length != count)
                throw new ArgumentException($"Count of tuple type elements ({underlyingTypes.Length}) does not match number of elements ({count})");
            return (ITuple)Activator.CreateInstance(frameworkType, values);
        }

        public override Type FrameworkType => frameworkType;

        public override string Name => "Tuple";

        public override ParameterizedType Parse(string typeName, Func<string, ClickHouseType> typeResolverFunc)
        {
            if (!typeName.StartsWith(Name))
                throw new ArgumentException(nameof(typeName));

            var underlyingTypeNames = typeName
                .Substring(Name.Length)
                .TrimRoundBrackets()
                .Split(',')
                .Select(t => t.Trim());

            return new TupleType
            {
                UnderlyingTypes = underlyingTypeNames.Select(typeResolverFunc).ToArray()
            };
        }

        public override string ToString() => $"{Name}({string.Join(",", UnderlyingTypes.Select(t => t.ToString()))})";
    }

    internal class NestedTypeInfo : TupleType
    {
        public override ClickHouseTypeCode TypeCode => ClickHouseTypeCode.Nested;

        public override ParameterizedType Parse(string typeName, Func<string, ClickHouseType> typeResolverFunc)
        {
            if (!typeName.StartsWith(Name))
                throw new ArgumentException(nameof(typeName));

            var underlyingTypeNames = typeName.Substring(Name.Length).TrimRoundBrackets().Split(',');

            return new NestedTypeInfo
            {
                UnderlyingTypes = underlyingTypeNames.Select(typeResolverFunc).ToArray()
            };
        }

        public override string Name => "Nested";
    }
}
