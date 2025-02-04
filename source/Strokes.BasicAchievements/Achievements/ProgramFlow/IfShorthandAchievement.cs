﻿using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using Strokes.BasicAchievements.NRefactory;
using Strokes.Core;
using Strokes.Core.Service;
using Strokes.Core.Service.Model;

namespace Strokes.BasicAchievements.Achievements
{
    [AchievementDescriptor("{9A45754C-CA06-4AFA-8489-D091427296CB}", "@IfShorthandAchievementName",
        AchievementDescription = "@IfShorthandAchievementDescription",
        AchievementCategory = "@ProgramFlow")]
    public class IfShorthandAchievement : NRefactoryAchievement
    {
        protected override AbstractAchievementVisitor CreateVisitor(StatisAnalysisSession statisAnalysisSession)
        {
            return new Visitor();
        }

        private class Visitor : AbstractAchievementVisitor
        {
            public override object VisitConditionalExpression(ConditionalExpression conditionalExpression, object data)
            {
                UnlockWith(conditionalExpression);

                return base.VisitConditionalExpression(conditionalExpression, data);
            }
        }
    }
}