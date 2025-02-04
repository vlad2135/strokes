﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using Strokes.Core;
using Strokes.Core.Service.Model;
using Strokes.Service.Data;

namespace Strokes.Data
{
    public class AppDataXmlCompletedAchievementsRepository : AppDataXmlFileRepositoryBase<List<CompletedAchievement>>, IAchievementRepository
    {
        private readonly List<Achievement> _achievements = new List<Achievement>();
        private readonly List<CompletedAchievement> _completedAchievements;

        public AppDataXmlCompletedAchievementsRepository(string storageFile) : base(storageFile)
        {
            _completedAchievements = Load() ?? new List<CompletedAchievement>();
        }

        public IEnumerable<Achievement> GetAchievements()
        {
            foreach (var achievement in _achievements)
            {
                var achievementInstance = achievement;
                var completedAchievement = _completedAchievements.FirstOrDefault(a => a.Guid == achievementInstance.Guid);

                if (completedAchievement != null)
                {
                    achievement.DateCompleted = completedAchievement.DateCompleted;
                    achievement.IsCompleted = completedAchievement.IsCompleted;
                }

                yield return achievement;
            }
        }

        public IEnumerable<Achievement> GetUnlockableAchievements()
        {
            var unlockedGuids = _completedAchievements.Select(a => a.Guid);
            return _achievements.Where(a => !a.IsCompleted && a.DependsOn.All(b => unlockedGuids.Contains(b.Guid)));
        }

        public void MarkAchievementAsCompleted(Achievement achievement)
        {
            achievement.DateCompleted = DateTime.Now;
            achievement.IsCompleted = true;

            var completedAchievement = new CompletedAchievement(achievement)
            {
                DateCompleted = DateTime.Now,
                IsCompleted = true
            };

            if(achievement.CodeOrigin != null)
            {
                completedAchievement.CodeSnippet = achievement.CodeOrigin.GetCodeSnippet();
                achievement.CodeSnippet = completedAchievement.CodeSnippet;
            }

            if (!_completedAchievements.Contains(completedAchievement))
            {
                _completedAchievements.Add(completedAchievement);
                PersistCompletedAchievements();
            }
        }

        public void LoadFromAssembly(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            var achievementTypesInAssembly = assembly.GetTypes().Where(a => typeof(AchievementBase).IsAssignableFrom(a) && !a.IsAbstract);
            var assemblyAchievements = achievementTypesInAssembly.Select(achievement => achievement.GetAchievementDto()).ToList();
            var achievementDescriptors = achievementTypesInAssembly.Select(achievement => achievement.GetDescriptionAttribute()).ToList();

            foreach (var achievement in assemblyAchievements)
            {
                var currentAchievement = achievement;
                var completedAchievement = _completedAchievements.FirstOrDefault(a => a.Guid == currentAchievement.Guid);

                //Bind information from the persisted storage about this achievement (this is because it's been completed at an earlier time)
                if (completedAchievement != null)
                {
                    currentAchievement.DateCompleted = completedAchievement.DateCompleted;
                    currentAchievement.IsCompleted = completedAchievement.IsCompleted;
                    currentAchievement.CodeSnippet = completedAchievement.CodeSnippet;
                }

                var dependsOnGuids = achievementDescriptors
                        .Where(a => a.Guid == currentAchievement.Guid)
                        .SelectMany(a => a.DependsOn)
                        .ToList();

                var unlocksGuids = achievementDescriptors
                        .Where(a => a.DependsOn.Contains(currentAchievement.Guid))
                        .Select(a => a.Guid)
                        .ToList();

                currentAchievement.DependsOn = assemblyAchievements.Where(a => dependsOnGuids.Contains(a.Guid)).ToList();
                currentAchievement.Unlocks = assemblyAchievements.Where(a => unlocksGuids.Contains(a.Guid)).ToList();

                _achievements.Add(currentAchievement);
            }
        }

        public void ResetAchievements()
        {
            _completedAchievements.Clear();
            foreach (var achievement in _achievements)
            {
                achievement.IsCompleted = false;
                achievement.DateCompleted = default(DateTime);
            }

            Erase();
        }

        private void PersistCompletedAchievements()
        {
            Save(_completedAchievements);
        }
    }
}