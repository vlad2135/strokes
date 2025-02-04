﻿using System;
using System.Resources;
using System.Reflection;
using Strokes.Core.Service.Model;

namespace Strokes.Core
{
    public static class AchievementDescriptorAttributeHelper
    {
        public static AchievementDescriptorAttribute GetDescriptionAttribute(this Type achievementType)
        {
            var descriptionAttributes = achievementType.GetCustomAttributes(typeof(AchievementDescriptorAttribute), true);

            if (descriptionAttributes.Length == 1)
            {
                return (AchievementDescriptorAttribute)descriptionAttributes[0];
            }

            if (descriptionAttributes.Length > 1)
            {
                throw new ArgumentException("Achievement class defines more than one AchievementDescriptionAttribute", "achievement");
            }

            throw new ArgumentException("Achievement class does not define an AchievementDescriptionAttribute", "achievement");
        }

        public static Achievement GetAchievementDto(this Type achievementType)
        {
            var descriptionAttribute = GetDescriptionAttribute(achievementType);
            var assembly = achievementType.Assembly;

            var AchievementResourcesType = assembly.GetType("Strokes.Resources.AchievementResources");
            var categoryResourcesType = assembly.GetType("Strokes.Resources.AchievementCategoryResources");

            var AchievementResources = (ResourceManager)AchievementResourcesType.GetProperty("ResourceManager",
                BindingFlags.Static | BindingFlags.Public).GetValue(null, null);

            var categoryResources = (ResourceManager)categoryResourcesType.GetProperty("ResourceManager",
                BindingFlags.Static | BindingFlags.Public).GetValue(null, null);

            var category = descriptionAttribute.AchievementCategory;
            if (category.StartsWith("@") && category.Length > 1)
                category = categoryResources.GetString(category.Substring(1));

            var title = descriptionAttribute.AchievementTitle;
            if (title.StartsWith("@") && title.Length > 1)
                title = AchievementResources.GetString(title.Substring(1));

            var description = descriptionAttribute.AchievementDescription;
            if (description.StartsWith("@") && description.Length > 1)
                description = AchievementResources.GetString(description.Substring(1));

            var descriptor = new Achievement
            {
                Guid = descriptionAttribute.Guid,
                AchievementType = achievementType,
                Category = category,
                Description = description,
                Name = title,
                Image = descriptionAttribute.Image
            };

            return descriptor;
        }
    }
}
