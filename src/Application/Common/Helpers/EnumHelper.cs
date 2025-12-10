using System;
using Application.Common.DTOs;

namespace Application.Common.Helpers
{
    public static class EnumHelper
    {
        public static int ToEnumId(this Enum enumValue)
        {
            if (enumValue == null)
            {
                return 0;
            }

            return Convert.ToInt32(enumValue);
        }

        public static EnumDto ToEnumDto(this Enum enumValue)
        {
            if (enumValue == null)
            {
                return new EnumDto
                {
                    Id = 0,
                    Nome = string.Empty
                };
            }

            return new EnumDto
            {
                Id = enumValue.ToEnumId(),
                Nome = enumValue.ToString()
            };
        }
    }
}
