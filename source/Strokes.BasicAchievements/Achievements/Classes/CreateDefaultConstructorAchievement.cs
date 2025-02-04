﻿using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.CSharp;
using Strokes.BasicAchievements.NRefactory;
using Strokes.Core;
using Strokes.Core.Service;
using Strokes.Core.Service.Model;

namespace Strokes.BasicAchievements.Achievements
{
    [AchievementDescriptor("{861D2E3B-4319-4EBA-A8D8-267243CED190}", "@CreateDefaultConstructorAchievementName",
        AchievementDescription = "@CreateDefaultConstructorAchievementDescription",
        AchievementCategory = "@Class")]
    public class CreateDefaultConstructorAchievement : NRefactoryAchievement
    {
        protected override AbstractAchievementVisitor CreateVisitor(StatisAnalysisSession statisAnalysisSession)
        {
            return new Visitor();
        }

        private class Visitor : AbstractAchievementVisitor
        {
            public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
            {
                if (constructorDeclaration.Parameters.Count == 0)
                    UnlockWith(constructorDeclaration);
                return base.VisitConstructorDeclaration(constructorDeclaration, data);
            }
        }
    }
}