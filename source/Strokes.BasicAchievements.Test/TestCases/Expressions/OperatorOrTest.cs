﻿using Strokes.BasicAchievements.Achievements;

namespace Strokes.BasicAchievements.Test.TestCases.Expressions
{
    [ExpectUnlock(typeof(OperatorOrAchievement))]
    public class OperatorOrTest
    {
        public void Test()
        {
            int a = 1;
            int b = 2;

            if (a == 1 || b == 2)
            {
            }
        }
    }
}
