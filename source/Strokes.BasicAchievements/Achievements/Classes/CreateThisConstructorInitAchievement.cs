﻿using System.Collections.Generic;
using System.Linq;
using ICSharpCode.NRefactory.Ast;
using Strokes.BasicAchievements.NRefactory;
using Strokes.Core;

namespace Strokes.BasicAchievements.Achievements
{
    [AchievementDescription("{DB056781-A107-4396-8D33-A9B94F0C05A3}", "@CreateThisConstructorInitAchievementName",
        AchievementDescription = "@CreateThisConstructorInitAchievementDescription",
        AchievementCategory = "@Class")]
    public class CreateThisConstructorInitAchievement : NRefactoryAchievement
    {
        protected override AbstractAchievementVisitor CreateVisitor()
        {
            return new Visitor();
        }

        private class Visitor : AbstractAchievementVisitor
        {
            public override object VisitConstructorDeclaration(ConstructorDeclaration constructorDeclaration, object data)
            {

                if (constructorDeclaration.ConstructorInitializer.ConstructorInitializerType== ConstructorInitializerType.This)
                    UnlockWith(constructorDeclaration);
                return base.VisitConstructorDeclaration(constructorDeclaration, data);
            }
        }
    }
}