﻿using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Ast;
using Strokes.BasicAchievements.NRefactory;
using Strokes.Core;

namespace Strokes.BasicAchievements.Achievements
{
    [AchievementDescription("{414E069A-0CE9-452B-BCB8-4131F1D949A5}", "@DestructorAchievementName",
        AchievementDescription = "@DestructorAchievementDescription",
        AchievementCategory = "@Class")]
    public class DestructorAchievement : NRefactoryAchievement
    {
        protected override AbstractAchievementVisitor CreateVisitor()
        {
            return new Visitor();
        }

        private class Visitor : AbstractAchievementVisitor
        {

            public override object VisitDestructorDeclaration(DestructorDeclaration destructorDeclaration, object data)
            {
                UnlockWith(destructorDeclaration);
                return base.VisitDestructorDeclaration(destructorDeclaration, data);
            }

        }
    }
}