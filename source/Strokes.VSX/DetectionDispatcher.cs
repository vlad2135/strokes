﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Strokes.Core;
using Strokes.Core.Contracts;
using Strokes.Core.Model;
using Strokes.Data;
using Strokes.GUI;

namespace Strokes.VSX
{
    public class DetectionDispatcher
    {
        /// <summary>
        /// Occours when the achievement detection have completed.
        /// </summary>
        public static event EventHandler<DetectionCompletedEventArgs> DetectionCompleted;

        /// <summary>
        /// Called when the achievement detection have completed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">
        ///     The <see cref="Strokes.VSX.DetectionCompletedEventArgs"/> instance containing the event data.
        /// </param>
        protected static void OnDetectionCompleted(object sender, DetectionCompletedEventArgs args)
        {
            if (DetectionCompleted != null)
            {
                DetectionCompleted(sender, args);
            }
        }

        /// <summary>
        /// Dispatches handling of achievement detection in the file(s) specified 
        /// in the passed BuildInformation object.
        /// 
        /// This method is detection method agnostic. It simply forwards the 
        /// BuildInformation object to all implementations of the Achievement class.
        /// </summary>
        /// <param name="buildInformation">
        ///     Objects specifying documents to parse for achievements.
        /// </param>
        public static bool Dispatch(BuildInformation buildInformation)
        {
            AchievementContext.OnAchievementDetectionStarting(null, new EventArgs());

            var unlockedAchievements = new List<AchievementDescriptor>();
            var achievementDescriptorRepository =
                StrokesVsxPackageEx.GetGlobalService<AchievementDescriptorRepository>();

            // For non VSX projects, the GlobalService won't be initialized, 
            // and as such, it will return null.
            if (achievementDescriptorRepository == null)
                achievementDescriptorRepository = new AchievementDescriptorRepository();

            using (var detectionSession = new DetectionSession(buildInformation))
            {
                var achievementDescriptors = achievementDescriptorRepository.GetAll();
                var uncompletedAchievements = achievementDescriptors.Where(a => !a.IsCompleted || AchievementContext.DisablePersist);

                var stopWatch = new Stopwatch();
                stopWatch.Start();

                var tasks = new Task[uncompletedAchievements.Count()];
                var i = 0;

                foreach (var uncompletedAchievement in uncompletedAchievements)
                {
                    var a = uncompletedAchievement;

                    tasks[i++] = Task.Factory.StartNew(() =>
                    {
                        var achievement = (Achievement)Activator.CreateInstance(a.AchievementType);

                        var achievementUnlocked = achievement.DetectAchievement(detectionSession);

                        if (achievementUnlocked)
                        {
                            a.CodeLocation = achievement.AchievementCodeLocation;
                            a.IsCompleted = true;
                            unlockedAchievements.Add(a);
                        }
                    });
                }

                Task.WaitAll(tasks);

                stopWatch.Stop();

                OnDetectionCompleted(null, new DetectionCompletedEventArgs()
                {
                    AchievementsTested = uncompletedAchievements.Count(),
                    ElapsedMilliseconds = (int)stopWatch.ElapsedMilliseconds
                });
            }

            if (unlockedAchievements.Count() > 0)
            {
                foreach (var completedAchievement in unlockedAchievements.Where(a => a != null)) //I've seen a case where this has been populated with nulls - I'm NOT sure why - but this at least tests for it.
                {
                    achievementDescriptorRepository.MarkAchievementAsCompleted(completedAchievement);
                }

                AchievementContext.OnAchievementsUnlocked(null, unlockedAchievements);

                return true;
            }

            return false;
        }
    }

    public class DetectionCompletedEventArgs : EventArgs
    {
        public int AchievementsTested
        {
            get;
            set;
        }

        public int ElapsedMilliseconds
        {
            get;
            set;
        }
    }
}